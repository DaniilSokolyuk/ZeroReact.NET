using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core.Utilities;

namespace ZeroReact.JsPool
{
    public sealed class ZeroJsPool : IDisposable
    {
        private readonly ZeroJsPoolConfig _config;

        private readonly ConcurrentDictionary<ChakraCoreJsEngine, int> _engines = new ConcurrentDictionary<ChakraCoreJsEngine, int>();
        private AutoResetEvent _enginePopulater = new AutoResetEvent(false);
        private AutoResetEvent _engineMaintenance = new AutoResetEvent(false);

        private ConcurrentQueue<Action<ChakraCoreJsEngine>> _sharedQueue = new ConcurrentQueue<Action<ChakraCoreJsEngine>>();
        private AutoResetEvent _sharedQueueEnqeued = new AutoResetEvent(false);

        public ZeroJsPool(ZeroJsPoolConfig config)
        {
            _config = config;

            new Thread(
                    () =>
                    {
                        while (!_disposedFlag.IsSet())
                        {
                            if (_engines.Count >= _config.StartEngines)
                            {
                                _enginePopulater?.WaitOne(500);
                            }

                            //pupulater
                            while (!_disposedFlag.IsSet() && _engines.Count < _config.StartEngines)
                            {
                                CreateEngine();
                            }

                            //MaxUsagesPerEngine
                            if (!_disposedFlag.IsSet() && 
                                _config.MaxUsagesPerEngine > 0 &&
                                _engines.Count < _config.MaxEngines)
                            {
                                var currentUsages = _engines.Values.Sum();
                                var maxUsages = _engines.Count * _config.MaxUsagesPerEngine;

                                var engineAverageOverflow = currentUsages > maxUsages * 0.7;

                                if (engineAverageOverflow)
                                {
                                    CreateEngine();
                                    _enginePopulater?.Set();
                                }
                            }
                        }
                    })
                {
                    IsBackground = true,
                    Name = "ZeroReact Engine creator"
                }
                .Start();

            new Thread(
                    () =>
                    {
                        while (!_disposedFlag.IsSet() && _config.MaxUsagesPerEngine > 0)
                        {
                            _engineMaintenance?.WaitOne();

                            var engineToDispose = _engines
                                .Where(x => x.Value >= _config.MaxUsagesPerEngine)
                                .OrderByDescending(x => x.Value)
                                .Select(x => x.Key)
                                .ToArray();

                            bool anyDisposed = false;

                            foreach (var engine in engineToDispose)
                            {
                                if (!_disposedFlag.IsSet() && _engines.Count >= _config.StartEngines)
                                {
                                    DisposeEngine(engine);
                                    anyDisposed = true;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (anyDisposed)
                            {
                                _engineMaintenance?.Set();
                            }
                        }
                    })
                {
                    IsBackground = true,
                    Name = "ZeroReact Engine destroyer"
                }
                .Start();
        }

        private void CreateEngine()
        {
            if (_disposedFlag.IsSet()) //test
            {
                return;
            }

            var engine = _config.EngineFactory();

            engine._dispatcher.Invoke(
                () =>
                {
                    if (_disposedFlag.IsSet()) //engine creation is really slow
                    {
                        return;
                    }

                    engine._dispatcher._sharedQueue = _sharedQueue;
                    engine._dispatcher._sharedQueueEnqeued = _sharedQueueEnqeued;
                });

            if (_disposedFlag.IsSet())
            {
                DisposeEngine(engine);
            }
            else
            {
                _engines.TryAdd(engine, 0);
            }
        }

        public void DisposeEngine(ChakraCoreJsEngine engine)
        {
            _engines.TryRemove(engine, out _);

            try
            {
                engine._dispatcher?.Invoke(
                    () =>
                    {
                        engine._dispatcher._sharedQueue = null;
                        engine._dispatcher._sharedQueueEnqeued = null;
                    });
            }
            catch (Exception ex)
            {
                // ignored
            }

            engine.Dispose();
            _enginePopulater?.Set();
        }

        private InterlockedStatedFlag _disposedFlag = new InterlockedStatedFlag();
        public void Dispose()
        {
            if (_disposedFlag.Set())
            {
                _engineMaintenance.Set();
                _engineMaintenance.Dispose();
                _engineMaintenance = null;

                _enginePopulater.Set();
                _enginePopulater.Dispose();
                _enginePopulater = null;

                foreach (var engine in _engines.Keys.ToArray())
                {
                    DisposeEngine(engine);
                }

                var sharedQueue = _sharedQueue;
                _sharedQueue = null;
                sharedQueue.Clear();

                _sharedQueueEnqeued.Dispose();
                _sharedQueueEnqeued = null;
            }
        }

        private static object obj = new object();

        public Task ScheduleWork(Action<ChakraCoreJsEngine> work)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _sharedQueue.Enqueue(jsEngine =>
            {
                if (_disposedFlag.IsSet()) //test
                {
                    tcs.SetCanceled();
                    return;
                }

                try
                {
                    work(jsEngine);
                    tcs.SetResult(obj);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
                finally
                {
                    if (_engines.TryGetValue(jsEngine, out var usageCount) && _engines.TryUpdate(jsEngine, usageCount + 1, usageCount))
                    {
                        if (_config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine)
                        {
                            MaintenanceEngine(jsEngine);
                        }
                    }
                }
            });
            _sharedQueueEnqeued.Set();

            return tcs.Task;
        }

        private void MaintenanceEngine(ChakraCoreJsEngine engine)
        {
            if (_disposedFlag.IsSet()) //test
            {
                DisposeEngine(engine);
                return;
            }

            _engineMaintenance.Set();
        }
    }
}
