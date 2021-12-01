// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x71d10697 {}
.assembly extern xunit.core {}
.class ILGEN_0x71d10697 {
.method static unsigned int64  Method_0x77f6a2f8() {
.maxstack	21
Start_Orphan_3b:
         ldc.i8 0xb4ecb82048e13e92
         ldc.i8 0xbfb8a94151035091
        clt.un
         ldc.i4.0
     clt.un
   pop
End_Orphan_3b:
        ldc.i8 0x13d297fd24797ea5
          ldc.i4.7
         conv.i8
       clt
          ldc.i4.s 122
        neg
          ldc.i4.3
         conv.u1
          ldc.i8 0x7afe9814bd561a01
         conv.i4
        ceq
       sub
      sub
          ldc.i8 0xb59388516bc15ca6
         conv.i8
          ldc.i8 0x1831d6f7495d77c7
          ldc.i8 0x1a3a74929f3a885d
         and
        sub
          ldc.i4.4
        conv.u8
       clt.un
          ldc.i4.0
          ldc.i4.6
         and
        conv.u4
          ldc.i4.0
        conv.i2
       shr.un
      add
     sub
          ldc.i4.2
         conv.u
          ldc.i8 0x966c60cdce3d4a04
          ldc.i8 0xa0ba985252691d77
         ceq
       sub
          ldc.i4.s -28
          ldc.i4.6
          ldc.i4.0
         add
       sub
      cgt
     conv.i1
    mul
   neg
Start_Orphan_65:
Start_Orphan_66:
        ldc.i8 0xfbb06645a164f2c5
        ldc.i8 0x1c1c39b29f65c2ee
       cgt.un
      pop
End_Orphan_66:
Start_Orphan_68:
        newobj     instance void [mscorlib]System.Object::.ctor()
       pop
End_Orphan_68:
         ldc.i4.7
         ldc.i4.4
        sub
       conv.i1
Start_Orphan_69:
        ldc.i4.0
       pop
End_Orphan_69:
         ldc.i8 0x1cdba11738e85a08
         ldc.i8 0xeb5cc634905e0a67
        and
       conv.u
      cgt
     pop
End_Orphan_65:
Start_Orphan_6b:
         ldc.i4.7
        neg
         ldc.i4.8
         ldc.i4.0
        cgt.un
       sub
      pop
End_Orphan_6b:
          ldc.i4.7
      conv.u
          ldc.i8 0x4c5823441f353f3b
          ldc.i8 0xdd29fdfb6a1ec281
         cgt.un
          ldc.i4.8
          ldc.i4.5
         ceq
        mul
     sub
        ldc.i4.m1
       conv.i4
          ldc.i8 0xf25005b250de705e
         conv.i8
        conv.i4
          ldc.i4 0x2d46d56e
         conv.u
        conv.u1
       clt
      sub
          ldc.i8 0x583f514f262c1d41
         conv.u
          ldc.i4.1
          ldc.i4.2
         clt.un
       sub
     xor
    and
   conv.u
  xor
 conv.u8
not
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2

 call unsigned int64  ILGEN_0x71d10697::Method_0x77f6a2f8()
conv.i4
ldc.i4.s 26
sub
 ret
}

}
