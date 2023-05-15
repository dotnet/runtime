// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b11413
{
}

.class public ILGEN_382872040 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
 .locals ( float32, int32, int32, float32, int32, int32, int32, int32, int32, float32, float32, float32, int32, int64, int64, int32, int64, float64)
  ldc.r4 float32(0x68a62d75)
  stloc 0
  ldc.i4 0x1ab129a2
  stloc 1
  ldc.i4 0x25b2486c
  stloc 2
  ldc.r4 float32(0x72c05eef)
  stloc 3
  ldc.i4 0x232f20a2
  stloc 4
  ldc.i4 0x6dff5e5d
  stloc 5
  ldc.i4 0x45df1e85
  stloc 6
  ldc.i4 0x74485f7f
  stloc 7
  ldc.i4 0x7a7f1edd
  stloc 8
  ldc.r4 float32(0x4f7652e7)
  stloc 9
  ldc.r4 float32(0x9e53167)
  stloc 10
  ldc.r4 float32(0x401a35e8)
  stloc 11
  ldc.i4 0x734b2ac0
  stloc 12
  ldc.i8 0x40ebd854a7053a1
  stloc 13
  ldc.i8 0xacceb94f911736
  stloc 14
  ldc.i4 0x46e95361
  stloc 15
  ldc.i8 0x2bec232a1bdb3967
  stloc 16
  ldc.r8 float64(0x740547f26d3d7588)
  stloc 17

         ldc.i8 0x20cf28b66be47626
        conv.u2
       conv.i8
         ldc.i4.1
        conv.i
       conv.i8
      xor
         ldloc 3
         ldloc 13
        stloc 14
       conv.r4
      conv.r4
     stloc 9
         ldc.r8 float64(0x7ec33a8470f84ff4)
        conv.r4
       conv.i
      conv.u8
         ldc.i8 0x6f5c507365fa1b51
         ldloc 14
        add
       conv.u8
         ldc.r8 float64(0x4df26db11370384f)
        ckfinite
       conv.i8
      sub
     add
    sub
         ldc.i4.8
         ldc.i4 0x7f3ba52
        mul
         ldc.i4 0x15d71e0f
         ldc.i4.2
        clt
       add
         ldc.r8 float64(0x4bb54f6b4b09ae3)
         ldc.r8 float64(0x62f353418472b6)
        cgt.un
         ldloc 7
         ldc.i8 0x17f78032245a14
        stloc 16
       sub
      add
         ldc.i4.m1
        conv.u2
         ldc.i4.4
         ldc.i4.7
        add
       ceq
         ldc.i4.3
         ldc.i4.1
        cgt
         ldc.r4 float32(0x6d1a1f7c)
        conv.i4
       xor
      stloc 1
     stloc 7
    conv.i8
   sub
  conv.i8
 conv.i4
         ldloc 13
         ldc.i4.m1
        stloc 1
       conv.u8
      conv.u8
     nop
    conv.r8
   nop
  conv.i
 conv.i1
pop

	pop
	ldc.i4 100
	ret
}
}
