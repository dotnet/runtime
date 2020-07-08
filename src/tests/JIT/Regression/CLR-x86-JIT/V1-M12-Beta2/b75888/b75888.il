// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b75888 { }
.class ILGEN_0x981b6a55 {

.field static	int32 field_0x2
.method static float32  Method_0x9d35bca7() {
.maxstack	15

 .locals (float64 LOCAL_0x2,native int[] LOCAL_0x3,unsigned int64 LOCAL_0x6,int16 LOCAL_0x8)
ldc.r8 float64(0x79790e4f8ea2f32d) 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x3
ldc.i8 0x94bb9f7af3853caa 
stloc LOCAL_0x6
ldc.i4 0x11baf42 
stloc LOCAL_0x8

ldc.i4 0xfcdc4879 
stsfld int32 ILGEN_0x981b6a55::field_0x2

       ldc.i4.5
      conv.ovf.u4.un
     conv.ovf.u2.un
      ldc.i4.s -12
       ldc.i4.4
       ldc.i4.8
      mul.ovf.un
     rem.un
    div.un
    ldsfld int32 ILGEN_0x981b6a55::field_0x2
   add.ovf
      ldloc LOCAL_0x3
      ldsfld int32 ILGEN_0x981b6a55::field_0x2
     ldelema [mscorlib]System.IntPtr
    ldind.i
   conv.ovf.i1
  clt
 brfalse Branch_0x2

ldc.r4 44.5
br Branch_0x3

 Branch_0x2:

Start_Orphan_c:
 ldc.i4 6 
 Loop_0x0: //WHAT
      ldloc LOCAL_0x6
     conv.u4
      ldsfld int32 ILGEN_0x981b6a55::field_0x2
     conv.ovf.u1.un
    shr
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_c:
       ldloc LOCAL_0x2
 Branch_0x3:

       ldloc LOCAL_0x8
      not
     conv.i8
    conv.ovf.i8.un
  conv.r.un
 conv.r8
 blt.un Branch_0x0
ldc.r4 55.6
br Branch_0x1
 Branch_0x0:
ldc.r4 44
Branch_0x1:

 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	1
.try {
 call float32  ILGEN_0x981b6a55::Method_0x9d35bca7()
 conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.IndexOutOfRangeException {
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