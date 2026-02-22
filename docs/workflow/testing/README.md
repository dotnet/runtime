# Testing the Runtime

Guides for testing the different components of dotnet/runtime.

## By Component

| Component | Guide | Description |
|-----------|-------|-------------|
| **CoreCLR** | [Testing CoreCLR](coreclr/testing.md) | Runtime tests in `src/tests/` |
| **Libraries** | [Testing Libraries](libraries/testing.md) | Library unit tests |
| **Mono** | [Testing Mono](mono/testing.md) | Mono-specific testing |
| **Host** | [Testing Host](host/testing.md) | Host activation tests |

## Quick Reference

### Library Tests (Most Common)

```bash
# Test a single library
cd src/libraries/System.Foo/tests
dotnet build /t:Test

# Run all library tests
./build.sh libs.tests -test
```

### CoreCLR Tests

```bash
# Build Core_Root first
./src/tests/build.sh generatelayoutonly

# Run all tests
./src/tests/run.sh

# Build and run specific test
./src/tests/build.sh -test:JIT/Test/Test.csproj
```

### Mono Tests

```bash
# Library tests on Mono
dotnet build /t:Test /p:RuntimeFlavor=mono
```

## Test Filtering

### Library Tests

```bash
# By class
dotnet build /t:Test /p:XUnitOptions="-class Namespace.TestClass"

# By method
dotnet build /t:Test /p:XUnitOptions="-method Namespace.Class.Method"

# Outer loop tests
dotnet build /t:Test /p:Outerloop=true
```

### CoreCLR Tests

```bash
# By priority (0 is default)
./src/tests/build.sh -priority=1

# By directory
./src/tests/build.sh -dir:JIT/Methodical

# By subtree
./src/tests/build.sh -tree:JIT
```

## Platform-Specific Testing

| Platform | Guide |
|----------|-------|
| WebAssembly | [Testing WASM](libraries/testing-wasm.md) |
| Android | [Testing Android](libraries/testing-android.md) |
| iOS/macOS | [Testing Apple](libraries/testing-apple.md) |

## Special Topics

| Topic | Guide |
|-------|-------|
| Filtering tests | [Test Filtering](libraries/filtering-tests.md) |
| Using corerun | [Using corerun and Core_Root](using-corerun-and-coreroot.md) |
| Dev packages | [Using Dev Packages](using-dev-shipping-packages.md) |
| Installed SDK | [Using Your Build](using-your-build-with-installed-sdk.md) |
| Visual Studio | [Testing in VS](visualstudio.md) |

## Test Results

- Library tests: `artifacts/bin/<Library>.Tests/*/testResults.xml`
- CoreCLR tests: `artifacts/log/TestRun_<Arch>_<Config>.html`
- Failures: `artifacts/log/TestRunResults_*.err`
