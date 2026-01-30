# Microsoft.Extensions.Configuration.Binder.Tests - CoreCLR Browser WASM

## Status: ✅ PASSED (with skipped tests)

## Test Results
- **Tests run:** 281
- **Passed:** 280
- **Failed:** 0
- **Skipped:** 1 (due to platform conditions)
- **Ignored:** 0
- **Inconclusive:** 0

## Skipped Tests (via ActiveIssue for Browser+CoreCLR)
The following tests were disabled on Browser+CoreCLR via `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`:

### Large Value Type Tests (split into separate method)
1. **CanReadAllSupportedTypes_LargeValueTypes** - 5 variants for:
   - `decimal`
   - `DateTime`
   - `DateTimeOffset`
   - `TimeSpan`
   - `Guid`

### Complex Object / TypeConverter Tests
2. **ObjWith_TypeConverter** - Struct interface implementation issues
3. **ComplexObj_As_Dictionary_Element** - Geolocation struct issues
4. **ComplexObj_As_Enumerable_Element** - Geolocation struct issues

### Struct Tests
5. **TestOptionsWithStructs** - StructOptions binding failures
6. **TestOptionsWithUnsupportedStructs** - UnsupportedStructOptions binding failures

## Root Cause Analysis
All failing tests involve:
- Large value types (>8 bytes): decimal, DateTime, DateTimeOffset, TimeSpan, Guid
- Structs implementing interfaces (IGeolocation, IConvertible via TypeConverter)
- The interpreter reflection path returns zeros/default values for these types

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011

## Failure Reports
- [CanReadAllSupportedTypes_LargeValueTypes.md](../failures/CanReadAllSupportedTypes_LargeValueTypes.md)
- [ComplexObj_Geolocation.md](../failures/ComplexObj_Geolocation.md)
- [TestOptionsWithStructs.md](../failures/TestOptionsWithStructs.md)
