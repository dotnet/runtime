# System.Reflection.InvokeInterpreted.Tests - Summary

## Status: ❌ CRASHED

## Test Results
- **Exit Code**: 71 (GENERAL_FAILURE)
- **Runtime**: WebAssembly crashed during test execution
- **No testResults.xml generated** - crash prevented completion

## Crash Details
The test crashed during `CreateDelegate_InheritedMethod` test with:
```
DOTNET: Unhandled error: RuntimeError: table index is out of bounds
```

**Note**: This is the same crash as System.Reflection.InvokeEmit.Tests - both crash on CreateDelegate_InheritedMethod.

## Failed Tests Before Crash (17 failures)
All failures are related to interpreted invocation of methods with larger value types:

### TestProperties failures (types with size > 8 bytes):
1. `TestProperties(typeName: "DateTimeOffset", ...)` - Expected: 9999-12-31T23:59:59.9999999+00:00, Actual: 0001-01-01T00:00:00.0000000+00:00
2. `TestProperties(typeName: "DateTime", ...)` - Expected: 9999-12-31T23:59:59.9999999, Actual: 0001-01-01T00:00:00.0000000
3. `TestProperties(typeName: "Decimal", ...)` - Expected: 42, Actual: 0
4. `TestProperties(typeName: "Guid", ...)` - Expected: 18b2a161-48b6-4d6c-af0e-e618c73c5777, Actual: 00000000-0000-0000-0000-000000000000

### ArgumentConversions failures:
5. `ArgumentConversions(..., "ValueType", ...)` - CustomValueType mismatch
6. `ArgumentConversions(..., "DateTime", ...)` - Expected: 0001-01-01T00:00:00.0000043, Actual: 0001-01-01T00:00:00.0000000
7. `ArgumentConversions(..., "DecimalWithAttribute", ...)` - Expected: -25825441708776829748.4, Actual: 0
8. `ArgumentConversions(..., "Decimal", ...)` - Expected: 103.14, Actual: 0
9. `ArgumentConversions(..., "NullableInt", ...)` - Expected: 42, Actual: null

### Nullable enum failures:
10. `InvokeNullableEnumParameterDefaultYes` - Expected: No, Actual: null
11. `InvokeNullableEnumParameterDefaultNo` - Expected: No, Actual: null  
12. `InvokeNullableEnumParameterDefaultNull` - Expected: No, Actual: null
13-17. (additional similar nullable enum tests in MethodInfoTests)

## Root Cause Analysis
1. **Value type handling bug**: The interpreter appears to fail when passing/returning value types larger than 8 bytes (DateTime, Decimal, Guid, etc.) - returns zeros instead
2. **Nullable handling bug**: Nullable<T> values are not being passed correctly - returns null instead of value
3. **CreateDelegate crash**: Fatal WASM crash in function table when creating delegates for inherited methods

## Comparison with Mono Baseline
Mono baseline exists but was not compared due to crash preventing testResults.xml generation.

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011

## Logs
- Console: [console_20260130_134036.log](console_20260130_134036.log)
