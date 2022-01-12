// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1915effd {}
.assembly extern xunit.core {}
.class ILGEN_0x1915effd {
.method static int32 Method_0x85e39eed() {
.maxstack	17

 .locals (unsigned int32 local_0x0)
ldc.i4 0x2026a291 
stloc local_0x0

Start_Orphan_0:
Start_Orphan_3:
 ldc.i8 0x20abab3f65a783d
 conv.ovf.i1.un
 brtrue Branch_0x6

 ldloc local_0x0
 brtrue Branch_0x8

 nop
 br Branch_0x9

 Branch_0x8:

 nop
 Branch_0x9:

 br Branch_0x7

 Branch_0x6:

 nop
 Branch_0x7:

End_Orphan_3:
 ldc.r4 float32(0x31b1eef)
 conv.i4
 ldc.i4.1
 conv.ovf.u.un
 bge Branch_0x2

 ldloca local_0x0
 ldind.u4
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0xe6e6942de508c47
 conv.ovf.i
 Branch_0x3:

pop
	
End_Orphan_0:
ldc.i4 00
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

.try {
call int32 ILGEN_0x1915effd::Method_0x85e39eed()

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
