// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b13586
{
}
.class ILGEN_6 {
 .method static int32 main() {
.entrypoint
.maxstack	20

 .locals (float64)
 ldc.r8 3.3
 stloc 0
 
 ldloca 0
 ldind.r8
 ldc.r8 1.1 
 ldloc 0
 sub
 cgt
 ldloca 0
 ldind.r8
 ldloc 0
 clt
 sub
 
 ldc.i4 99
 add
 ret
}
}
