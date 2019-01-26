``` ini

BenchmarkDotNet=v0.10.14, OS=macOS 10.14 (18A391) [Darwin 18.0.0]
Intel Core i7-3720QM CPU 2.60GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.103
  [Host] : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                      Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|---------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
| Environment_CreateComponent | 39.22 ms | 0.6728 ms | 0.6293 ms | 312.5000 | 250.0000 | 250.0000 | 1313.84 KB |
|   ZeroReact_CreateComponent | 37.29 ms | 0.4250 ms | 0.3549 ms |        - |        - |        - |    2.15 KB |