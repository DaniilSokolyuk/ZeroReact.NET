``` ini

BenchmarkDotNet=v0.10.14, OS=macOS 10.14 (18A391) [Darwin 18.0.0]
Intel Core i7-3720QM CPU 2.60GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.103
  [Host] : .NET Core 2.2.1 (CoreCLR 4.6.27207.03, CoreFX 4.6.27207.03), 64bit RyuJIT

Job=InProcess  Toolchain=InProcessToolchain  

```
|                      Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|---------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
| Environment_CreateComponent | 40.32 ms | 0.7962 ms | 0.9478 ms | 312.5000 | 250.0000 | 250.0000 | 1314.33 KB |
|   ZeroReact_CreateComponent | 37.39 ms | 0.6337 ms | 0.5927 ms |        - |        - |        - |    2.09 KB |
