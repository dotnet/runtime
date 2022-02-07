# Limitations of Native AOT Runtime

The native AOT .NET runtime form factor comes with a number of fundamental limitations and compatibility issues. The reasons 
behind them are discussed in length in 
the [.NET Runtime Form Factors](https://github.com/dotnet/designs/blob/main/accepted/2020/form-factors.md) roadmap.

The key limitations include:

- No dynamic loading (e.g. `Assembly.LoadFile`)
- No runtime code generation (e.g. `System.Reflection.Emit`)
- No C++/CLI, no built-in COM and WinRT interop support
- No [unconstrained reflection](reflection-in-aot-mode.md)
