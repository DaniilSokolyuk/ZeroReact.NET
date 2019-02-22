using System;
using System.Collections.Concurrent;
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
        private readonly AutoResetEvent _enginePopulater = new AutoResetEvent(false);

        private readonly ConcurrentDictionary<ChakraCoreJsEngine, int> _engines = new ConcurrentDictionary<ChakraCoreJsEngine, int>();

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
                            if (!disposed.IsSet() && _config.MaxUsagesPerEngine > 0 && _engines.Count < _config.MaxEngines)
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
                    IsBackground = true
                }
                .Start();
        }

        private void CreateEngine()
        {
            var engine = _config.EngineFactory();
            engine._dispatcher._sharedQueue = _sharedQueue;
            engine._dispatcher._sharedQueueEnqeued = _sharedQueueEnqeued;

            _engines.TryAdd(engine, 0);
        }

        public void DisposeEngine(ChakraCoreJsEngine engine)
        {
            engine?.Dispose();
            _engines.TryRemove(engine, out _);

            if (!disposed.IsSet())
            {
                _enginePopulater.Set();
            }
        }

        private InterlockedStatedFlag disposed;
        public void Dispose()
        {
            if (disposed.Set())
            {
                foreach (var engine in _engines.Keys)
                {
                    DisposeEngine(engine);
                }

                _engines.Clear();
                _enginePopulater?.Dispose();
            }
        }

        public ConcurrentQueue<ScriptDispatcher.ActionTask> _sharedQueue = new ConcurrentQueue<ScriptDispatcher.ActionTask>();
        public AutoResetEvent _sharedQueueEnqeued = new AutoResetEvent(false);

        public Task ScheduleWork(Action<ChakraCoreJsEngine> work)
        {
            var task = new ScriptDispatcher.ActionTask(
                jsEngine =>
                {
                    _engines[jsEngine]++;
                    work(jsEngine);

                    //MaintenanceEngine
                    if (disposed.IsSet() ||
                        !_engines.TryGetValue(jsEngine, out var usageCount) || //idk how
                        (_config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine)) //MaxUsagesPerEngine
                    {
                        //DisposeEngine(jsEngine);
                        return;
                    }

                    if (_config.GarbageCollectionInterval > 0 && usageCount % _config.GarbageCollectionInterval == 0) //gc
                    {
                        //jsEngine.CollectGarbage();
                    }
                });

            _sharedQueue.Enqueue(task);
            _sharedQueueEnqeued.Set();

            return task.TaskCompletionSource.Task;
        }
    }
}
