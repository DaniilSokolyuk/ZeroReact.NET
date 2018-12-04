using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using React;

namespace ZeroReact.Benchmarks
{
	[MemoryDiagnoser]
	[InProcess]
    public abstract class BaseBenchmark
	{
		[GlobalSetup]
		public void Setup()
		{
			PopulateTestData();
			RegisterZeroReact();
            RegisterReact();
		}
		
		protected JObject _testData = JObject.FromObject(new Dictionary<string, string>() { ["name"] = "Tester" });

		protected IServiceProvider sp;

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
		
		public class NoTextWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Unicode;
		}
	}
}
