# ZeroReact.NET
Almost zero allocations and truly async alternative to ReactJS.NET




# Benchmarks

## Render Single Component
``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.15063.1387 (1703/CreatorsUpdate/Redstone2)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3515625 Hz, Resolution=284.4444 ns, Timer=TSC
.NET Core SDK=2.2.104
  [Host] : .NET Core 2.2.2 (CoreCLR 4.6.27317.07, CoreFX 4.6.27318.02), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                 Method |     Mean |     Error |    StdDev | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|----------------------- |---------:|----------:|----------:|------------:|------------:|------------:|--------------------:|
| ZeroReact_RenderSingle | 22.18 ms | 0.4312 ms | 0.4614 ms |           - |           - |           - |             1.05 KB |
|   ReactJs_RenderSingle | 24.52 ms | 5.4801 ms | 5.6277 ms |     62.5000 |           - |           - |          1311.62 KB |

## Web Simulation
``` ini

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.15063.1387 (1703/CreatorsUpdate/Redstone2)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3515625 Hz, Resolution=284.4444 ns, Timer=TSC
.NET Core SDK=2.2.104
  [Host] : .NET Core 2.2.2 (CoreCLR 4.6.27317.07, CoreFX 4.6.27318.02), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                   Method |     Mean |     Error |    StdDev |   Median | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------- |---------:|----------:|----------:|---------:|------------:|------------:|------------:|--------------------:|
| ReactJSNet_WebSimulation | 366.7 ms | 22.825 ms | 67.299 ms | 326.3 ms |   4000.0000 |   3000.0000 |   3000.0000 |         24437.02 KB |
|  ZeroReact_WebSimulation | 118.1 ms |  2.359 ms |  5.226 ms | 117.2 ms |    400.0000 |           - |           - |            15.65 KB |


