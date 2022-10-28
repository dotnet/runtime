# System.Runtime.Numerics
Contains additional numeric types that complement the numeric primitives (such as `Byte`, `Double`, and `Int32`) that are defined by .NET. This includes:

* The `BigInteger` structure, which is a non-primitive integral type that supports arbitrarily large integers.
* The `Complex` structure, which represents a complex number. A complex number is a number in the form *a* + *b*i, where *a* is the real part, and *b* is the imaginary part.
* The SIMD-enabled vector types, which include `Vector2`, `Vector3`, `Vector4`, `Matrix3x2`, `Matrix4x4`, `Plane`, and `Quaternion`.

Documentation can be found here: https://learn.microsoft.com/en-us/dotnet/api/system.numerics.

This area also includes all of the interfaces that make up Generic Math, which is discussed more here: https://learn.microsoft.com/en-us/dotnet/standard/generics/math.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Numerics+label%3A%22help+wanted%22+) issues.


## Source
* `BigInteger` and `Complex`: [./src/System/Numerics](./src/System/Numerics).
* Everything else: [../../coreclr/System.Private.CoreLib/src/System/Numerics](../../coreclr/System.Private.CoreLib/src/System/Numerics)
* Tests for this library live in [./tests](./tests) and [../System.Numerics.Vectors/tests](../System.Numerics.Vectors/tests)

## Deployment
[System.Runtime.Numerics](https://www.nuget.org/packages/System.Runtime.Numerics) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.