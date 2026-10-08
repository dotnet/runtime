# Fuzzing .NET libraries

This project contains fuzzing targets for various .NET libraries, as well as supporting code for generating OneFuzz deployments from them.
Targets are instrumented using [SharpFuzz](https://github.com/Metalnem/sharpfuzz), and ran using [libFuzzer](https://llvm.org/docs/LibFuzzer.html).

The runtime and fuzzing targets are periodically rebuilt and published to OneFuzz via [deploy-to-onefuzz.yml](../../../eng/pipelines/libraries/fuzzing/deploy-to-onefuzz.yml).

Useful links:
- [libFuzzer documentation](https://llvm.org/docs/LibFuzzer.html)
- [libFuzzer tutorial with examples](https://github.com/google/fuzzing/blob/master/tutorial/libFuzzerTutorial.md)
- [More SharpFuzz samples](https://github.com/Metalnem/dotnet-fuzzers)
- [OneFuzz documentation](https://aka.ms/onefuzz)

## Running locally

> [!NOTE]
> The instructions assume you are running on Windows as that is what the continuous fuzzing runs currently use.

### Prerequisites

Build the runtime with the desired configuration if you haven't already:
```cmd
./build.cmd clr+libs -rc release
```

> [!TIP]
> The `-rc release` configuration here builds runtime in `Release` and libraries in `Debug` mode.
> Automated fuzzing runs use a `Checked` runtime + `Debug` libraries configuration by default.
> You can use any configuration locally, but `Checked` is recommended when testing changes in `System.Private.CoreLib`.

Install the SharpFuzz command line tool:
```cmd
dotnet tool install --global SharpFuzz.CommandLine
```

### Fuzzing locally

Build the `DotnetFuzzing` fuzzing project. It is self-contained, so it will produce `DotnetFuzzing.exe` along with a copy of all required libraries.

```cmd
cd src/libraries/Fuzzing/DotnetFuzzing

dotnet build
```

Run `run.bat`, which will create separate directories for each fuzzing target, instrument the relevant assemblies, and generate a helper script for running them locally.
When iterating on changes, remember to rebuild the project again.

```cmd
dotnet build; .\run.bat
```

Start fuzzing by running the `local-run.bat` script in the folder of the fuzzer you are interested in.
```cmd
deployment/HttpHeadersFuzzer/local-run.bat
```

See the [libFuzzer options](https://llvm.org/docs/LibFuzzer.html#options) documentation for more information on how to customize the fuzzing process.
For example, here is how you can run the fuzzer against a `header-inputs` corpus directory for 10 minutes, running multiple instances in parallel:
```cmd
deployment/HttpHeadersFuzzer/local-run.bat header-inputs -max_total_time=600 -jobs=5
```

### Generating coverage reports

After letting the fuzzer run for a while, you can use the generated inputs to test code coverage.

```cmd
mkdir header-inputs
deployment/HttpHeadersFuzzer/local-run.bat header-inputs

.\collect-coverage.ps1 HttpHeadersFuzzer header-inputs
```

The HTML report can be opened from
```cmd
.\coverage-report\html\index.html
```

### Collecting source code coverage

Instal prerequisite tools if needed

```powershell
dotnet tool install --global dotnet-coverage
dotnet tool install --global dotnet-reportgenerator-globaltool
```

The following PowerShell commands use `KerberosPacLogonInfoFuzzer` as an example.

```powershell
$root = $(git rev-parse --show-toplevel)
$fuzzer = "KerberosPacLogonInfoFuzzer"
$build = "$root\artifacts\bin\DotnetFuzzing\Debug\net10.0\win-x64"
$deployment = "$root\src\libraries\Fuzzing\DotnetFuzzing\deployment"
$corpus = "$deployment\$fuzzer\corpus"
$coverageDir = "$root\artifacts\pac-coverage"

dotnet-coverage collect `
    --output "$coverageDir\coverage.cobertura.xml" --output-format cobertura `
    "$build\DotnetFuzzing.exe" $fuzzer $corpus
if ($LASTEXITCODE -ne 0) { throw "Seed coverage collection failed" }

reportgenerator `
    "-reports:$coverageDir\coverage.cobertura.xml" `
    "-targetdir:$coverageDir\html" `
    "-reporttypes:Html;TextSummary"
if ($LASTEXITCODE -ne 0) { throw "Report generation failed" }

Start-Process "$coverage\html\index.html"
```

`CollectCoverage.ps1` automates the process:

```powershell
.\CollectCoverage.ps1 KerberosPacLogonInfoFuzzer
```

It reads target assemblies from `OneFuzzConfig.json` and uses the deployment's `corpus` and
`generated-corpus` directories when present. For other corpus locations, pass
`-CorpusDirectories <directory1>,<directory2>`. Use `-BuildDirectory` for a different
uninstrumented build configuration or framework, and `-OutputDirectory` to select the report location.
The default report is `artifacts\fuzz-coverage\<fuzzer-name>\html\index.html`.
The script runs directly from the original build directory. It does not build, fuzz, or install tools. Replay measures saved inputs, not every transient fuzz input.

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

`KerberosPacLogonInfoFuzzer` targets `System.Net.Security.Fuzzing`, a helper assembly that links the production PAC logon-info parser source.
This allows the Unix-only parser to be instrumented on Windows without changing the product assembly.
Its initial corpus contains the valid PAC logon-info sample used by the parser's functional tests.

### Running against a sample input

The program accepts two arguments: the name of the fuzzer and the path to a sample input file / directory.
To run the HttpHeaders target against the `inputs` directory, use the following command:

```cmd
cd src/libraries/Fuzzing/DotnetFuzzing

dotnet run HttpHeadersFuzzer inputs
```

This can be useful when debugging a crash.
