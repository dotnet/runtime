// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x24fde2d6 {}
.class ILGEN_0x24fde2d6 {
.method static int32 Main()
{
.maxstack	13
.entrypoint
 ldc.r8 float64(0x1b376e25972ab43)
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.i
 ldc.i4 100
 add
 ret
}
}