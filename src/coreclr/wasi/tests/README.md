# CoreCLR-WASI smoke tests

A minimal local harness for running managed test assemblies under
`corerun.wasm` on `wasmtime`. Intended for fast iteration when changing
CoreCLR code that affects the WASI target.

This is **not** a substitute for the full `src/tests/` CoreCLR runtime
test tree. It's a tiny smoke suite that grows by accretion as new
WASI-specific changes need regression coverage.

## Prerequisites

* A working CoreCLR-WASI build:
  ```sh
  ./build.sh -s clr+libs -c Release -os wasi /p:TestAssemblies=false
  ```
* `wasmtime` — auto-downloaded by the harness using the version pinned
  in `src/mono/wasi/wasmtime-version.txt` (the same provisioning target
  the Mono WASI tests use, `eng/testing/wasi-provisioning.targets`).

## Running

```sh
# Build + run all smoke tests:
./src/coreclr/wasi/tests/run-tests.sh

# Run one test by name:
./src/coreclr/wasi/tests/run-tests.sh HelloWorld

# List available tests:
./src/coreclr/wasi/tests/run-tests.sh --list
```

Each test is a managed executable that prints **`WASI-SMOKE-PASS:<name>`**
on its own line to stdout when the scenario completes successfully. The
runner scans stdout for the sentinel; a missing sentinel is a failure,
regardless of exit code.

> Why a sentinel and not the conventional exit-100? Today `corerun.cpp`'s
> `corerun_shutdown` overwrites the managed `Main`'s return value with
> `latched_exit_code` from `coreclr_shutdown_2` before propagating to the
> process, so the WASI host always sees `Environment.ExitCode` (default
> 0) rather than the value `Main` returned. Tracked as a separate
> follow-up; once fixed, the runner can switch back to exit-code-based
> assertions.

## Adding a test

1. Create a directory under `smoke/`, e.g. `smoke/MyTest/`.
2. Add a `MyTest.csproj` (copy an existing one — just `<OutputType>Exe</OutputType>`
   and `<TargetFramework>net11.0</TargetFramework>`).
3. Add a `Program.cs` whose entry point prints
   `WASI-SMOKE-PASS:MyTest` on its own line when the scenario succeeds.
   Return any int — the runner currently ignores the exit code.
4. Re-run `./src/coreclr/wasi/tests/run-tests.sh` — the new test is
   picked up automatically.

Keep tests small, hermetic, and focused on a single runtime behaviour
(EH, GC, finalization, interop, …). The point of the suite is to make a
WASI regression obvious in seconds.

## Debugging a failing test

When a smoke test fails, the runner prints the wasmtime backtrace and
leaves a hint about staging. Best next step is to attach the
[`nesm-wasm`](https://github.com/lewing/nesm) MCP tools to the staging
directory's `corerun` — they give cleaner stderr capture (wasmtime can
truncate / reorder) and source-level DWARF debugging:

```sh
KEEP_STAGING=1 ./src/coreclr/wasi/tests/run-tests.sh <Name>
# note the printed staging path, then in nesm-wasm:
#   wasm_run path=<staging>/corerun args=["<Name>.dll"] \
#            env={CORE_ROOT: "/", DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: "true"} \
#            preopens={".": "<staging>"}
```

The runner sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` by default
because the staged runtime pack does not bundle ICU data — without it
any code path that reads a CoreLib resource string (e.g.
`ArgumentException.Message`) hits an unrecoverable `FailFast` from the
`GlobalizationMode+Settings` cctor.
