# Test Failure Report

## Test Information
- **Test Suite**: Microsoft.Extensions.Configuration.Binder.Tests
- **Test Class**: Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests
- **Test Methods**: 
  - ComplexObj_As_Dictionary_Element
  - ComplexObj_As_Enumerable_Element
  - ObjWith_TypeConverter
- **Test File**: src/libraries/Microsoft.Extensions.Configuration.Binder/tests/Common/ConfigurationBinderTests.cs

## Failure Details
- **Platform**: Browser/WASM + CoreCLR (interpreter)
- **Passes on Mono**: Yes
- **GitHub Issue**: https://github.com/dotnet/runtime/issues/123011

## Error Message
```
Assert.Equal() Failure: Values differ
Expected: 3
Actual:   0
```

## Stack Trace
```
at Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests.ValidateGeolocation(IGeolocation location)
at Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests.ComplexObj_As_Dictionary_Element()
```

## Root Cause
The `Geolocation` struct has properties (Latitude, Longitude) that are not being bound correctly through the reflection-based configuration binding on CoreCLR+Browser interpreter. The properties remain at their default value (0) instead of being set to 3 and 4.

This is related to the same interpreter bug affecting large value types - the struct binding mechanism relies on reflection invoke paths that don't work correctly for value types on the Browser/WASM interpreter.

## Fix Applied
Added `[ActiveIssue]` attribute to skip these tests on Browser+CoreCLR until the interpreter bug is fixed.
