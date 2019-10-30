// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b80043 { }
.class ILGEN_0x7a14cd08 {
.field static	unsigned int32 field_0x0
.method static float64  Method_0xe36037f1(unsigned int16 Arg_0x0, int32 Arg_0x1, int16 Arg_0x2, float32 Arg_0x3, float32 Arg_0x4) {
.maxstack	9
 nop nop nop nop nop nop nop nop nop nop nop nop nop 
 ldc.i4 0xaaf53595 
 stsfld unsigned int32 ILGEN_0x7a14cd08::field_0x0

Start_Orphan_8:
   ldc.i4.0
 newarr [mscorlib]System.Byte
  ldc.i4.5
   ldsfld unsigned int32 ILGEN_0x7a14cd08::field_0x0
   ldsfld unsigned int32 ILGEN_0x7a14cd08::field_0x0
  shl
 stelem.i1
End_Orphan_8:
 ldc.r4 100.0
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	11
.try {
 ldc.i4 0x13cd6ee5 
 ldc.i4 0xab972627 
 ldc.i4 0xbcd8558 
 ldc.r4 float32(0xaa92aaf1) 
 ldc.r4 float32(0xeddc519f) 
 call float64  ILGEN_0x7a14cd08::Method_0xe36037f1(unsigned int16 Arg_0x0, int32 Arg_0x1, int16 Arg_0x2, float32 Arg_0x3, float32 Arg_0x4)
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