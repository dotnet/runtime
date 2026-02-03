# Libraries Development Guide

This guide covers everything you need to build, test, and debug the .NET libraries (the BCL - Base Class Library).

**Source location:** `src/libraries/`

## Quick Reference

| Task | Command |
|------|---------|
| Build all libraries | `./build.sh libs` |
| Build + test one library | `cd src/libraries/System.Foo/tests && dotnet build /t:Test` |
| Run all library tests | `./build.sh libs.tests -test` |

## Building

### First-Time Setup

Before working on libraries, build the runtime they'll run on:

```bash
# Build CoreCLR (Release) + Libraries (Debug) - recommended setup
./build.sh clr+libs -rc Release
```

### Building a Single Library

Navigate to the library and build:

```bash
cd src/libraries/System.Collections
dotnet build
```

Or from repo root:

```bash
./build.sh -projects src/libraries/*/System.Collections.slnx
```

### Building All Libraries

```bash
./build.sh libs
```

### Build Options

| Flag | Description |
|------|-------------|
| `-c Release` | Build in Release mode |
| `-arch x86` | Build for x86 architecture |
| `-os linux` | Build for Linux |
| `/p:RuntimeFlavor=Mono` | Build against Mono instead of CoreCLR |

### Iterating on System.Private.CoreLib

When you change `System.Private.CoreLib`, rebuild it and update the test host:

```bash
./build.sh clr.corelib+clr.nativecorelib+libs.pretest -rc Release
```

For Mono:

```bash
./build.sh mono.corelib+libs.pretest
```

## Testing

### Testing a Single Library

```bash
cd src/libraries/System.Collections/tests
dotnet build /t:Test
```

### Filtering Tests

Run specific test class:

```bash
dotnet build /t:Test /p:XUnitOptions="-class Test.ClassUnderTests"
```

Run specific test method:

```bash
dotnet build /t:Test /p:XUnitOptions="-method Namespace.Class.Method"
```

### Running Outer Loop Tests

Outer loop tests are slower but more comprehensive:

```bash
dotnet build /t:Test /p:Outerloop=true
```

### Testing All Libraries

```bash
./build.sh libs.tests -test -c Release
```

### Speeding Up Test Iteration

Skip rebuild when code hasn't changed:

```bash
dotnet build /t:Test /p:testnobuild=true --no-restore
```

### Test Results

Test logs are at: `artifacts/bin/System.Foo.Tests/Debug/net11.0/testResults.xml`

## Debugging

### Visual Studio (Windows)

1. Open the library's solution file (`.slnx`)
2. Set the test project as startup project
3. Debug as normal

**Note:** Starting with VS 2022 17.5, you may need to disable signature validation for local builds. See [Debugging CoreCLR](debugging/coreclr/debugging-runtime.md#resolving-signature-validation-errors-in-visual-studio).

### VS Code

See [Debugging with VS Code](debugging/libraries/debugging-vscode.md) for detailed setup.

### Debugging on Unix

See [Unix Debugging Instructions](debugging/libraries/unix-instructions.md).

## Common Tasks

### Adding a New Test

Add tests to existing test files when possible. If creating a new test file:

1. Add it to the appropriate test project under `tests/`
2. Follow existing naming conventions
3. Use `[Fact]` for simple tests, `[Theory]` for parameterized tests

### Updating Reference Assemblies

When adding new APIs, update the reference source:

```bash
cd src/libraries/System.Foo/ref
dotnet build /t:GenerateReferenceSource
```

See [Updating Reference Source](../../coding-guidelines/updating-ref-source.md).

### Building Packages

```bash
./build.sh libs
dotnet pack src/libraries/System.Foo/src/ -c Release
```

### API Compatibility Errors

If you see API compatibility errors and they're expected (e.g., updating preview APIs):

```bash
dotnet pack /p:ApiCompatGenerateSuppressionFile=true
```

## Platform-Specific Development

### WebAssembly

See [WebAssembly Libraries](building/libraries/webassembly-instructions.md).

### Android/iOS

See [Testing on Mobile](testing/libraries/testing-android.md) and [Testing on Apple](testing/libraries/testing-apple.md).

## Deep Dives

| Topic | Link |
|-------|------|
| Building details | [Building Libraries](building/libraries/README.md) |
| Testing details | [Testing Libraries](testing/libraries/testing.md) |
| Code coverage | [Code Coverage](building/libraries/code-coverage.md) |
| Cross-building | [Cross-Building](building/libraries/cross-building.md) |
| Project guidelines | [Project Guidelines](../../coding-guidelines/project-guidelines.md) |
