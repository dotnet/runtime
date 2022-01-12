// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xfa70290a {}
.assembly extern xunit.core {}
.class ILGEN_0xfa70290a {
.method static unsigned int8  Method_0x8f046f0b(unsigned int16 Arg_0x0, int8 Arg_0x1, int8 Arg_0x2, native int Arg_0x3, unsigned int64 Arg_0x4) {
.maxstack	11
Start_Orphan_0:
Start_Orphan_1:
   ldarga Arg_0x2
   ldc.i4.3
  stind.i1
End_Orphan_1:
    ldarg Arg_0x3
    ldc.i4.2
   add
    ldarg Arg_0x3
    ldarg Arg_0x3
   or
  or
Start_Orphan_2:
   ldarga Arg_0x0
   ldc.i4.1
  stind.i2
End_Orphan_2:
    ldarg Arg_0x4
    ldc.i8 0xf8c6c94af2160a3d
   add
  conv.i4
 sub
pop
End_Orphan_0:
Start_Orphan_4:
  ldarga Arg_0x3
   ldarga Arg_0x3
  ldind.i
 stind.i
End_Orphan_4:
Start_Orphan_5:
   ldarga Arg_0x0
    ldc.i4.m1
    ldarg Arg_0x0
   ceq
  stind.i2
End_Orphan_5:
Start_Orphan_8:
    ldarga Arg_0x0
    ldc.i4.1
   stind.i2
End_Orphan_8:
    ldarg Arg_0x4
     ldc.i4.3
     ldc.i4.0
    mul
   shr
Start_Orphan_a:
    ldc.i4.m1
   starg Arg_0x2
End_Orphan_a:
     ldarg Arg_0x4
    conv.u1
   conv.u8
  xor
Start_Orphan_b:
    ldarg Arg_0x4
   not
  starg Arg_0x4
End_Orphan_b:
Start_Orphan_c:
    ldarga Arg_0x4
    ldarg Arg_0x4
   stind.i8
End_Orphan_c:
     ldc.i8 0xca7854bf8baf83c7
     ldc.i4.1
    shr
     ldarg Arg_0x4
     ldc.i8 0xa395033520987c05
    clt
   shr.un
     ldarg Arg_0x4
     ldc.i8 0xbba0451077d0eff0
    xor
   neg
  mul
 or
     ldarg Arg_0x4
     ldc.i4.8
    shr
   neg
    ldarga Arg_0x4
   ldind.u8
  and
     ldarg Arg_0x4
    conv.i8
     ldarg Arg_0x4
    conv.i4
   shr
  conv.u8
 add
cgt.un
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
ldc.i4 0x1ebe67ca 
ldc.i4 0xffa718d 
ldc.i4 0x4b248cb1 
ldc.i4 0x8f04322d conv.i
ldc.i8 0xf76449ff4d06d175 

 call unsigned int8  ILGEN_0xfa70290a::Method_0x8f046f0b(unsigned int16 Arg_0x0, int8 Arg_0x1, int8 Arg_0x2, native int Arg_0x3, unsigned int64 Arg_0x4)
conv.i4
ldc.i4 99
add
 ret
}

}
