// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b39381' {}
.assembly extern xunit.core {}
.class ILGEN_0xcb543741 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 .locals (unsigned int64 local_0x4)
.try {
 ldc.i8 0x1df13072  
 stloc local_0x4

 ldloc local_0x4
 conv.ovf.i1
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i4.0
 ldelema [mscorlib]System.Int64
 pop

 ldc.i4 0x77b32b3
pop

  leave a1
} catch [mscorlib]System.OverflowException {
  pop
  leave a1
}
a1:
ldc.i4 100
 ret
}

}
