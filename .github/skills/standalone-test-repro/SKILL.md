---
name: standalone-test-repro
description: >
  Generate a standalone console application under artifacts/tmp from a managed
  test under src/tests so it can be run directly with corerun. USE FOR:
  "create a standalone repro for this runtime test", converting a CoreCLR test
  wrapper path such as JIT/.../TestName.cmd into a console app, or extracting a
  fully qualified xUnit test method from a merged test assembly, including when
  a CI scenario such as jitstress1 is specified. DO NOT USE FOR: adding a
  regression test to src/tests, library tests under src/libraries,
  NativeAOT-only tests, or tests whose behavior fundamentally requires a
  native, browser, mobile, multi-process, or external-service harness.
---

# Standalone Runtime Test Repro

Generate a source-level console application for one managed test under
`src/tests/`. Put it under `artifacts/tmp/standalone-test-repro/` and make it
runnable as:

```text
<CORE_ROOT>/corerun[.exe] <repro-output>/StandaloneTestRepro.dll
```

The output is an isolated repro, not a launcher that references the original
test assembly. Copy and adapt the test's source and required helpers so the
repro remains understandable and editable.

## Scope Gate

This skill supports managed CoreCLR tests that can meaningfully execute through
`corerun`. Before extraction, inspect the project and generated wrapper when
available.

Stop and explain the limitation instead of producing a misleading repro if the
test fundamentally depends on any of these:

- A native executable as the primary test process
- NativeAOT, Mono, browser, WASI, Android, iOS, tvOS, or MacCatalyst hosting
- A profiler, debugger, COM server, custom host, or externally installed tool
  that cannot be represented by a console app plus local files
- A coordinated multi-process harness where invoking one managed method changes
  the behavior under test
- Network services, credentials, privileged machine configuration, or other
  unavailable external infrastructure

Local native libraries, data files, child processes, and environment variables
are allowed when they can be copied into the repro directory and their use is
part of the behavior being reproduced.

## Workflow

### 1. Establish the Runtime Test Environment

Read:

- `src/tests/README.md`
- The nearest applicable `README.md` files between the resolved test directory
  and `src/tests/`
- `.github/instructions/tests.instructions.md`
- `.github/instructions/csharp.instructions.md`
- Any area-specific instruction file that applies to the resolved source

Use the `build-and-test` skill before invoking any build or test command.

Determine the intended OS, architecture, and configuration from the supplied
path or CI context. Otherwise default to the current host, `x64`, and `Release`.
Prefer an existing matching `CORE_ROOT`. If it does not exist, generate the
matching Core_Root layout using the runtime-test instructions from the
`build-and-test` skill.

Do not modify files under `src/tests/`.

### 2. Resolve the Test

Preserve the original identifier for the final report, then load exactly one
resolution sub-skill:

- For a path ending in `.cmd` or `.sh`, read and follow
  [`../standalone-test-repro-cmd/SKILL.md`](../standalone-test-repro-cmd/SKILL.md).
- For a fully qualified test method name, read and follow
  [`../standalone-test-repro-method/SKILL.md`](../standalone-test-repro-method/SKILL.md).

If the input does not clearly match either form, use the `ask_user` tool. The
sub-skill must resolve the exact source method, owning project, and effective
test invocation before continuing here.

Capture an explicitly requested test scenario separately from the test
identifier. After resolving the owning project, read and follow
[`../test-scenario-env/SKILL.md`](../test-scenario-env/SKILL.md) when a scenario
was supplied. Pass it the canonical scenario name, owning `.csproj`, target OS,
architecture, configuration, runtime flavor, and output path
`artifacts/tmp/standalone-test-repro/<sanitized-test-name>/.env`.

### 3. Capture the Original Test Contract

Before editing the repro, record everything that can affect behavior:

- Source files and helper types used by the test
- Project properties including `Optimize`, `AllowUnsafeBlocks`,
  `CheckForOverflowUnderflow`, `DefineConstants`, `LangVersion`,
  `PlatformTarget`, and nullable context
