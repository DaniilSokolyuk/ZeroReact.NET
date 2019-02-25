# ZeroReact.NET [![Build status](https://ci.appveyor.com/api/projects/status/9v382r4s13dn91d9?svg=true)](https://ci.appveyor.com/project/DaniilSokolyuk/zeroreact-net) [![NuGet Version](https://img.shields.io/nuget/v/ZeroReact.AspNetCore.svg)](https://www.nuget.org/packages/ZeroReact.AspNetCore/) 

Almost zero allocations and truly async alternative to [ReactJS.NET](https://github.com/reactjs/React.NET)

* Not supported On-the-fly JSX to JavaScript compilation (only AddScriptWithoutTransform for performance reasons)
* Not supported render functions (ReactJS.NET v4 feature) (planned)

# Migration from ReactJS.NET
1. Make sure you use @await Html.PartialAsync and @await Html.RenderAsync on cshtml views, synchronous calls can deadlock application 
2. Replace 
* @Html.React to @await Html.React
* @Html.ReactWithInit to @await ReactWithInitAsync
* @Html.ReactRouter to @await Html.ReactRouterAsync
3. Register ZeroReact in service collection, example [here](https://github.com/DaniilSokolyuk/ZeroReact.NET/blob/2795b6d2dcf5b3e902ebbd7b21b6470462a182ac/src/ZeroReact.Sample.Webpack.AspNetCore/Startup.cs#L19)
4. [Install native implementations of ChakraCore](https://github.com/Taritsyn/JavaScriptEngineSwitcher/wiki/ChakraCore), without JavaScriptEngineSwitcher.ChakraCore, ZeroReact contains modified version of JavaScriptEngineSwitcher.ChakraCore for performance reasons


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


