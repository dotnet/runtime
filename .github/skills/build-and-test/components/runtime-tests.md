# Runtime Tests

Covers `src/tests/`. Confirm the component's [baseline sentinel](../SKILL.md#baseline-sentinels)
first — running an individual test additionally requires the Core_Root layout.

Subdirectories under `src/tests/` may contain `README.md` files with area-specific guidance
(e.g. EventPipe test patterns).

**Build all tests:**

```bash
./build.sh clr+libs -lc release -rc checked
./src/tests/build.sh checked
./src/tests/run.sh checked
```

**Build a single test project** (path is relative to the repo root):

```bash
# Use -priority1 ("-Priority 1" on Windows) for tests with <CLRTestPriority>1</CLRTestPriority>,
# otherwise the build silently reports "0 test projects" and builds nothing.
src/tests/build.sh -Test tracing/eventpipe/eventsvalidation/GCEvents.csproj x64 Release -priority1
```

Other useful flags (run `src/tests/build.sh -h` for the full list):

| Flag | Description |
|------|-------------|
| `-Test <path>` | Build one project |
| `-Dir <path>` | Build all projects in a directory |
| `-Tree <path>` | Build a subtree recursively |
| `-priority1` (`-Priority 1` on Windows) | Include priority 1 tests |
| `-GenerateLayoutOnly` | Generate Core_Root layout only |

**Generate Core_Root layout** (required before running individual tests):

```bash
src/tests/build.sh -GenerateLayoutOnly x64 Release
```

**Run a single test:**

```bash
export CORE_ROOT=$(pwd)/artifacts/tests/coreclr/<os>.x64.Release/Tests/Core_Root
cd artifacts/tests/coreclr/<os>.x64.Release/<test-path>/
$CORE_ROOT/corerun <TestName>.dll
# Exit code 100 = pass, any other value = fail.
```

On Windows: `$env:CORE_ROOT = "$PWD\artifacts\tests\coreclr\windows.x64.Release\Tests\Core_Root"`,
then run `& "$env:CORE_ROOT\corerun.exe" <TestName>.dll` from the test's output directory.
