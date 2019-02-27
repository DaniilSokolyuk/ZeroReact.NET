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
        private readonly AutoResetEvent _enginePopulater = new AutoResetEvent(false);

        private readonly AutoResetEvent _engineMaintenance = new AutoResetEvent(false);
        private readonly ConcurrentQueue<ChakraCoreJsEngine> _enginesToMaintenance = new ConcurrentQueue<ChakraCoreJsEngine>();

        private readonly ConcurrentQueue<Action<ChakraCoreJsEngine>> _sharedQueue = new ConcurrentQueue<Action<ChakraCoreJsEngine>>();
        private readonly AutoResetEvent _sharedQueueEnqeued = new AutoResetEvent(false);

        public ZeroJsPool(ZeroJsPoolConfig config)
        {
            _config = config;

            new Thread(
                    () =>
                    {
                        while (!disposed.IsSet())
                        {
                            if (_engines.Count >= _config.StartEngines)
                            {
                                _enginePopulater.WaitOne(1000);
                            }

                            //pupulater
                            while (!disposed.IsSet() && _engines.Count < _config.StartEngines)
                            {
                                CreateEngine();
                            }

                            //MaxUsagesPerEngine
                            if (!disposed.IsSet() && _config.MaxUsagesPerEngine > 0 &&
                                _engines.Count < _config.MaxEngines)
                            {
                                var currentUsages = _engines.Values.Sum();
                                var maxUsages = _engines.Count * _config.MaxUsagesPerEngine;

                                var engineAverageOverflow = currentUsages > maxUsages * 0.7;

                                if (engineAverageOverflow)
                                {
                                    CreateEngine();
                                }
                            }
                        }
                    })
            {
                IsBackground = true,
                Name = "Engine creator"
            }
                .Start();

            new Thread(
                    () =>
                    {
                        while (!disposed.IsSet())
                        {
                            _engineMaintenance.WaitOne();

                            while (_enginesToMaintenance.TryDequeue(out var maintenangeEngine))
                            {
                                if (disposed.IsSet())
                                {
                                    return;
                                }

                                if (!_engines.TryGetValue(maintenangeEngine, out var usageCount) ||
                                    _config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine
                                )
                                {
                                    DisposeEngine(maintenangeEngine);
                                }
                                else if (maintenangeEngine.SupportsGarbageCollection &&
                                         _config.GarbageCollectionInterval > 0) //gc
                                {
                                    maintenangeEngine.CollectGarbage();
                                }
                            }
                        }
                    })
            {
                IsBackground = true,
                Name = "Engine maintenance"
            }
                .Start();
        }

        private void CreateEngine()
        {
            var engine = _config.EngineFactory();

            engine._dispatcher.Invoke(
                () =>
                {
                    if (disposed.IsSet())
                    {
                        return;
                    }

                    engine._dispatcher._sharedQueue = _sharedQueue;
                    engine._dispatcher._sharedQueueEnqeued = _sharedQueueEnqeued;
                });

            if (disposed.IsSet())
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
                engine._dispatcher.Invoke(
                    () =>
                    {
                        engine._dispatcher._sharedQueue = null;
                        engine._dispatcher._sharedQueueEnqeued = null;
                    });
            }
            finally
            {
                engine.Dispose();

                if (!disposed.IsSet())
                {
                    _enginePopulater.Set();
                }
            }
        }

        private InterlockedStatedFlag disposed;
        public void Dispose()
        {
            if (disposed.Set())
            {
                _engineMaintenance.Set();
                _enginePopulater.Set();

                _engineMaintenance.Close();
                _enginePopulater.Close();

                foreach (var engine in _engines.Keys.ToArray())
                {
                    DisposeEngine(engine);
                }

                _sharedQueue.Clear();
                _sharedQueueEnqeued.Close();
            }
        }

        private static object obj = new object();

        public Task ScheduleWork(Action<ChakraCoreJsEngine> work)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            _sharedQueue.Enqueue(jsEngine =>
            {
                if (disposed.IsSet()) //test
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
                        if ((_config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine) ||
                            (_config.GarbageCollectionInterval > 0 && usageCount % _config.GarbageCollectionInterval == 0))
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
            if (disposed.IsSet()) //test
            {
                DisposeEngine(engine);
                return;
            }

            _enginesToMaintenance.Enqueue(engine);
            _engineMaintenance.Set();
        }
    }
}
