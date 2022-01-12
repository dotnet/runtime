// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbb363761 {}
.assembly extern xunit.core {}
.class ILGEN_0xbb363761 {

//BEGIN STATIC FIELDS
//END STATIC FIELDS
.method static int32 Method_0x60a9(int64 Arg_0x0, float32 Arg_0x1, unsigned int32 Arg_0x2, int64 Arg_0x3, unsigned int16 Arg_0x4, unsigned int64 Arg_0x5, float32 Arg_0x6) {
.maxstack	17

 .locals (int8 local_0x0,int16 local_0x1,unsigned int64 local_0x2,int32 local_0x3,float64 local_0x4,unsigned int64 local_0x5,int8 local_0x6,float32[] local_0x7,int64 local_0x8)
//BEGIN INIT LOCALS
ldc.i4 0x4fed22e7 
stloc local_0x0
ldc.i4 0x1cdd36c8 
stloc local_0x1
ldc.i8 0x49f97a413b842d60 
stloc local_0x2
ldc.i4 0x4dc420b6 
stloc local_0x3
ldc.r8 float64(0x79ba12aa459769fd) 
stloc local_0x4
ldc.i8 0x5bee537f8d24fe2 
stloc local_0x5
ldc.i4 0x44056e6c 
stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x7
ldc.i8 0x67d4430054422aaf 
stloc local_0x8

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldarg Arg_0x1
 ldc.r4 float32(0x427d771d)
 sub
 conv.i8
 pop
End_Orphan_2:
 ldloca local_0x1
 pop
End_Orphan_1:
Start_Orphan_4:
Start_Orphan_5:
 ldarga Arg_0x0
 pop
End_Orphan_5:
Start_Orphan_6:
 nop
End_Orphan_6:
 ldarga Arg_0x6
 ldind.r4
Start_Orphan_7:
 nop
End_Orphan_7:
 ldarg Arg_0x6
 ckfinite
 clt
 conv.u1
 newarr [mscorlib]System.Single
 stloc local_0x7
End_Orphan_4:
Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldarga Arg_0x5
 pop
End_Orphan_8:
Start_Orphan_a:
Start_Orphan_b:
 nop
End_Orphan_b:
 ldloca local_0x3
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i4.5
 ldarg Arg_0x4
 mul.ovf.un
 stind.i4
End_Orphan_a:
 ldc.i4.0
 ldc.i4.0
 sub
Start_Orphan_d:
 nop
End_Orphan_d:
 ldc.i4.1
 ldc.i4.2
 sub
 cgt
Start_Orphan_e:
 ldloc local_0x8
 stloc local_0x8
End_Orphan_e:
Start_Orphan_f:
 nop
End_Orphan_f:
 ldc.i4.2
 conv.ovf.i4.un
 conv.ovf.u1
 xor
Start_Orphan_10:
 nop
End_Orphan_10:
 ldc.i4.8
 ldc.i4.6
 mul.ovf
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.s 81
 conv.ovf.i4.un
 mul.ovf.un
 conv.ovf.u4
 xor
 not
 conv.ovf.u1.un
 pop
End_Orphan_0:
Start_Orphan_12:
Start_Orphan_13:
 ldloca local_0x3
Start_Orphan_14:
 ldloca local_0x3
 ldc.i4.5
 stind.i4
End_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldloc local_0x4
 ldc.r8 float64(0x272c6c7b6273723f)
 add
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.r8 float64(0x711143fd4b9872ae)
 conv.r8
 clt
 stind.i4
End_Orphan_13:
 ldloca local_0x2
Start_Orphan_17:
Start_Orphan_18:
 nop
End_Orphan_18:
 ldloca local_0x4
 ldind.r8
 stloc local_0x4
End_Orphan_17:
 ldc.i4.4
 conv.ovf.u8.un
 stind.i8
End_Orphan_12:
Start_Orphan_19:
Start_Orphan_1a:
 ldarga Arg_0x5
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldloc local_0x5
 conv.ovf.u8.un
 stind.i8
End_Orphan_1a:
Start_Orphan_1c:
 ldarg Arg_0x0
 stloc local_0x5
End_Orphan_1c:
 ldc.i8 0x7e2655637542012
 conv.i1
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldloc local_0x2
 conv.u1
 add.ovf
Start_Orphan_1e:
 ldarg Arg_0x0
 stloc local_0x8
End_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldc.i4.3
 conv.u
 conv.ovf.u4.un
 clt.un
 starg Arg_0x4
End_Orphan_19:
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i4.s 68
Start_Orphan_21:
Start_Orphan_22:
 ldloc local_0x1
 stloc local_0x3
End_Orphan_22:
 ldarga Arg_0x3
Start_Orphan_23:
 ldarga Arg_0x5
 ldloc local_0x8
 stind.i8
End_Orphan_23:
 ldc.i4.1
 conv.ovf.u8.un
Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.i4.4
 conv.i8
 mul
 stind.i8
End_Orphan_21:
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldloca local_0x4
Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.r8 float64(0x7b1237c328987cf1)
 stind.r8
End_Orphan_25:
 ldloc local_0x7
Start_Orphan_28:
 ldc.i4.4
 starg Arg_0x4
End_Orphan_28:
Start_Orphan_29:
 nop
End_Orphan_29:
 ldloca local_0x3
 ldind.i4
 ldelem.r4
 conv.ovf.u4
 sub.ovf
 conv.r4
 ckfinite
 conv.i1
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x1f4529a4be81124 
ldc.r4 float32(0x15ed2718) 
ldc.i4 0x581b352e 
ldc.i8 0x4e366e22526b6177 
ldc.i4 0x35665785 
ldc.i8 0x1f302166654b05 
ldc.r4 float32(0x5fd4a6f) 

call int32 ILGEN_0xbb363761::Method_0x60a9(int64 Arg_0x0, float32 Arg_0x1, unsigned int32 Arg_0x2, int64 Arg_0x3, unsigned int16 Arg_0x4, unsigned int64 Arg_0x5, float32 Arg_0x6)
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
