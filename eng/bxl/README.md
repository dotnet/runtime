# BuildXL CoreCLR Tests

The BuildXL-backed CoreCLR test flow currently supports Linux x64 with a
Checked CoreCLR runtime and Release libraries.

## Setup

Restore the repository tools if `bxl` is not already on `PATH`:

```bash
dotnet tool restore
export PATH="$HOME/.dotnet/tools:$PATH"
```

Build the runtime and generate the Checked `Core_Root` that BuildXL uses:

```bash
./build.sh clr+libs+clr.iltools -lc Release -rc Checked
src/tests/build.sh checked x64 generatelayoutonly
```

## Build and Run

Build all CoreCLR tests through BuildXL:

```bash
src/tests/build.sh --bxl checked x64
```

Run all BuildXL-backed tests:

```bash
src/tests/run.sh --bxl checked x64
```

To build and run in one command:

```bash
src/tests/build.sh --bxl checked x64 runtests
```

To restrict the work to a test subtree:

```bash
src/tests/build.sh --bxl checked x64 -tree JIT/Regression
src/tests/run.sh --bxl checked x64 --tree=JIT/Regression
```

BuildXL outputs are under `Out/`. On failure, see
`Out/Logs/BuildXL.Dev.log`.
