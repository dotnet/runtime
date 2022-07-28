// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b53884' {}
.assembly extern xunit.core {}
.class ILGEN_0x3c109d11 {

//BEGIN STATIC FIELDS
.field static	int64[] field_0x0
.field static	unsigned int16 field_0x1
//END STATIC FIELDS
.method static int32 Method_0x52483fac(int16 Arg_0x0, float64 Arg_0x1, unsigned int8 Arg_0x2, int8 Arg_0x3, float64 Arg_0x4) {
.maxstack	13

 .locals (int8[] local_0x0,unsigned int64 local_0x1,unsigned int16 local_0x2,int16[] local_0x3,float32[] local_0x4)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x0
ldc.i8 0x1fcc6f3fa09d4e90 
stloc local_0x1
ldc.i4 0x4cd2653b 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x3c109d11::field_0x0
ldc.i4 0x946373d7 
stsfld unsigned int16 ILGEN_0x3c109d11::field_0x1

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloc local_0x1
 ldc.i8 0x1fb853a5dd42128d
 beq Branch_0x4

 ldc.i8 0x2be65cdaf7b6147b
 br Branch_0x5

 Branch_0x4:

 ldloc local_0x1
 Branch_0x5:

Start_Orphan_1:
 nop
End_Orphan_1:
 ldloc local_0x2
 ldc.i4 0xc57efe2b
 blt Branch_0x6

 ldc.i8 0x5b18cf731ae05cee
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0xd118ad571ba6d95c
 Branch_0x7:

 add
 conv.ovf.u2
Start_Orphan_2:
 ldc.i4.7
 conv.u1
 newarr [mscorlib]System.Int16
 ldc.i8 0x7cf9c37b93d18f0
 ldc.i8 0x923f5920ea8c0ff8
 clt.un
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.i4.s 68
 neg
 stelem.i2
End_Orphan_2:
Start_Orphan_4:
 ldloca local_0x1
 pop
End_Orphan_4:
 ldloc local_0x1
 ldc.i8 0x6afe4f4720f7f783
 add.ovf.un
 conv.ovf.u4.un
 ldc.i4.2
 ldc.i4.2
 div.un
 conv.u
 blt.un Branch_0x8

Start_Orphan_6:
 ldsfld int64[] ILGEN_0x3c109d11::field_0x0
 ldc.i4 0xe6334866
 ldc.i8 0xf5659f836dfa9c93
 stelem.i8
End_Orphan_6:
 ldc.i8 0x754589795346a827
 conv.ovf.i
 conv.ovf.i4.un
 br Branch_0x9

 Branch_0x8:

 ldc.i4.7
 ldc.i4.1
 div.un
 conv.ovf.u
 Branch_0x9:

 beq Branch_0x2

 ldc.i4.7
 conv.ovf.i2.un
 conv.ovf.u2.un
 conv.u1
 br Branch_0x3

 Branch_0x2:

 ldloc local_0x4
 ldlen
 Branch_0x3:

 ldarg Arg_0x0
 ldc.i4.7
 sub.ovf
 conv.ovf.u8.un
 conv.ovf.u1.un
 ldc.i4.1
 ldc.i4.3
 and
 ldsfld int64[] ILGEN_0x3c109d11::field_0x0
 ldlen
 add.ovf.un
 conv.ovf.i.un
 add
 bge Branch_0x0

 ldc.i4.0
 starg Arg_0x2
 br Branch_0x1

 Branch_0x0:

 ldc.i4.7
 ldc.i4.1
 div.un
 conv.u1
 conv.u1
 newarr [mscorlib]System.SByte
 ldloc local_0x1
 ldc.i8 0x2e4d95ef62dbbddd
 ceq
 conv.r.un
 conv.u4
Start_Orphan_12:
 ldc.r8 float64(0x73deab1fa2d1dc6f)
 ldarg Arg_0x1
 clt.un
 starg Arg_0x0
End_Orphan_12:
 ldarg Arg_0x3
 conv.i4
 conv.u1
 newarr [mscorlib]System.SByte
 ldloc local_0x1
 ldc.i4.0
 shr.un
 conv.ovf.u2.un
 ldelem.i1
 stelem.i1
 Branch_0x1:

End_Orphan_0:
 ldloc local_0x1
 ldloc local_0x1
 ceq
 conv.ovf.i8
 ldc.i8 0x4c06aeb21a9af2c3
 ldloc local_0x1
 div.un
 ldloc local_0x1
 ldloc local_0x1
 mul.ovf
 add.ovf.un
 or
 ldloc local_0x1
 ldc.i8 0x4cd201d932d144a9
 rem.un
 ldc.i4.5
 conv.ovf.i4
 shr.un
 ldsfld unsigned int16 ILGEN_0x3c109d11::field_0x1
 ldc.i4.s -29
 xor
 conv.ovf.u8
 sub.ovf.un
 mul.ovf.un
Start_Orphan_18:
Start_Orphan_19:
 ldloca local_0x2
 ldc.i4.s 121
 stind.i2
End_Orphan_19:
 ldsfld int64[] ILGEN_0x3c109d11::field_0x0
 ldloc local_0x1
 conv.ovf.u2
 ldelema [mscorlib]System.Int64
 ldc.i8 0x94cb258a57d14fb9
 stind.i8
End_Orphan_18:
 ldc.i8 0xa60d377f2863e61c
 ldsflda unsigned int16 ILGEN_0x3c109d11::field_0x1
 ldind.u2
 shr.un
 rem
 ldc.i8 0xbefd79945ea0a168
 ldloc local_0x1
 ldarg Arg_0x0
 shr.un
Start_Orphan_27:
 nop
End_Orphan_27:
 conv.i8
 mul.ovf
Start_Orphan_2a:
 ldloc local_0x4
 ldc.i4.5
 ldelema [mscorlib]System.Single
 ldc.r8 float64(0xadf5a9ab8a062a30)
 stind.r4
End_Orphan_2a:
 ldc.i4.7
 neg
 conv.ovf.i8
 ldc.i8 0xed6fb300c84940c7
 conv.ovf.u8.un
Start_Orphan_2d:
 nop
End_Orphan_2d:
 ldsfld int64[] ILGEN_0x3c109d11::field_0x0
 ldc.i4.5
 ldelema [mscorlib]System.Int64
 ldind.i8
 mul.ovf
 mul.ovf
 conv.u8
 sub.ovf
 clt
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xb6bbe8b6 
ldc.r8 float64(0x10dc9c5bc5640da4) 
ldc.i4 0x520ab1a8 
ldc.i4 0xd464936f 
ldc.r8 float64(0x2d0a6f3c2213caa6) 

call int32 ILGEN_0x3c109d11::Method_0x52483fac(int16 Arg_0x0, float64 Arg_0x1, unsigned int8 Arg_0x2, int8 Arg_0x3, float64 Arg_0x4)
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
