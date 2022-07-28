// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b85317' {}
.assembly extern xunit.core {}
.class ILGEN_0xddaef29d {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
     ldc.i8 0x1f65d516e9665e42
    conv.r4
   ckfinite
  conv.u8
conv.i4
ldc.i4 100
add
 ret
}

}



