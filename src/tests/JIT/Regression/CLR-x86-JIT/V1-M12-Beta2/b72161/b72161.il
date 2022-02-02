// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b72161 { }
.assembly extern xunit.core {}
.class ILGEN_0x27d73bb9 {
.field static	int64[] field_0x0
.field static	int64 field_0x1
.field static	int32[] field_0xa
.method static int32  Method_0xfc75e117(native int Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2, unsigned int16 Arg_0x3, native int Arg_0x4, int32 Arg_0x5, int8 Arg_0x6, int8 Arg_0x7, unsigned int16 Arg_0x8, int32 Arg_0x9, unsigned int8 Arg_0xa, int8 Arg_0xb, int16 Arg_0xc) {
.maxstack	15

ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x27d73bb9::field_0x0
ldc.i8 0xe0ac6544fdf34b09 
stsfld int64 ILGEN_0x27d73bb9::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x27d73bb9::field_0xa

   ldsflda int64 ILGEN_0x27d73bb9::field_0x1
  ldind.i8
 conv.ovf.i1
  ldc.i4.6
Start_Orphan_4:
    ldsfld int64[] ILGEN_0x27d73bb9::field_0x0
     ldsfld int32[] ILGEN_0x27d73bb9::field_0xa
      ldc.r8 float64(0x6267bbcfcc7573f7)
     conv.u1
    ldelem.i4
      ldsfld int64 ILGEN_0x27d73bb9::field_0x1
     conv.ovf.i4
    conv.i8
   stelem.i8
End_Orphan_4:
Start_Orphan_7:
      ldc.i8 0x84ef3cf27d899026
      ldsfld int64 ILGEN_0x27d73bb9::field_0x1
     or
    starg Arg_0x2
End_Orphan_7:
mul
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	27
.try {
ldc.i4 0xc599eda5 conv.i
ldc.i4 0xcac473c 
ldc.i8 0x9179278dba912e67 
ldc.i4 0xbb3d8e6 
ldc.i4 0x9d7aba5f conv.i
ldc.i4 0x9f75a549 
ldc.i4 0x8f629710 
ldc.i4 0xff78b757 
ldc.i4 0xb8d60a56 
ldc.i4 0xa062e7e2 
ldc.i4 0x5c437067 
ldc.i4 0x7942210b 
ldc.i4 0x1fd242f0 

 call int32  ILGEN_0x27d73bb9::Method_0xfc75e117(native int Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2, unsigned int16 Arg_0x3, native int Arg_0x4, int32 Arg_0x5, int8 Arg_0x6, int8 Arg_0x7, unsigned int16 Arg_0x8, int32 Arg_0x9, unsigned int8 Arg_0xa, int8 Arg_0xb, int16 Arg_0xc)
conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}
}