- `RuntimeHostConfigurationOption` values
- Evaluated `CLRTestEnvironmentVariable` names and values from the owning
  `.csproj`, including applicable imported properties and conditions
- Target-specific `CLRTestBatchEnvironmentVariable` and
  `CLRTestBashEnvironmentVariable` items
- Evaluated `CLRTestBatchPreCommands`, `CLRTestBashPreCommands`,
  `CLRTestBatchPostCommands`, and `CLRTestBashPostCommands`
- The requested scenario and its evaluated environment from
  `src/tests/Common/testenvironment.proj`, when specified
- `CLRTestExecutionArguments`
- Expected exit code, which defaults to `100` for runtime tests
- Working directory, input files, native libraries, and other copied assets
- Architecture, OS, configuration, and runtime flavor guards

This list is a mandatory minimum, not an exhaustive allowlist. Inspect all
evaluated project metadata and the generated wrapper for additional setup,
launch, cleanup, or environment behavior before declaring the contract
complete.

When a local wrapper exists, treat its effective command and environment as the
ground truth. Project files are the fallback when no wrapper has been generated.

### 4. Create the Repro

Create:

```text
artifacts/tmp/standalone-test-repro/<sanitized-test-name>/
```

Use a stable, filesystem-safe name derived from the type and method or wrapper
stem. If that directory already exists, inspect it first. Replace only files
owned by a previous run of this skill; never recursively delete an unresolved
or broad path.

The directory must contain at least:

```text
StandaloneTestRepro.csproj
Program.cs
```

If a scenario was requested or the owning project defines any effective common
or target-specific environment items, also create:

```text
.env
```

Additional `.cs` files and local assets are allowed when they improve clarity
or are required for faithful behavior.

Use this project as the starting point, adding only properties required by the
original test:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$(NetCoreAppCurrent)</TargetFramework>
    <AssemblyName>StandaloneTestRepro</AssemblyName>
    <UseAppHost>false</UseAppHost>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>
```

Follow these extraction rules:

- When a scenario was requested, the `test-scenario-env` skill owns `.env`
  generation. Do not independently reconstruct or append scenario settings.
- Write each evaluated `CLRTestEnvironmentVariable` and applicable
  `CLRTestBatchEnvironmentVariable` or `CLRTestBashEnvironmentVariable` to
  `.env` as `NAME=VALUE`, one variable per line. Resolve MSBuild properties and
  item metadata for the selected OS, architecture, configuration, and runtime
  flavor; do not copy unevaluated expressions such as `$(SomeProperty)`.
- Encode an empty value as `NAME=''`; a bare `NAME=` before another entry is
  not safe because CoreRun's parser skips the following newline while looking
  for a value. Use dotenv quoting or escaping when a value contains whitespace,
  `#`, quotes, backslashes, newlines, or variable expansion syntax. Follow the
  syntax supported by CoreRun's dotenv parser in
  `src/coreclr/hosts/corerun/dotenv.cpp`.
- Include only environment variables defined by the test project and its
  applicable imports. Do not copy the ambient shell environment or CI secrets
  into `.env`.
- Merge scenario variables first, selected-target Batch/Bash environment items
  second, and common `CLRTestEnvironmentVariable` items last, matching generated
  wrapper order. A later value replaces an earlier value with the same name.
- If a local generated wrapper and the evaluated project disagree, use the
  wrapper's effective values and investigate why before continuing.
- Copy the relevant test logic, not the whole merged assembly.
- Preserve the code shape that may trigger the failure. Do not simplify
  control flow, types, constants, inlining attributes, optimization settings,
  or concurrency merely because a smaller version appears equivalent.
- Include only transitively required helper types and source files.
- Remove xUnit and runtime test-framework dependencies where practical.
  Replace assertions with small local checks that throw an exception containing
  expected and actual values.
- Prefer source-level local replacements for simple `TestLibrary` helpers.
  Do not reference the original test project or its output assembly.
- Preserve required setup and cleanup with `try/finally` or `using`; do not
  silently omit fixtures or disposal.
