using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using React;
using ZeroReact.AspNetCore;

namespace ZeroReact.Benchmarks
{
	public abstract class ComponentRenderBenchmarksBase
	{
		protected JObject _testData = JObject.FromObject(new Dictionary<string, string>() { ["name"] = "Tester" });

	    private IServiceProvider sp;

		protected void PopulateTestData()
		{
			for (int i = 0; i < 10000; i++)
			{
				_testData.Add("key" + i, "value" + i);
			}
		}

		protected void RegisterZeroReact()
		{
		    var services = new ServiceCollection();

		    services.AddZeroReactCore(
		        config =>
		        {
		            config.AddScriptWithoutTransform("Sample.js");
		            config.StartEngines = 2;
		            config.MaxEngines = 2;
		            config.MaxUsagesPerEngine = 0;
		            config.AllowJavaScriptPrecompilation = true;
		        });

            sp = services.BuildServiceProvider();
		}

        protected void RegisterReact()
        {
            Initializer.Initialize(registration => registration.AsSingleton());
            AssemblyRegistration.Container.Register<React.ICache, React.NullCache>();
            AssemblyRegistration.Container.Register<React.IFileSystem, React.PhysicalFileSystem>();
            AssemblyRegistration.Container.Register<IReactEnvironment, ReactEnvironment>().AsMultiInstance();

            JsEngineSwitcher.Current.EngineFactories.Add(new ChakraCoreJsEngineFactory());
            JsEngineSwitcher.Current.DefaultEngineName = ChakraCoreJsEngine.EngineName;

            var configuration = ReactSiteConfiguration.Configuration;
            configuration
                .SetReuseJavaScriptEngines(true)
                .SetAllowJavaScriptPrecompilation(true);
            configuration
                .SetStartEngines(2)
                .SetMaxEngines(2)
                .SetMaxUsagesPerEngine(0)
                .SetLoadBabel(false)
                .AddScriptWithoutTransform("Sample.js");
        }

	    [Benchmark]
	    public async Task Environment_CreateComponent()
	    {
	        var tk = new NoTextWriter();

	        var tasks = Enumerable.Range(0, 5).Select(
	            async x =>
	            {
	                var environment = AssemblyRegistration.Container.Resolve<IReactEnvironment>();
	                var component = environment.CreateComponent("HelloWorld", _testData, serverOnly: true);

	                component.RenderHtml(tk, renderServerOnly: true);
	                environment.ReturnEngineToPool();
	                await Task.Delay(0);
	            });

	        await Task.WhenAll(tasks);
        }

        [Benchmark]
		public async Task ZeroReact_CreateComponent()
		{
		    var tasks = Enumerable.Range(0, 5).Select(
		        async x =>
		        {
		            using (var scope = sp.CreateScope())
		            {
		                var reactContext = scope.ServiceProvider.GetRequiredService<IReactScopedContext>();

		                var component = reactContext.CreateComponent<ReactComponent>("HelloWorld");
		                component.Props = _testData;
		                component.ServerOnly = true;

		                await component.RenderHtml();
		            }
		        });

		    await Task.WhenAll(tasks);
		}
    }

    public class NoTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.Unicode;
    }
}
