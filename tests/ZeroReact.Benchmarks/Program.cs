using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace ZeroReact.Benchmarks
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
            //var tt = new WebSimulateBenchmark();
            //tt.Setup();

            //for (int i = 0; i < 10000000; i++)
            //{
            //    await tt.ZeroReact_CreateComponent();
            //}


            BenchmarkRunner.Run<WebSimulateBenchmark>();
            //BenchmarkRunner.Run<SingleComponentBenchmark>();
            Console.ReadKey();
		}
	}
}
