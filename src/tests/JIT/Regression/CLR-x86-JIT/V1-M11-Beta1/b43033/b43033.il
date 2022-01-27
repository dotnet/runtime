// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b43033' {}
.assembly extern xunit.core {}
.class ILGEN_0x64be9170 {
.method static int32 Method_0xe55ca458(int64 Arg_0x0) {
.maxstack	9

 .locals (float64[] local_0x0,unsigned int32 local_0x3)

ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x0
ldc.i4 0x146ba2dc 
stloc local_0x3
 
 ldc.i4.7
 conv.i8
 ldloc local_0x0
 ldc.i4.m1
 ldelem.r8
 conv.ovf.u8
Start_Orphan_d:
 ldarga Arg_0x0
 ldc.i8 0xe813fcabdd025c40
 stind.i8
End_Orphan_d:
 blt.un Branch_0x2

Start_Orphan_f:
 ldloc local_0x3
 conv.ovf.i8.un
 pop
End_Orphan_f:
 ldc.i8 0xbbe69058a5513df7
 ldc.i8 0xe5de0a6c129109d0
 div
 conv.ovf.i1
 br Branch_0x3

 Branch_0x2:

 ldc.i4.7
 Branch_0x3:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x8405c730422029aea0cfec704529196c 

call int32 ILGEN_0x64be9170::Method_0xe55ca458(int64 Arg_0x0)

  pop
  leave out_of_here
} catch [mscorlib]System.IndexOutOfRangeException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}
}
