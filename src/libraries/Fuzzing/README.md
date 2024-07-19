# Fuzzing .NET libraries

This project contains fuzzing targets for various .NET libraries, as well as supporting code for generating OneFuzz deployments from them.
Targets are instrumented using [SharpFuzz](https://github.com/Metalnem/sharpfuzz), and ran using [libFuzzer](https://llvm.org/docs/LibFuzzer.html).

The runtime and fuzzing targets are periodically rebuilt and published to OneFuzz via [deploy-to-onefuzz.yml](/eng/pipelines/libraries/fuzzing/deploy-to-onefuzz.yml).

Useful links:
- [libFuzzer documentation](https://llvm.org/docs/LibFuzzer.html)
- [libFuzzer tutorial with examples](https://github.com/google/fuzzing/blob/master/tutorial/libFuzzerTutorial.md)
- [More SharpFuzz samples](https://github.com/Metalnem/dotnet-fuzzers)
- [OneFuzz documentation](https://aka.ms/onefuzz)

## Running locally

> [!NOTE]
> The instructions assume you are running on Windows as that is what the continuous fuzzing runs currently use.

### Prerequisites

Build the runtime with the following arguments:
```cmd
./build.cmd clr+libs+packs+host -rc Checked -c Debug
```
and install the SharpFuzz command line tool:
```cmd
dotnet tool install --global SharpFuzz.CommandLine
```

> [!TIP]
> The project uses a checked runtime + debug libraries configuration by default.
> If you want to use a different configuration, make sure to also adjust the artifact paths in `nuget.config`.

### Fuzzing locally

The `prepare-onefuzz` command will create separate directories for each fuzzing target, instrument the relevant assemblies, and generate a helper script for running them locally.
Note that this command must be ran on the published artifacts (won't work with `dotnet run`).

```cmd
cd src/libraries/Fuzzing/DotnetFuzzing

dotnet publish -o publish; publish/DotnetFuzzing.exe prepare-onefuzz deployment
```

You can now start fuzzing by running the `local-run.bat` script in the folder of the fuzzer you are interested in.
```cmd
deployment/HttpHeadersFuzzer/local-run.bat
```

See the [libFuzzer options](https://llvm.org/docs/LibFuzzer.html#options) documentation for more information on how to customize the fuzzing process.
For example, here is how you can run the fuzzer against a `header-inputs` corpus directory for 10 minutes, running multiple instances in parallel:
```cmd
deployment/HttpHeadersFuzzer/local-run.bat header-inputs -timeout=30 -max_total_time=600 -jobs=5
```

## Creating a new fuzzing target

To create a new fuzzing target, you need to create a new class that implements the `IFuzzer` interface.
See existing implementations in the `Fuzzers` directory for reference.

As an example, let's test that `IPAddress.TryParse` never throws on invalid input, and doesn't access any bytes after the end of `bytes`:
```c#
internal sealed class IPAddressFuzzer : IFuzzer
{
    public string[] TargetAssemblies => ["System.Net.Primitives"]; // Assembly where IPAddress lives
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        // PooledBoundedMemory is a helper class that ensures reading past the end of the buffer will trigger an access violation.
        using var chars = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(bytes), PoisonPagePlacement.After);

        _ = IPAddress.TryParse(chars.Span, out _);
    }
}
```

- `TargetAssemblies` is a list of assemblies where the tested code lives and that must be instrumented.
- `TargetCoreLibPrefixes` is the same, but for types/namespaces in `System.Private.CoreLib`.
- `FuzzTarget` is the logic that the fuzzer will run for every test input. It should exercise code from the target assemblies.

Once you've created the new target, you can follow instructions above to run it locally.
Targets are discovered via reflection, so they will automatically become available for local runs and continuous fuzzing in CI.

### Running against a sample input

The program accepts two arguments: the name of the fuzzer and the path to a sample input file / directory.
To run the HttpHeaders target against the `inputs` directory, use the following command:

```cmd
cd src/libraries/Fuzzing/DotnetFuzzing

dotnet run HttpHeadersFuzzer inputs
```

This can be useful when debugging a crash, or running the fuzzer over existing inputs to collect code coverage.