- Preserve applicable batch or Bash pre/post commands by translating their
  effects into explicit repro setup, launch, and cleanup steps. Stop if those
  effects cannot be represented faithfully by a standalone console app plus
  local files.
- Invoke instance methods on a correctly initialized instance.
- Await `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>` results.
- For a theory with several rows, print the row before invoking it and stop on
  the first failure.
- Preserve unsafe code, P/Invoke declarations, generated code, and conditional
  compilation required by the failing path.
- Copy required local data and native binaries into the build output using
  project items such as `Content` with `CopyToOutputDirectory`. Keep their
  relative paths consistent with the original test.
- Do not download packages, binaries, or test data from issue links. Use only
  trusted repository sources and already-built repository artifacts.

The console entry point should return the original success code, normally
`100`, and allow failures to remain visible:

```csharp
internal static class Program
{
    private static int Main()
    {
        RunTest();

        return 100;
    }
}
```

Use `static async Task<int> Main()` when asynchronous invocation is required.
Do not catch exceptions merely to convert them into success-shaped output. A
catch is acceptable only to add test-case context before rethrowing.

### 5. Build

Build with the repository's bootstrapped SDK from the repository root. Use a
dedicated output directory inside the repro:

```text
.\dotnet.cmd build artifacts\tmp\standalone-test-repro\<name>\StandaloneTestRepro.csproj -c Release -o artifacts\tmp\standalone-test-repro\<name>\out
```

On Unix, use `./dotnet.sh` and Unix path separators.

If the original test requires a different optimization or configuration, use
that configuration consistently and explain it in the final report. Fix all
compile errors in the repro; do not work around them by referencing the
original test assembly.

### 6. Run with CoreRun

Run from the repro output directory so relative paths behave consistently.
Apply the captured environment variables, host configuration options, and test
arguments.

Windows:

```powershell
Push-Location artifacts\tmp\standalone-test-repro\<name>\out
& "$env:CORE_ROOT\corerun.exe" -e ..\.env .\StandaloneTestRepro.dll
$exitCode = $LASTEXITCODE
Pop-Location
```

Unix:

```bash
(
  cd artifacts/tmp/standalone-test-repro/<name>/out
  "$CORE_ROOT/corerun" -e ../.env ./StandaloneTestRepro.dll
)
exit_code=$?
```

Include `-e ../.env` only when the file was generated. If
`RuntimeHostConfigurationOption` items were present, translate them to
`corerun -p Name=Value` arguments before `-e`. Pass test arguments after the
managed DLL.

Success normally exits `100`; a CI failure repro may intentionally crash or
return another value. Record the actual exit code and meaningful output. Do not
claim the CI failure reproduced unless the observed behavior matches it.

### 7. Validate Fidelity

Before completing:

1. Compare the repro invocation with the original wrapper or project contract.
2. Confirm the repro does not reference the original test assembly.
3. If `.env` was generated, compare every entry with the effective scenario,
   target-specific environment items, and common `CLRTestEnvironmentVariable`
   items, then confirm the `corerun` command uses it.
4. Confirm all required local files exist in `out`.
5. Run the repro with the selected `corerun`.
6. If practical, run the original test with the same runtime and environment
   and compare the relevant behavior.
7. Inspect `git status` and ensure only the new skill or explicitly requested
   repository changes are tracked; repro files under `artifacts/` should remain
   untracked or ignored.

If the standalone app builds and runs but does not reproduce the reported
failure, keep the app and state that result plainly. Include any configuration
from CI that was unavailable locally.

## Final Response

Lead with the result and include:

- The resolved source method and owning project
- The repro directory and built DLL
- The exact `corerun` command, including required environment variables and
  `-p` options
- The generated `.env` path and variable names, without unnecessarily echoing
  sensitive values
- The canonical scenario name, when one was requested
- The observed exit code and whether it matched the reported failure
- Any intentionally retained local native or data dependencies

Do not paste the full generated source unless the user asks for it.
