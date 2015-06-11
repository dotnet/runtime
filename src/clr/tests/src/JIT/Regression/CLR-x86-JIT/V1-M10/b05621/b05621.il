// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b05621
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

.locals (float32, float32)
 ldc.r4 1
 ldc.r4 1
 stloc 0
 stloc 1
 ldc.i4.2
 conv.u
 ldloc 0
 neg
 stloc 1
 conv.u
 ldloc 0
 ldloc 1
 ceq
 conv.u8
 conv.u
 clt

ldc.i4 100
add
 ret
}
}