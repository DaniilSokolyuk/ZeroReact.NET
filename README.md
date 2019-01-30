# [Work in progress] ZeroReact.NET
Almost zero allocation,  truly async 


``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.15063.1387 (1703/CreatorsUpdate/Redstone2)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3515626 Hz, Resolution=284.4444 ns, Timer=TSC
.NET Core SDK=2.1.500
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                      Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|---------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
|  ReactJSNet_CreateComponent | 40.53 ms | 0.5189 ms | 0.4854 ms | 312.5000 | 187.5000 | 187.5000 | 1452.47 KB |
|   ZeroReact_CreateComponent | 22.30 ms | 0.3976 ms | 0.4083 ms |        - |        - |        - |    1.78 KB |
