// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xac0ba7f2 {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.class ILGEN_0xac0ba7f2 {
.method static int32 Method_0x7949() {
.maxstack	19

Start_Orphan_3b:
 ldc.i4.4
 ldc.i4 0xe5
 mul
 ldc.i8 0x100
 conv.ovf.u1
 mul.ovf
 pop
End_Orphan_3b:

Start_Orphan_41:
 ldc.i4 255
 newarr [mscorlib]System.Int32 
 ldc.i4.4
 ldc.i4 55
 stelem.i4 
End_Orphan_41:

ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
.try {
 call int32 ILGEN_0xac0ba7f2::Method_0x7949()
 pop
 leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
 ldc.i4 100
ret
}

}
