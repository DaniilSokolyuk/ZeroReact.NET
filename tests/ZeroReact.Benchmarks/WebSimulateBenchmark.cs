using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using React;

namespace ZeroReact.Benchmarks
{
	public class WebSimulateBenchmark : BaseBenchmark
	{
		private readonly NoTextWriter tk = new NoTextWriter(); //TODO: simulate PagedBufferedTextWriter

        [Benchmark]
        public async Task ZeroReact_WebSimulation()
        {
            var tasks = Enumerable.Range(0, 10).Select(async x =>
            {
                using (var scope = sp.CreateScope())
                {
                    foreach (var ind in Enumerable.Range(0, 25))
                    {
                        var reactContext = scope.ServiceProvider.GetRequiredService<IReactScopedContext>();

                        var component = reactContext.CreateComponent<ZeroReact.Components.ReactComponent>("HelloWorld");
                        component.Props = _testData;

                        await component.RenderHtml();

                        component.WriteOutputHtmlTo(tk);
                    }

                    scope.ServiceProvider.GetRequiredService<IReactScopedContext>().GetInitJavaScript(tk);
                }
            });

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public void ReactJSNet_WebSimulation()
        {
            Parallel.For(0, 10, i =>
            {
                var environment = AssemblyRegistration.Container.Resolve<IReactEnvironment>();
                foreach (var ind in Enumerable.Range(0, 25))
                {
                    var component = environment.CreateComponent("HelloWorld", _testData);

                    component.RenderHtml(tk);
                    environment.ReturnEngineToPool();
                }

                environment.GetInitJavaScript(tk);
                ((IDisposable) environment).Dispose();
            });
        }
    }
}
