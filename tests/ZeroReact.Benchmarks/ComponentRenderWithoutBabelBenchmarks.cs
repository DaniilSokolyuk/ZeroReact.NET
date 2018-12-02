using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace ZeroReact.Benchmarks
{
	[MemoryDiagnoser]
	[InProcess]
    public class ComponentRenderWithoutBabelBenchmarks : ComponentRenderBenchmarksBase
	{
		[GlobalSetup]
		public void Setup()
		{
			PopulateTestData();
			RegisterZeroReact();
            RegisterReact();
		}
	}
}
