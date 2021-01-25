// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x152f1077 {}
.class ILGEN_0x152f1077 {

.method static int32 Method_0x2763af56(int64 Arg_0x0) {
.maxstack	15

 .locals (int32[] local_0x0,float32 local_0x1,int8[] local_0x2,unsigned int64[] local_0x3,unsigned int16[] local_0x4,unsigned int64[] local_0x5)

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x0
ldc.r4 float32(0x5ea6668c) 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x5

 ldc.i8 0x19666fe920a01a54
 neg
 conv.ovf.i2.un
 ldc.i4 0x3220eb39
 ldc.i4.0
 mul.ovf.un
 ldc.i4.3
 conv.u1
 cgt
 sub.ovf
Start_Orphan_18:
 ldloc local_0x2
 stloc local_0x2
End_Orphan_18:
Start_Orphan_1a:
 ldloc local_0x4
 ldc.i4.0
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_1a:
 ldc.r4 float32(0x978afeff)
 conv.u
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldc.i4.8
 conv.u1
Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldc.i4.4
 conv.ovf.i
 add.ovf.un
 rem
 ceq
Start_Orphan_1d:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldc.i8 0x2ea7a30a9f9ccb89
 conv.ovf.i8.un
 conv.ovf.i8
 starg Arg_0x0
End_Orphan_1d:
Start_Orphan_1f:
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_21:
 nop
End_Orphan_21:
 ldc.i4.s 62
 ldc.i4.7
 ceq
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i4.0
 conv.ovf.u1
 stelem.i4
End_Orphan_1f:
 ldc.i4.1
 conv.i2
 neg
 conv.ovf.u
 ldc.i4.4
 neg
Start_Orphan_23:
 ldloca local_0x1
 ldc.r4 float32(0xb35aeece)
 stind.r4
End_Orphan_23:
 ldc.r4 float32(0xf345591b)
 conv.ovf.u1
 conv.i
 rem
 rem.un
 add.ovf.un
 conv.i
Start_Orphan_24:
Start_Orphan_25:
 ldloc local_0x0
 ldc.i4.0
 ldc.i4.s 54
 stelem.i4
End_Orphan_25:
 ldloc local_0x5
Start_Orphan_26:
 ldloc local_0x4
 ldc.i4.8
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_26:
 ldloc local_0x2
 ldc.i4.8
 ldelem.i1
 conv.ovf.u2.un
 ldelema [mscorlib]System.UInt64
 ldarg Arg_0x0
 ldarg Arg_0x0
 mul.ovf
 ldc.i8 0xa404cc72f5740578
 not
 sub
Start_Orphan_28:
 ldloc local_0x5
 stloc local_0x5
End_Orphan_28:
 ldc.i4 0xa0fadd11
 conv.u8
 ldc.i8 0xb5a28546972eb3f1
 ldarg Arg_0x0
 div.un
 rem.un
 sub.ovf
 stind.i8
End_Orphan_24:
 ldc.i4.5
 conv.ovf.i4
 not
 conv.r8
 conv.ovf.u2
Start_Orphan_31:
 ldloc local_0x0
 ldc.i4.s -79
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_31:
 ceq
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x5e537187d7162636 

call int32 ILGEN_0x152f1077::Method_0x2763af56(int64 Arg_0x0)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
