// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
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
