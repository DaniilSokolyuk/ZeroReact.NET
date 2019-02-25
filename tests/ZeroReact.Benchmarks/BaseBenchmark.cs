using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
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
					config.StartEngines = Math.Max(Environment.ProcessorCount, 4);
					config.MaxEngines = Math.Max(Environment.ProcessorCount * 2, 8);
					config.MaxUsagesPerEngine = Environment.ProcessorCount;
					config.AllowJavaScriptPrecompilation = false;
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
				.SetAllowJavaScriptPrecompilation(false);
			configuration
				.SetStartEngines(Math.Max(Environment.ProcessorCount, 4))
				.SetMaxEngines(Math.Max(Environment.ProcessorCount * 2, 8))
				.SetMaxUsagesPerEngine(Environment.ProcessorCount)
				.SetLoadBabel(false)
				.AddScriptWithoutTransform("Sample.js");
		}
		
		public class NoTextWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Unicode;
		}
	}
}
