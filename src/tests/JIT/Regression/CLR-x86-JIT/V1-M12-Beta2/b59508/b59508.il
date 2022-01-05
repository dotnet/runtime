// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b59508' {}
.assembly extern xunit.core {}
.class ILGEN_0x486ae288 {
.method static int32 Method_0x317f3e0e(int32 Arg_0x2) {
.maxstack	9
.locals (int16[] LOCAL_0x0,float64[] LOCAL_0x1,unsigned int64[] LOCAL_0x2)

 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc LOCAL_0x0
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x1
 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x2

 ldarg Arg_0x2
 ldarg Arg_0x2
 blt Branch_0x4
 ldc.i8 0x5c94863c1f6c8fe4
 br Branch_0x5
Branch_0x4:
 ldc.i8 0x23fdbdb2f517cc45
Branch_0x5:
 ldarg Arg_0x2
 conv.ovf.u8
 and
 br Branch_0x1
Branch_0x1:
 conv.i4
 ldc.i4 470026780
 sub
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 ldc.i4 0x5c842a99
 call int32 ILGEN_0x486ae288::Method_0x317f3e0e(int32 Arg_0x2)
 ret
}
}
