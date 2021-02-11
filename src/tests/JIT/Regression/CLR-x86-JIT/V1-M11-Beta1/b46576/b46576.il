// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x153960c7 {}
.class ILGEN_0x153960c7 {

.field static	int32[] field_0x0
.field static	int64 field_0x1
.field static	int64[] field_0x2
.field static	unsigned int16 field_0x3
.field static	int16[] field_0x4
.field static	int64[] field_0x5

.method static int32 Method_0xe3ccd7e2(unsigned int16 Arg_0x0, int8 Arg_0x1, unsigned int8 Arg_0x2) {
.maxstack	19

 .locals (unsigned int16 local_0x0,unsigned int8 local_0x1,float64 local_0x2,float64[] local_0x3,int64[] local_0x4)

ldc.i4 0x5f269da4 
stloc local_0x0
ldc.i4 0xcc8b74a9 
stloc local_0x1
ldc.r8 float64(0x7eeb6c61fb4f553b) 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x4

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x153960c7::field_0x0
ldc.i8 0x8c12bd8193de654b 
stsfld int64 ILGEN_0x153960c7::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x153960c7::field_0x2
ldc.i4 0x6f71d5b7 
stsfld unsigned int16 ILGEN_0x153960c7::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x153960c7::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x153960c7::field_0x5


Start_Orphan_0:
Start_Orphan_1:
 ldc.i8 0x46c7651cc0afe23e
 not
 ldc.i4.3
 conv.ovf.i8
 bge Branch_0x8

 ldloca local_0x2
 ldind.r8
 br Branch_0x9

 Branch_0x8:

 ldloca local_0x2
 ldind.r8
 Branch_0x9:

Start_Orphan_4:
 ldsfld int64[] ILGEN_0x153960c7::field_0x5
 stsfld int64[] ILGEN_0x153960c7::field_0x5
End_Orphan_4:
 ldarg Arg_0x2
 conv.r4
 bne.un Branch_0x6

Start_Orphan_5:
 ldsfld int16[] ILGEN_0x153960c7::field_0x4
 ldloc local_0x0
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_5:
 ldc.i8 0x3a1ae004fe4eea63
 ldc.i4.m1
 shl
 conv.ovf.u1
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0xabb01d19347c0907
 ldc.i8 0x3bd06afba3228194
 div.un
 conv.ovf.i4
 Branch_0x7:

Start_Orphan_8:
 ldloc local_0x4
 ldsfld int16[] ILGEN_0x153960c7::field_0x4
 ldlen
 ldsfld int64 ILGEN_0x153960c7::field_0x1
 stelem.i8
End_Orphan_8:
 ldsfld unsigned int16 ILGEN_0x153960c7::field_0x3
 ldloc local_0x3
 ldc.i4.1
 ldelem.r8
 conv.i4  
 mul.ovf
 bge.un Branch_0x4

 ldc.i4.7
 br Branch_0x5

 Branch_0x4:

 ldc.i4 0x33aae86f
 ldsfld unsigned int16 ILGEN_0x153960c7::field_0x3
 conv.ovf.i1.un
 sub.ovf
 Branch_0x5:

Start_Orphan_11:
 ldloca local_0x0
 pop
End_Orphan_11:
pop

End_Orphan_1:
End_Orphan_0:
Start_Orphan_80:
 ldsfld int32[] ILGEN_0x153960c7::field_0x0
 ldc.i4 55
 ldelema [mscorlib]System.Int32
ldc.i4 55
stind.i
End_Orphan_80:
ldc.i4 55
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x49df9393 
ldc.i4 0x5f8404da 
ldc.i4 0x987ae713 

call int32 ILGEN_0x153960c7::Method_0xe3ccd7e2(unsigned int16 Arg_0x0, int8 Arg_0x1, unsigned int8 Arg_0x2)

  pop
  leave out_of_here
} catch [mscorlib]System.IndexOutOfRangeException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
