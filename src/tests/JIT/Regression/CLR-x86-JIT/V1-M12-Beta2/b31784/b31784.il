// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b31784' {}
.assembly extern xunit.core {}
.class ILGEN_0x9d6df124 {

//BEGIN STATIC FIELDS
.field static	float32 field_0x0
.field static	unsigned int16 field_0x1
.field static	unsigned int8 field_0x2
//END STATIC FIELDS
.method static int32 Method_0x450(int64 Arg_0x0, int16 Arg_0x1) {
.maxstack	17

 .locals (int8 local_0x0,int32 local_0x1,unsigned int16 local_0x2,unsigned int64[] local_0x3,int64 local_0x4)
//BEGIN INIT LOCALS
ldc.i4 0x748a49b 
stloc local_0x0
ldc.i4 0x1cd41cbc 
stloc local_0x1
ldc.i4 0x1c5072 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i8 0x4390b1d505c1f00 
stloc local_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.r4 float32(0x75662085) 
stsfld float32 ILGEN_0x9d6df124::field_0x0
ldc.i4 0x636f7773 
stsfld unsigned int16 ILGEN_0x9d6df124::field_0x1
ldc.i4 0x3fda2c83 
stsfld unsigned int8 ILGEN_0x9d6df124::field_0x2


Start_Orphan_0:
 ldarga Arg_0x0
 ldind.i8
 stloc local_0x4
End_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldsflda unsigned int16 ILGEN_0x9d6df124::field_0x1
 pop
End_Orphan_2:
Start_Orphan_6:
 ldloc local_0x3
 ldc.i4.s 72
 ldarg Arg_0x0
 stelem.i8
End_Orphan_6:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.UInt64
 ldloc local_0x2
 ldelem.u8
 conv.r8
 conv.ovf.i1
 stloc local_0x2
End_Orphan_1:
Start_Orphan_7:
Start_Orphan_8:
 ldloc local_0x3
 ldc.i4.2
 ldc.i8 0x1db42879776e3dc5
 stelem.i8
End_Orphan_8:
 ldsflda float32 ILGEN_0x9d6df124::field_0x0
 ldind.r4
 stsfld float32 ILGEN_0x9d6df124::field_0x0
End_Orphan_7:
Start_Orphan_b:
 ldloca local_0x0
 pop
End_Orphan_b:
 ldc.i4.3
 conv.r4
 ldloc local_0x0
 conv.ovf.i1
 conv.r4
 mul
 conv.r4
Start_Orphan_c:
 ldc.r8 float64(0x26976908174e4a6a)
 conv.i4
 stsfld unsigned int8 ILGEN_0x9d6df124::field_0x2
End_Orphan_c:
 ldsflda float32 ILGEN_0x9d6df124::field_0x0
 ldind.r4
 div
 conv.i1
 ldsfld float32 ILGEN_0x9d6df124::field_0x0
 conv.r4
 conv.ovf.i4
 ldc.i8 0x1677f2b2221701b
 conv.ovf.i1.un
Start_Orphan_18:
 ldc.i4.0
 stloc local_0x2
End_Orphan_18:
 ldc.r8 float64(0xfd56f1ad831ee1)
 ckfinite
 conv.i2
 conv.ovf.u4
 sub.ovf.un
 sub.ovf
 conv.ovf.u.un
 sub
 ldc.r8 float64(0x42122af3f271c7b)
 conv.u8
 conv.r8
 conv.u
 conv.i
 conv.ovf.i.un
Start_Orphan_1e:
 ldloc local_0x3
 ldc.i4.0
 ldc.i4 0x4d2e7fc0
 sub.ovf.un
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1e:
 ldloc local_0x0
Start_Orphan_21:
 ldsflda float32 ILGEN_0x9d6df124::field_0x0
 ldsfld float32 ILGEN_0x9d6df124::field_0x0
 stind.r4
End_Orphan_21:
 ldloc local_0x0
 conv.ovf.u
Start_Orphan_28:
 ldc.i4.7
 stloc local_0x0
End_Orphan_28:
 sub.ovf.un
 sub
 conv.ovf.i1.un
 cgt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x21c066afeea5623 
ldc.i4 0x7851f5b 

call int32 ILGEN_0x9d6df124::Method_0x450(int64 Arg_0x0, int16 Arg_0x1)
 pop
 leave the_end
 } catch [mscorlib]System.IndexOutOfRangeException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}

}
