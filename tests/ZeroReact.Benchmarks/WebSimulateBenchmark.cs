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
		private readonly NoTextWriter tk = new NoTextWriter();

	    //[Benchmark]
	    //public async Task Environment_CreateComponent()
	    //{

	    //    var tasks = Enumerable.Range(0, 5).Select(
	    //        async x =>
	    //        {
	    //            var environment = AssemblyRegistration.Container.Resolve<IReactEnvironment>();
	    //            var component = environment.CreateComponent("HelloWorld", _testData, serverOnly: true);

	    //            component.RenderHtml(tk, renderServerOnly: true);
	    //            environment.ReturnEngineToPool();
	    //            await Task.Delay(0);
	    //        });

	    //    await Task.WhenAll(tasks);
     //   }

        [Benchmark]
		public async Task ZeroReact_CreateComponent()
		{
		    var tasks = Enumerable.Range(0, 5).Select(
		        async x =>
		        {
		            using (var scope = sp.CreateScope())
		            {
		                var reactContext = scope.ServiceProvider.GetRequiredService<IReactScopedContext>();

		                var component = reactContext.CreateComponent<ZeroReact.Components.ReactComponent>("HelloWorld");
		                component.Props = _testData;
		                component.ServerOnly = true;

		                await component.RenderHtml();
		            }
		        });

		    await Task.WhenAll(tasks);
		}
    }
}
