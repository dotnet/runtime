# Host Development Guide

This guide covers building, testing, and debugging the .NET host components - the executables and libraries that launch and configure .NET applications.

**Source location:** `src/native/corehost/`, `src/installer/`

## Quick Reference

| Task | Command |
|------|---------|
| Build host | `./build.sh host -rc Release -lc Release` |
| Build host + tests | `./build.sh host.tests -rc Release -lc Release` |
| Run host tests | `./build.sh host.tests -test -rc Release -lc Release` |

## What are the Host Components?

The host components include:

- **dotnet** - The main CLI executable
- **apphost** - Per-app executable template
- **hostfxr** - Framework resolution logic
- **hostpolicy** - Runtime configuration and startup

## Building

### Prerequisites

First, build the runtime and libraries:

```bash
./build.sh clr+libs -c Release
```

### Building the Host

```bash
./build.sh host -runtimeConfiguration Release -librariesConfiguration Release
```

Or using shorthand:

```bash
./build.sh host -rc Release -lc Release
```

### Build Outputs

Host binaries are placed in: `artifacts/bin/win-x64.Release/corehost/`

The Visual Studio solution is at: `artifacts/obj/win-<Arch>.<Config>/corehost/ide/corehost.slnx`

Open it with:

```cmd
build.cmd -vs corehost.slnx -a x64 -c Release
```

## Testing

### Building Tests

Tests are included in the `host` subset by default. Build just the tests:

```bash
./build.sh host.tests -rc Release -lc Release
```

### Running All Tests

```bash
./build.sh host.tests -test -rc Release -lc Release
```

Without rebuilding:

```bash
./build.sh host.tests -test -testnobuild
```

### Running Specific Tests

```bash
dotnet test artifacts/bin/HostActivation.Tests/Debug/net11.0/HostActivation.Tests.dll \
  --filter "category!=failing"
```

Filter to specific tests:

```bash
dotnet test artifacts/bin/HostActivation.Tests/Debug/net11.0/HostActivation.Tests.dll \
  --filter "DependencyResolution&category!=failing"
```

### Test Context

Host tests require:

1. Pre-built test project output
2. Product binaries in .NET install layout
3. TestContextVariables.txt configuration

These are created by the `host.pretest` subset (included in `host`).

To update test context without running tests:

```bash
./build.sh host.pretest

# Then for a specific test project:
dotnet build src/installer/tests/HostActivation.Tests \
  -t:SetupTestContextVariables \
  -p:RuntimeConfiguration=Release \
  -p:LibrariesConfiguration=Release
```

## Debugging

### Visual Studio

1. Open the solution:
   ```cmd
   build.cmd -vs Microsoft.DotNet.CoreSetup -rc Release -lc Release
   ```

2. Set your test project as startup project
3. Debug as normal

### Investigating Test Failures

Test results are in: `artifacts/TestResults/`

Tests launch separate processes (dotnet, apphost) and validate output. On failure, they report:
- The file path of the launched executable
- Command-line arguments
- Environment variables

### Preserving Test Artifacts

By default, test artifacts are deleted after tests complete. To keep them:

```bash
export PRESERVE_TEST_RUNS=1
./build.sh host.tests -test
```

This helps with debugging by preserving the exact test setup.

## Common Tasks

### Using apphost

See [Using apphost](testing/host/using-apphost.md) for testing with custom app hosts.

### Testing with Your Build

See [Using Your Build with Installed SDK](testing/using-your-build-with-installed-sdk.md).

## Deep Dives

| Topic | Link |
|-------|------|
| Testing details | [Testing Host](testing/host/testing.md) |
| Host design | [Host Components Design](../design/features/host-components.md) |
| Using dev packages | [Dev Shipping Packages](testing/using-dev-shipping-packages.md) |
