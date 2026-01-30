# Test: Microsoft.Extensions.Options.Tests.OptionsTest.Configure_GetsNullableOptionsFromConfiguration

## Test Suite
Microsoft.Extensions.Options.Tests

## Failure Type
assertion

## Exception Type
Xunit.Sdk.CollectionException (Assert.Collection failure)

## Stack Trace
```
Assert.Collection() Failure: Item comparison failure
             ↓ (pos 0)
Collection: [["MyNullableBool"] = True, ["MyNullableInt"] = 1, ["MyNullableDateTime"] = 2015-01-01T00:00:00.0000000]
Error:      Assert.Equal() Failure: Values differ
            Expected: True
            Actual:   null
            Stack Trace:
               at Microsoft.Extensions.Options.Tests.OptionsTest.<>c__DisplayClass12_0.<Configure_GetsNullableOptionsFromConfiguration>b__2(KeyValuePair`2 kvp)
   at Microsoft.Extensions.Options.Tests.OptionsTest.Configure_GetsNullableOptionsFromConfiguration(IDictionary`2 configValues, IDictionary`2 expectedValues)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter/configuration-binding
- The test fails when binding non-null values to nullable properties via configuration
- Works when all values are null (third test case passes)
- Mono baseline: 107 tests passing, CoreCLR: 108 tests run (1 extra test: TestCurrentValueDoesNotAllocateOnceValueIsCached)
- Issue appears to be with nullable type binding in Microsoft.Extensions.Configuration.Binder on CoreCLR/Browser
