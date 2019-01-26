﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using JavaScriptEngineSwitcher.Core;
using JSPool;

namespace ZeroReact.JsPool
{
    public class CustomJsPool : JsPool<PooledJsEngine, IJsEngine>, IJsPool
    {
        private readonly AutoResetEvent _enginePopulateEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _engineEnqueuedEvent = new AutoResetEvent(false);

        // move to metadata
        private readonly ConcurrentQueue<PooledJsEngine> _enginesToMaintenance = new ConcurrentQueue<PooledJsEngine>();

        public CustomJsPool(JsPoolConfig<IJsEngine> config) : base(config)
        {
            //populater
            new Thread(
                () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        WaitHandle.WaitAny(new WaitHandle[] { _enginePopulateEvent }, TimeSpan.FromSeconds(1));

                        while (EngineCount < _config.StartEngines)
                        {
                            var engine = CreateEngine();
                            _availableEngines.Add(engine);
                        }

                        if (EngineCount >= _config.MaxEngines)
                        {
                            continue;
                        }

                        var currentUsages = _registeredEngines.Keys.Sum(x => x.UsageCount);
                        var maxUsages = _registeredEngines.Count * _config.MaxUsagesPerEngine;

                        var engineAverageOverflow = currentUsages > maxUsages * 0.7;

                        if (engineAverageOverflow)
                        {
                            var engine = CreateEngine();
                            _availableEngines.Add(engine);
                        }
                    }
                }).Start();

            //maintencer
            new Thread(
                () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _engineEnqueuedEvent.WaitOne();

                        while (!_cancellationTokenSource.IsCancellationRequested && _enginesToMaintenance.TryDequeue(out var engine))
                        {
                            if (!_registeredEngines.ContainsKey(engine) || 
                                (_config.MaxUsagesPerEngine > 0 && engine.UsageCount >= _config.MaxUsagesPerEngine))
                            {
                                DisposeEngine(engine);
                                continue;
                            }

                            engine.CollectGarbage();
                            _availableEngines.Add(engine);
                        }
                    }
                }).Start();
        }

        protected override void ReturnEngineToPoolInternal(PooledJsEngine engine)
        {
            if (!_registeredEngines.ContainsKey(engine) || 
                (_config.MaxUsagesPerEngine > 0 && engine.UsageCount >= _config.MaxUsagesPerEngine) || 
                (engine.UsageCount % _config.GarbageCollectionInterval == 0))
            {
                MaintenanceEngine(engine);
            }
            else
            {
                _availableEngines.Add(engine);
            }
        }

        private void MaintenanceEngine(PooledJsEngine engine)
        {
            _enginesToMaintenance.Enqueue(engine);
            _engineEnqueuedEvent.Set();
        }

        protected override void PopulateEngines()
        {
            _enginePopulateEvent.Set();
        }
    }
}