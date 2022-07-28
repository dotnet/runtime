// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b75890 { }
.assembly extern xunit.core {}
.class ILGEN_0xc40344d2 {
.field static	int64[] field_0x0
.field static	native int field_0x1
.field static	int16[] field_0x5
.method static unsigned int64  Method_0xd62794cc(int64 Arg_0x7) {
.maxstack	19
.locals (int8[] LOCAL_0x5,native int LOCAL_0x8)
 ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x5
 ldc.i4 0x3dcfa08d 
 stloc LOCAL_0x8
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xc40344d2::field_0x0
 ldc.i4 0xee4c379f 
 stsfld native int ILGEN_0xc40344d2::field_0x1
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xc40344d2::field_0x5
 ldsfld native int ILGEN_0xc40344d2::field_0x1
 conv.i1
 ldc.i4.0
 blt.un Branch_0x0
         ldloc LOCAL_0x8
       conv.i4
      conv.ovf.i8.un
         ldarg Arg_0x7
       conv.ovf.u8
      conv.ovf.u8
Start_Orphan_61:
      ldsfld int16[] ILGEN_0xc40344d2::field_0x5
     stsfld int16[] ILGEN_0xc40344d2::field_0x5
End_Orphan_61:
         ldarg Arg_0x7
         ldsfld int64[] ILGEN_0xc40344d2::field_0x0
         ldc.i4.3
        ldelema [mscorlib]System.Int64
       ldind.i8
        ldsfld int64[] ILGEN_0xc40344d2::field_0x0
         ldloc LOCAL_0x5
         ldc.i4.8
        ldelem.i1
       ldelem.i8
      add.ovf.un
     mul.ovf
    sub.ovf.un
   sub.ovf
  conv.ovf.i.un
 conv.ovf.i8.un
 br Branch_0x1
Branch_0x0:
 ldc.i8 9
Branch_0x1:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17
.try {
ldc.i8 0x513e1016876aecd7 
 call unsigned int64  ILGEN_0xc40344d2::Method_0xd62794cc(int64 Arg_0x7)
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
