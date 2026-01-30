# Test Failure Report

## Test Information
- **Test Suite**: Microsoft.Extensions.Configuration.Binder.Tests
- **Test Class**: Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests
- **Test Method**: CanReadAllSupportedTypes (multiple InlineData cases)
- **Test File**: src/libraries/Microsoft.Extensions.Configuration.Binder/tests/Common/ConfigurationBinderTests.cs

## Failure Details
- **Platform**: Browser/WASM + CoreCLR (interpreter)
- **Passes on Mono**: Yes
- **GitHub Issue**: https://github.com/dotnet/runtime/issues/123011

## Failing Test Cases
All failures involve value types larger than 8 bytes:

1. **TimeSpan**: `CanReadAllSupportedTypes("99.22:22:22.1234567", typeof(System.TimeSpan))`
   - Expected: 99.22:22:22.1234567
   - Actual: 00:00:00

2. **DateTimeOffset**: `CanReadAllSupportedTypes("12/24/2015 13:44:55 +4", typeof(System.DateTimeOffset))`
   - Expected: 2015-12-24T13:44:55.0000000+04:00
   - Actual: 0001-01-01T00:00:00.0000000+00:00

3. **DateTime**: `CanReadAllSupportedTypes("2015-12-24T07:34:42-5:00", typeof(System.DateTime))`
   - Expected: 2015-12-24T12:34:42.0000000+00:00
   - Actual: 0001-01-01T00:00:00.0000000

4. **Decimal**: `CanReadAllSupportedTypes("79228162514264337593543950335", typeof(decimal))`
   - Expected: 79228162514264337593543950335
   - Actual: 0

5. **Guid**: `CanReadAllSupportedTypes("CA761232-ED42-11CE-BACD-00AA0057B223", typeof(System.Guid))`
   - Expected: ca761232-ed42-11ce-bacd-00aa0057b223
   - Actual: 00000000-0000-0000-0000-000000000000

## Root Cause
The CoreCLR interpreter on Browser/WASM has a bug where value types larger than 8 bytes (DateTime, DateTimeOffset, TimeSpan, Decimal, Guid) return zeros instead of actual values when passed through reflection-based binding paths.

This is the same underlying issue affecting:
- System.Reflection.InvokeInterpreted.Tests
- System.Reflection.InvokeEmit.Tests

## Fix Applied
Added `[ActiveIssue]` attribute to skip these specific test cases on Browser+CoreCLR until the interpreter bug is fixed.
