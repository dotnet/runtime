# xunit v3 Migration Notes

This document covers common issues and patterns encountered when migrating
tests from xunit v2 to xunit v3.

## Empty `[MemberData]` at Runtime

In xunit v3, a `[Theory]` whose `[MemberData]` source returns **zero rows**
is a hard failure ("No data found"), not a silent no-op. When running through
the test harness this surfaces as:

```
[FATAL ERROR] System.InvalidOperationException
  Cannot find test case metadata for ID <sha256-hash>
```

This commonly happens when:

- A `[MemberData]` source filters its output based on platform support
  (e.g., `Where(x => SomeAlgorithm.IsSupported)`) and all items are
  filtered out on the current platform.
- A `[MemberData]` source **throws during enumeration** (e.g., because an
  object in the test data does not support `GetHashCode()`). The
  `ModifiedType` returned by `GetModifiedFieldType()` is a known example —
  it throws `NotSupportedException` from `GetHashCode()`.

### Diagnosing

Run the test assembly directly with `-list full` to map the failing ID to a
test method name:

```bash
# Use the testhost dotnet, not the system one
artifacts/bin/testhost/net11.0-linux-Debug-x64/dotnet exec \
  artifacts/bin/<TestProject>/Debug/net11.0-unix/<TestProject>.dll \
  -list full 2>&1 | grep <sha256-hash>
```

Then inspect the `[MemberData]` source for conditional logic or types that
cannot be serialized/hashed by xunit v3.

### Fixing Empty Data

Switch to an unconditional data source and move the platform check into the
test body:

```cs
// BROKEN: MemberData can return zero rows
public static IEnumerable<object[]> SupportedAlgorithmsTestData =>
    AllAlgorithms.Where(a => MyAlgorithm.IsSupported(a)).Select(a => new object[] { a });

[Theory]
[MemberData(nameof(SupportedAlgorithmsTestData))]
public void MyTest(MyAlgorithm algorithm) { /* ... */ }
```

```cs
// FIXED: MemberData always returns rows; skip at runtime
public static IEnumerable<object[]> AllAlgorithmsTestData =>
    AllAlgorithms.Select(a => new object[] { a });

[Theory]
[MemberData(nameof(AllAlgorithmsTestData))]
public void MyTest(MyAlgorithm algorithm)
{
    Assert.SkipUnless(MyAlgorithm.IsSupported(algorithm), "Not supported on this platform.");
    /* ... */
}
```

### Fixing Non-Serializable Data

When test data contains types that xunit v3 cannot serialize or hash (e.g.,
`ModifiedType` from `GetModifiedFieldType()`), pass simple identifiers
through `[InlineData]` and construct the problematic objects inside the test
body:

```cs
// BROKEN: ModifiedType throws NotSupportedException from GetHashCode()
public static IEnumerable<object[]> TestData
{
    get
    {
        yield return [someSignatureType, typeof(Foo).GetField("Bar").GetModifiedFieldType()];
    }
}

[Theory]
[MemberData(nameof(TestData))]
public void MyTest(Type signatureType, Type reflectedType) { /* ... */ }
```

```cs
// FIXED: pass field name, construct ModifiedType in test body
[Theory]
[InlineData(nameof(Foo.Bar))]
public void MyTest(string fieldName)
{
    Type reflectedType = typeof(Foo).GetField(fieldName).GetModifiedFieldType();
    Type signatureType = /* construct based on fieldName */;
    /* ... */
}
```

## `ConditionalTheory` → `[Theory]` + `Assert.SkipUnless`

`[ConditionalTheory]` from `Microsoft.DotNet.XUnitExtensions` evaluates its
conditions **before** enumerating `[MemberData]`. If the condition is false,
the entire theory is skipped without touching test data.

When migrating to plain `[Theory]`, move the condition into the test body
using `Assert.SkipUnless`:

```cs
// Before (xunit v2 / ConditionalTheory)
[ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsFeatureSupported))]
[MemberData(nameof(MyTestData))]
public void MyTest(int x) { /* ... */ }

// After (xunit v3)
[Theory]
[MemberData(nameof(MyTestData))]
public void MyTest(int x)
{
    Assert.SkipUnless(PlatformDetection.IsFeatureSupported, "Requires IsFeatureSupported");
    /* ... */
}
```

**Important**: Ensure the `[MemberData]` source does not depend on the same
condition. If it does, refactor the data source to always return rows (see
[Fixing Empty Data](#fixing-empty-data) above).

## `ConditionalFact` → `[Fact]` + `Assert.SkipUnless`

The same pattern applies:

```cs
// Before
[ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFeatureSupported))]
public void MyTest() { /* ... */ }

// After
[Fact]
public void MyTest()
{
    Assert.SkipUnless(PlatformDetection.IsFeatureSupported, "Requires IsFeatureSupported");
    /* ... */
}
```

## `EqualException.ForMismatchedValues` Signature Change

In xunit v3, `EqualException.ForMismatchedValues` requires `string`
parameters instead of `object`. Calls that previously passed arbitrary
objects must now call `.ToString()`:

```cs
// Before (xunit v2)
throw EqualException.ForMismatchedValues(expected, actual, banner);

// After (xunit v3)
throw EqualException.ForMismatchedValues(expected.ToString(), actual.ToString(), banner);
```

## Runner Configuration

The test runner configuration is in `eng/testing/xunit/xunit.runner.json`.
Key settings for the migration:

- `"preEnumerateTheories": false` — theories are **not** pre-enumerated at
  discovery time. Data is enumerated at runtime. This means `[MemberData]`
  sources are called during execution, not discovery.
- `"diagnosticMessages": true` — enables diagnostic output for debugging
  discovery and execution issues.
