using System;
using JavaScriptEngineSwitcher.Core;

namespace ZeroReact.JsPool
{
    public class ZeroJsPoolConfig
    {
        public ZeroJsPoolConfig()
        {
            StartEngines = 10;
            MaxEngines = 25;
            MaxUsagesPerEngine = 100;
        }

        public Func<IJsEngine> EngineFactory { get; set; }
        public int StartEngines { get; set; }
        public int MaxEngines { get; set; }
        public int MaxUsagesPerEngine { get; set; }
        public int GarbageCollectionInterval { get; set; }
    }
}