# System.Diagnostics.StackTrace.Tests - Browser WASM Test Summary

## Test Configuration
- **Runtime:** CoreCLR (interpreter, no JIT)
- **Test Environment:** browser-wasm, Chrome via xharness
- **Build Configuration:** Release
- **Date:** 2026-01-30

## Results

| Metric | Value |
|--------|-------|
| Total Tests | N/A |
| Passed | N/A |
| Failed | N/A |
| Skipped | N/A |

## Build Failure

The test suite failed to build with error:
```
System.InvalidOperationException: No file exists for the asset at either location 
'/home/pavelsavara/dev/runtime/artifacts/bin/System.Diagnostics.StackTrace.Tests/Release/net11.0/browser-wasm/publish/System.Diagnostics.StackTrace.Tests.dll'
```

This appears to be an issue with static web assets during the browser-wasm build process.

## Conclusion
**BUILD FAILED** - Needs investigation for static web assets error.
