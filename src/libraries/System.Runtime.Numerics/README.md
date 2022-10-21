# System.Runtime.Numerics
Contains additional numeric types that complement the numeric primitives (such as Byte, Double, and Int32) that are defined by .NET.

This namespace includes the following types:

The BigInteger structure, which is a nonprimitive integral type that supports arbitrarily large integers. An integral primitive such as Byte or Int32 includes a MinValue and a MaxValue property, which define the lower bound and upper bound supported by that data type. In contrast, the BigInteger structure has no lower or upper bound, and can contain the value of any integer.

The Complex structure, which represents a complex number. A complex number is a number in the form a + bi, where a is the real part, and b is the imaginary part.

The SIMD-enabled vector types, which include Vector2, Vector3, Vector4, Matrix3x2, Matrix4x4, Plane, and Quaternion.



## Status: [Active](../../libraries/README.md#development-statuses)
TODO: Explanation of status


## Source
* CoreClr-specific: [../../coreclr/System.Private.CoreLib/src/System/Reflection]

## Deployment
TODO: whether or not it is included in the shared framework

TODO: should we include Globalization stuff?

Also, where is System.Numerics.Tensors?