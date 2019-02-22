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
        private readonly AutoResetEvent _engineMaintenance = new AutoResetEvent(false);
        private readonly ConcurrentQueue<ChakraCoreJsEngine> _enginesToMaintenance = new ConcurrentQueue<ChakraCoreJsEngine>();

        private readonly ConcurrentDictionary<ChakraCoreJsEngine, int> _engines = new ConcurrentDictionary<ChakraCoreJsEngine, int>();

        private readonly ConcurrentQueue<ChakraCoreJsEngine> _availableEngines = new ConcurrentQueue<ChakraCoreJsEngine>();

        public ZeroJsPool(ZeroJsPoolConfig config)
        {
            _config = config;

            new Thread(
                () =>
                {
                    while (!disposed.IsSet())
                    {
                        if (_enginesToMaintenance.IsEmpty && _engines.Count >= _config.StartEngines)
                        {
                            _engineMaintenance.WaitOne(1000);
                        }

                        //pupulater
                        while (!disposed.IsSet() && _engines.Count < _config.StartEngines)
                        {
                            var engine = CreateEngine();
                            AddToAvailiableEngines(engine);
                        }

                        //MaxUsagesPerEngine
                        if (!disposed.IsSet() && _config.MaxUsagesPerEngine > 0 && _engines.Count < _config.MaxEngines)
                        {
                            var currentUsages = _engines.Values.Sum();
                            var maxUsages = _engines.Count * _config.MaxUsagesPerEngine;

                            var engineAverageOverflow = currentUsages > maxUsages * 0.7;

                            if (engineAverageOverflow)
                            {
                                var engine = CreateEngine();
                                AddToAvailiableEngines(engine);
                            }
                        }

                        if (!disposed.IsSet() && _enginesToMaintenance.TryDequeue(out var maintenangeEngine))
                        {
                            if (!_engines.TryGetValue(maintenangeEngine, out var usageCount) ||
                                (_config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine)) //MaxUsagesPerEngine disposer
                            {
                                DisposeEngine(maintenangeEngine);
                            }
                            else if (maintenangeEngine.SupportsGarbageCollection && _config.GarbageCollectionInterval > 0) //gc
                            {
                                maintenangeEngine.CollectGarbage();
                                AddToAvailiableEngines(maintenangeEngine);
                            }
                            else
                            {
                                //idk why we here :)
                                AddToAvailiableEngines(maintenangeEngine);
                            }
                        }
                    }
                }).Start();
        }

        private ChakraCoreJsEngine CreateEngine()
        {
            var engine = _config.EngineFactory();
            _engines.TryAdd(engine, 0);
            return engine;
        }

        private void AddToAvailiableEngines(ChakraCoreJsEngine engine)
        {
            if (disposed.IsSet()) //test
            {
                DisposeEngine(engine);
                return;
            }
            
            _availableEngines.Enqueue(engine);
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

        public void DisposeEngine(ChakraCoreJsEngine engine)
        {
            engine?.Dispose();
            _engines.TryRemove(engine, out _);

            if (!disposed.IsSet())
            {
                _engineMaintenance.Set();
            }
        }

        private InterlockedStatedFlag disposed;
        public void Dispose()
        {
            if (disposed.Set())
            {
                while (_availableEngines.TryDequeue(out var engine) | _enginesToMaintenance.TryDequeue(out var engine2))
                {
                    DisposeEngine(engine ?? engine2);
                }

                _engines.Clear();
                _engineMaintenance?.Dispose();
            }
        }

        public void Return(ChakraCoreJsEngine engine)
        {
            if (!_engines.TryGetValue(engine, out var usageCount) ||
                (_config.MaxUsagesPerEngine > 0 && usageCount >= _config.MaxUsagesPerEngine) ||
                (_config.GarbageCollectionInterval > 0 && usageCount % _config.GarbageCollectionInterval == 0))
            {
                MaintenanceEngine(engine);
            }
            else
            {
                AddToAvailiableEngines(engine);
            }
        }

        public Task ScheduleWork(Action<ChakraCoreJsEngine> work)
        {
            while (true)
            {
                if (_availableEngines.TryDequeue(out var engine))
                {
                    return engine.Schedule(jsEngine =>
                    {
                        _engines[engine]++;
                        work(jsEngine);
                        Return(jsEngine);
                    });
                }
            }
        }
    }
}
