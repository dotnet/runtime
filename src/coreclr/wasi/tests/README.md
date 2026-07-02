# CoreCLR-WASI test harness

Two ways to validate CoreCLR-WASI changes locally:

1. **Existing CoreCLR runtime tests** under `src/tests/` — the full
   `JIT`, `baseservices`, etc. trees. Built with the standard
   `src/tests/build.sh` and run via [`run.cs`](run.cs), a file-based
   C# app that discovers `*.sh` wrappers, launches them in parallel,
   tracks pass/fail/timeout against a known-failures list, and
   summarises results. **This is the primary harness.**

2. **Hand-written smoke tests** under `smoke/` — tiny `Main`s exercising
   one runtime behaviour at a time. Driven by `run-tests.sh`. Useful
   for very early bring-up scenarios where the `src/tests/`
   infrastructure isn't yet usable (e.g. validating that `corerun.wasm`
   itself boots before any test layout exists).

## Prerequisites

* A working CoreCLR-WASI build:
  ```sh
  ./build.sh -s clr+libs -c Release -os wasi /p:TestAssemblies=false
  ```
* `wasmtime` — auto-downloaded by `run-tests.sh`, or run
  `./.dotnet/dotnet build src/coreclr/wasi/tests/provision-wasmtime.proj`
  to fetch it without running any tests. `run.cs` looks for it via
  `$WASMTIME`, then `~/.wasmtime/bin/wasmtime`, then `$PATH`.

## Running CoreCLR runtime tests (recommended)

```sh
# Stage the Core_Root layout (needed once per build).
./src/tests/build.sh -GenerateLayoutOnly wasi Release

# Build a tree of tests as per-test standalone executables. The per-test
# .sh wrappers are only emitted when BuildAllTestsAsStandalone=true.
BuildAllTestsAsStandalone=true ./src/tests/build.sh -tree JIT/jit64 \
    wasi Release -priority1 -skipgeneratelayout -skipnative

# Run them under wasmtime — 8 jobs, 60s per-test timeout, with the
# committed known-failures list:
./.dotnet/dotnet run src/coreclr/wasi/tests/run.cs -- \
    --tree=JIT/jit64 --jobs=8 --timeout=60 \
    --known-failures=src/coreclr/wasi/tests/known-failures.txt
```

`run.cs` options:

| Flag | Default | Description |
|------|---------|-------------|
| `--tree=<rel>`         | (all)             | Limit to wrappers under `<test-root>/<rel>` |
| `--jobs=N`             | half of cpu count | Max parallel test processes |
| `--timeout=SECONDS`    | 300               | Per-test wall-clock cap |
| `--known-failures=PATH`| (none)            | File of tests expected to fail or time out, one path per line, `# comments` ok |
| `--list`               |                   | Print discovered test wrappers and exit |

Env overrides: `CONFIG` (`Release`/`Debug`/`Checked`), `TESTROOT`,
`CORE_ROOT`, `WASMTIME` (path to a wasmtime binary).

The runner exits 0 only when there are no unexpected failures or
timeouts. Tests that pass while listed in the known-failures file are
reported as `unexpected pass` so the list stays honest.

### Debugging a single failing test

The generated `.sh` wrappers run cleanly under raw bash, but the
wasmtime stderr output is hard to read when corerun aborts. For a clean
managed stack trace use the `nesm-wasm` MCP tools against the staged
Core_Root (same preopens the wrapper uses — nesm-wasm shows the
managed exception chain plus the wasm trap without truncation):

```text
wasm_run \
  path=artifacts/tests/coreclr/wasi.wasm.Release/Tests/Core_Root/corerun \
  args=["<TestName>.dll"] \
  env={CORE_ROOT: "/core", DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: "true"} \
  preopens={".": "<test-bin-dir>", "/core": "<Core_Root>"}
```

`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` is required because the
staged runtime pack does not bundle ICU data — without it any code
path that reads a CoreLib resource string (e.g.
`ArgumentException.Message`) hits an unrecoverable `FailFast` from the
`GlobalizationMode+Settings` cctor.

## Hand-written smoke suite

```sh
# Build + run all smoke tests:
./src/coreclr/wasi/tests/run-tests.sh

# Run one test by name:
./src/coreclr/wasi/tests/run-tests.sh HelloWorld

# List available tests:
./src/coreclr/wasi/tests/run-tests.sh --list
```

Each smoke test is a managed executable that prints
**`WASI-SMOKE-PASS:<name>`** on stdout when its scenario completes
successfully. The runner scans stdout for that sentinel; a missing
sentinel is a failure regardless of exit code. (This pre-dates the
`CORERUN_EXIT_CODE` marker now emitted by corerun, which makes
exit-code-based assertions viable for the `src/tests/` runner.)

### Adding a smoke test

1. Create a directory under `smoke/`, e.g. `smoke/MyTest/`.
2. Add a `MyTest.csproj` (copy an existing one — just
   `<OutputType>Exe</OutputType>` and `<TargetFramework>net11.0</TargetFramework>`).
3. Add a `Program.cs` whose entry point prints
   `WASI-SMOKE-PASS:MyTest` on its own line when the scenario succeeds.
4. Re-run `./src/coreclr/wasi/tests/run-tests.sh` — the new test is
   picked up automatically.

### Known-broken smoke scenarios

Smoke tests that currently fail on WASI corerun live under
[`known-broken/`](known-broken/README.md) and are excluded from the
default smoke suite to keep the green baseline clean. To run one
anyway:

```sh
KEEP_STAGING=1 ./src/coreclr/wasi/tests/run-tests.sh <Name>
```
