# ZeroReact.NET
Almost zero allocation,  truly async 


``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.15063.1387 (1703/CreatorsUpdate/Redstone2)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=3515626 Hz, Resolution=284.4444 ns, Timer=TSC
.NET Core SDK=2.1.500
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                      Method |      Mean |     Error |    StdDev |     Gen 0 |     Gen 1 |    Gen 2 |  Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|---------:|-----------:|
|       React_CreateComponent | 110.48 ms | 2.1075 ms | 2.2550 ms | 1312.5000 | 1187.5000 | 937.5000 | 6567.07 KB |
|   ZeroReact_CreateComponent |  50.62 ms | 0.9509 ms | 0.9765 ms |         - |         - |        - |   19.03 KB |
