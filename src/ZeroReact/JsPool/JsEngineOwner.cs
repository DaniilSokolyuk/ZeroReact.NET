using System;
using JavaScriptEngineSwitcher.Core;

namespace ZeroReact.JsPool
{
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
}