using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Core.Utilities;

namespace ZeroReact.JsPool
{
    public class ZeroJsPoolConfig
    {
        public ZeroJsPoolConfig()
        {
            StartEngines = 10;
            MaxEngines = 25;
            MaxUsagesPerEngine = 100;
            GarbageCollectionInterval = 20;
        }

        public Func<IJsEngine> EngineFactory { get; set; }
        public int StartEngines { get; set; }
        public int MaxEngines { get; set; }
        public int MaxUsagesPerEngine { get; set; }
        public int GarbageCollectionInterval { get; set; }
    }

    public sealed class JsEngineOwner : IDisposable
    {
        private WeakReference<ZeroJsPool> _sourcePoolReference;

        public JsEngineOwner(ZeroJsPool pool, IJsEngine engine)
        {
            _engine = engine;
            _sourcePoolReference = new WeakReference<ZeroJsPool>(pool);
        }

        private IJsEngine _engine;
        public IJsEngine Engine => _engine;

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_engine is null || _sourcePoolReference is null)
                {
                    return;
                }

                if (_sourcePoolReference.TryGetTarget(out ZeroJsPool pool))
                {
                    pool.Return(_engine);
                }
                else
                {
                    //looks like our pool is not exists more....
                    _engine.Dispose();
                }

                _sourcePoolReference = null;
                _engine = null;
            }
            else
            {
                _engine.Dispose();
            }
        }

        ~JsEngineOwner()
        {
            Dispose(false);
        }
    }

    public sealed class ZeroJsPool : IDisposable
    {
        private readonly ZeroJsPoolConfig _config;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly AutoResetEvent _engineMaintenance = new AutoResetEvent(false);
        private readonly ConcurrentQueue<IJsEngine> _enginesToMaintenance = new ConcurrentQueue<IJsEngine>();

        private readonly ConcurrentDictionary<IJsEngine, int> _engines = new ConcurrentDictionary<IJsEngine, int>();

        private readonly ConcurrentQueue<IJsEngine> _availableEngines = new ConcurrentQueue<IJsEngine>();
        private readonly SemaphoreSlim _availableEnginesLock = new SemaphoreSlim(0);

        public ZeroJsPool(ZeroJsPoolConfig config)
        {
            _config = config;

            new Thread(
                () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        if (_enginesToMaintenance.IsEmpty && _engines.Count >= _config.StartEngines)
                        {
                            _engineMaintenance.WaitOne(1000);
                        }

                        //pupulater
                        while (_engines.Count < _config.StartEngines)
                        {
                            var engine = CreateEngine();
                            AddToAvailiableEngines(engine);
                        }

                        //MaxUsagesPerEngine
                        if (_config.MaxUsagesPerEngine > 0 && _engines.Count < _config.MaxEngines)
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

                        if (!_cancellationTokenSource.IsCancellationRequested && _enginesToMaintenance.TryDequeue(out var maintenangeEngine))
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

        #region Taking

        public ValueTask<JsEngineOwner> TakeAsync(CancellationToken cancellationToken = default)
        {
            var task = _availableEnginesLock.WaitAsync(cancellationToken);
            return task.IsCompleted
                ? new ValueTask<JsEngineOwner>(TakeEngineFunc())
                : new ValueTask<JsEngineOwner>(TakeEngineFuncAwaited(task));
        }


        private async Task<JsEngineOwner> TakeEngineFuncAwaited(Task task)
        {
            await task;
            return TakeEngineFunc();
        }

        private JsEngineOwner TakeEngineFunc()
        {
            if (_availableEngines.TryDequeue(out var engine))
            {
                _engines[engine]++;
                return new JsEngineOwner(this, engine);
            }

            throw new Exception("Not availiable engine");
        }

        #endregion

        private IJsEngine CreateEngine()
        {
            var engine = _config.EngineFactory();
            _engines.TryAdd(engine, 0);
            return engine;
        }

        public void Return(IJsEngine engine)
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

        private void AddToAvailiableEngines(IJsEngine engine)
        {
            _availableEngines.Enqueue(engine);
            _availableEnginesLock.Release();
        }

        private void MaintenanceEngine(IJsEngine engine)
        {
            _enginesToMaintenance.Enqueue(engine);
            _engineMaintenance.Set();
        }

        public void DisposeEngine(IJsEngine engine)
        {
            engine?.Dispose();
            _engines.TryRemove(engine, out _);
            _engineMaintenance.Set();
        }

        private InterlockedStatedFlag disposed;
        public void Dispose()
        {
            if (disposed.Set())
            {
                _cancellationTokenSource.Cancel();
                _engineMaintenance?.Dispose();
                _availableEnginesLock?.Dispose();

                while (_availableEngines.TryDequeue(out var engine) | _enginesToMaintenance.TryDequeue(out var engine2))
                {
                    DisposeEngine(engine ?? engine2);
                }

                _engines.Clear();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
