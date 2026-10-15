## About

Provides a library for verifying .NET intermediate language (IL) against the ECMA-335 specification. For a command-line interface, use the [dotnet-ilverify](https://www.nuget.org/packages/dotnet-ilverify/) tool.

## Thread safety, reader lifetime and resolver requirements

- A `Verifier` is not thread-safe. Do not call its methods, or enumerate sequences it returned, on more than one thread at the same time. Calls that do not overlap may come from different threads.
- `Verify` returns a lazily evaluated sequence. Verification, including the `IResolver` calls it needs, happens while the sequence is enumerated. `SetSystemModuleName` calls the `IResolver` immediately.
- A `Verifier` creates a module for each `PEReader` it uses, whether passed to `Verify` or returned by `IResolver`. It may keep reading from that reader after the enumeration that first used it has completed. Keep those readers undisposed, and the memory they read alive and unchanged, until you no longer use the `Verifier` or the sequences it returned. The `Verifier` does not dispose them.
- `IResolver` must return the same `PEReader` instance each time it is asked for the same assembly or module. The verifier identifies modules by `PEReader` instance. A second instance for the same assembly becomes a second module with distinct types, which can produce wrong verification results. That includes the assembly being verified: if the verifier also asks for it by name, return the instance that is passed to `Verify`. For example, when `System.Private.CoreLib` is both the system module and the assembly being verified, `SetSystemModuleName` asks for it by name before verification starts.
