// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28522'
{
}


.class public ILGEN_3171381355 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
 .locals ( int32, int64, float64, int32, int64, float64, int32, int64, float32, float32, int32, int32, float64, int64, float32, float64, float32, float64, float32, float64, int64, int64, int64, float32, int64, float64, int32, float64, int32, int64, float64, float32, int32, int32, int32, float64, int64, int32, int32, float32, int32, int32, int64, int32, int32, int64, int32, int64, float64, int32, int32, int32, int32, int64, float32, float32, int32, float32, int64, int32, int32, int32, int32, int64)
  ldc.i4 0x679f671
  stloc 0
  ldc.i8 0x1a43684e28102765
  stloc 1
  ldc.r8 float64(0x6c37391f20e24001)
  stloc 2
  ldc.i4 0x4a681a32
  stloc 3
  ldc.i8 0x4797c112691b84
  stloc 4
  ldc.r8 float64(0x599b376343205caa)
  stloc 5
  ldc.i4 0x6684541e
  stloc 6
  ldc.i8 0x306075e334787932
  stloc 7
  ldc.r4 float32(0x6f13b5e)
  stloc 8
  ldc.r4 float32(0x7274ac2)
  stloc 9
  ldc.i4 0x39a9820
  stloc 10
  ldc.i4 0x5c3542e1
  stloc 11
  ldc.r8 float64(0x5958169729885f0)
  stloc 12
  ldc.i8 0x78f297f61607ef8
  stloc 13
  ldc.r4 float32(0x16d07cc8)
  stloc 14
  ldc.r8 float64(0x6e694c4b531123cb)
  stloc 15
  ldc.r4 float32(0x109135db)
  stloc 16
  ldc.r8 float64(0x67c13a0f4b107dfa)
  stloc 17
  ldc.r4 float32(0x2ca3364f)
  stloc 18
  ldc.r8 float64(0x61e0627d471528d9)
  stloc 19
  ldc.i8 0x1182255e44c91316
  stloc 20
  ldc.i8 0x5eac10ad1125169e
  stloc 21
  ldc.i8 0x2c8b7e755e00119
  stloc 22
  ldc.r4 float32(0x5f831515)
  stloc 23
  ldc.i8 0x4a2f255645381ec9
  stloc 24
  ldc.r8 float64(0x72217a6d67dbdb6)
  stloc 25
  ldc.i4 0x216576
  stloc 26
  ldc.r8 float64(0x18a1e66f95368d)
  stloc 27
  ldc.i4 0x6caa2b99
  stloc 28
  ldc.i8 0x6557812331623b
  stloc 29
  ldc.r8 float64(0x3af77acf5f9f71f0)
  stloc 30
  ldc.r4 float32(0x60464a02)
  stloc 31
  ldc.i4 0x47437d7d
  stloc 32
  ldc.i4 0x59ad160
  stloc 33
  ldc.i4 0x459b160a
  stloc 34
  ldc.r8 float64(0x43ca66cd73662b9d)
  stloc 35
  ldc.i8 0x76585882035e9
  stloc 36
  ldc.i4 0x4175423f
  stloc 37
  ldc.i4 0x17092140
  stloc 38
  ldc.r4 float32(0x19b82de0)
  stloc 39
  ldc.i4 0xca73a4c
  stloc 40
  ldc.i4 0x52ae17ff
  stloc 41
  ldc.i8 0x1a487bbe400120ad
  stloc 42
  ldc.i4 0x42342c9d
  stloc 43
  ldc.i4 0x1b1c1859
  stloc 44
  ldc.i8 0x540c6ea12829656a
  stloc 45
  ldc.i4 0x3f5fa85
  stloc 46
  ldc.i8 0x7feb32d6132232ad
  stloc 47
  ldc.r8 float64(0x5fbd765317bf5cc)
  stloc 48
  ldc.i4 0x1ff5cbd
  stloc 49
  ldc.i4 0x6b2455d1
  stloc 50
  ldc.i4 0x65313623
  stloc 51
  ldc.i4 0x4695551a
  stloc 52
  ldc.i8 0x7da87b3f4c1d4533
  stloc 53
  ldc.r4 float32(0x4bc474b0)
  stloc 54
  ldc.r4 float32(0x43632d30)
  stloc 55
  ldc.i4 0x4ff84619
  stloc 56
  ldc.r4 float32(0x7bb1689)
  stloc 57
  ldc.i8 0x65814af1af05121
  stloc 58
  ldc.i4 0x6a36275f
  stloc 59
  ldc.i4 0x3299581c
  stloc 60
  ldc.i4 0x62c12047
  stloc 61
  ldc.i4 0x24d82ff7
  stloc 62
  ldc.i8 0x6cef33e885d2159
  stloc 63

          ldloc 29
         conv.i
          ldc.i4.6
         conv.i
        and
       conv.u8
          ldc.r4 float32(0x402a4454)
         conv.i8
        nop
          ldloc 42
          ldloc 58
         cgt
        conv.r4
       pop
      xor
     conv.u
          ldc.i8 0x32ae275f77e23f47
          ldloc 18
         pop
          ldc.i8 0x13a01aaf0c41af
          ldloc 7
         stloc 1
        xor
          ldc.i8 0x80d1ab041a357c6
          ldloc 47
         and
          ldc.r8 float64(0x78423dc29451268)
          ldc.i4.1
         pop
        stloc 2
       pop
      conv.i
          ldloc 23
          ldc.r4 float32(0x20797031)
         pop
        conv.r4
          ldloc 18
          ldc.i4.6
         pop
        nop
       clt.un
      conv.u
     ceq
    sub
          ldc.r8 float64(0x1468702711701f2c)
         conv.i8
        nop
       conv.r.un
          ldloc 21
          ldloc 31
         pop
          ldloc 2
         conv.i8
        or
          ldloc 8
         conv.i8
          ldc.i8 0x20592fa01f302b3c
         conv.i2
        stloc 26
       or
      stloc 29
     conv.r4
    conv.u4
   stloc 40
          ldloc 56
         conv.i8
        neg
       conv.u
          ldloc 25
          ldc.i4.8
         stloc 0
          ldc.i4.0
         conv.r.un
        sub
          ldc.r8 float64(0x660646af6f6c5579)
          ldloc 59
         stloc 26
          ldloc 31
          ldloc 14
         sub
        pop
       clt.un
      mul
          ldc.i4.3
         conv.u
        conv.r4
          ldloc 34
         conv.i
          ldc.i4.6
          ldc.i4.7
         mul
        clt
       stloc 50
      conv.r4
     stloc 9
    conv.r8
   conv.i
  xor
          ldloc 11
         conv.r.un
          ldc.r8 float64(0x41b17f464609e87)
          ldc.i4.3
         pop
        mul
          ldloc 12
          ldc.i4.m1
         stloc 38
        conv.i
       pop
          ldc.r8 float64(0x75eb379574f6bb2)
         conv.i8
        not
       conv.u2
      stloc 44
     ckfinite
    conv.i4
   conv.u4
  not
 cgt
          ldloc 14
         conv.i
          ldloc 19
         conv.r4
        stloc 23
       conv.u8
          ldloc 28
         not
        conv.i8
          ldloc 53
         neg
        neg
       stloc 63
      stloc 22
          ldloc 1
          ldc.i8 0x6d45f92412330c4
         cgt.un
        conv.i8
          ldc.i8 0x2de2614a755875cc
         conv.i8
          ldloc 7
          ldc.i8 0x47bf6d862f643458
         pop
        stloc 36
       sub
          ldc.r8 float64(0x46345aba3fdd14dc)
         ckfinite
          ldloc 19
          ldc.i8 0x3cf66c79444e87a
         stloc 22
        pop
       conv.i8
      xor
     ceq
          ldc.i4.5
         conv.i8
        conv.r4
          ldc.r8 float64(0x26473f9d200453d)
          ldloc 39
         stloc 23
          ldloc 20
          ldc.i8 0x615e35aa61531415
         pop
        pop
       stloc 12
          ldc.i4.1
         conv.r.un
          ldc.r8 float64(0x1aa65f8972a740e6)
         neg
        sub
       conv.i
      pop
          ldloc 34
         conv.i2
        conv.r.un
          ldc.i8 0x2d037a1c23151c43
          ldloc 36
         sub
          ldloc 29
          ldloc 42
         pop
        mul
       stloc 4
          ldloc 40
         conv.i4
        conv.r.un
          ldloc 11
          ldc.i4.1
         pop
        conv.i1
       pop
      cgt
     pop
    pop
   conv.i4
          ldc.i8 0x4db6ff074e52e25
         conv.u4
        conv.r8
       conv.i8
      nop
          ldloc 24
          ldloc 5
         stloc 15
        conv.r.un
       conv.i
      conv.u2
     stloc 34
          ldc.r4 float32(0xd18e09)
         conv.u4
        conv.i2
       conv.u8
          ldc.i8 0x7fb63c0351e04638
          ldloc 17
         stloc 12
        nop
          ldc.i4.0
          ldc.i4.8
         sub
        conv.i
       pop
      xor
          ldc.r8 float64(0x5e067aa04dc5033)
          ldloc 27
         sub
          ldloc 2
         conv.r4
        pop
          ldloc 9
          ldc.i4.s -90
         stloc 51
        neg
       pop
          ldc.i4.m1
          ldc.i4.0
         add
        conv.r8
          ldc.r4 float32(0x7a4d4aa6)
          ldc.i8 0xf897f4025253865
         stloc 53
          ldloc 23
         neg
        cgt
       stloc 3
      stloc 2
     pop
    mul
          ldc.r8 float64(0xb11101044ef2163)
          ldc.r8 float64(0x2af6211d5f7f4bd0)
         mul
          ldloc 1
          ldloc 20
         add
        pop
          ldc.i4.2
         conv.u
          ldc.i4.2
          ldloc 51
         pop
        clt.un
       stloc 28
      conv.i8
     not
          ldc.i4.3
          ldc.i4 0x9bd3b08
         cgt
        conv.u
       conv.i8
          ldc.i8 0x36601a8b58ad3337
          ldloc 13
         sub
          ldloc 12
         neg
        stloc 17
       conv.i4
      stloc 26
          ldc.i8 0x35257eaa201f4f2f
         not
          ldc.i8 0x38c17bd220338cf
          ldloc 47
         sub
        xor
          ldloc 54
         conv.i
        conv.i8
       xor
      nop
     ceq
    pop
   ceq
  clt
          ldc.i8 0x535f28d84bb63a56
         conv.u2
          ldc.i4.2
          ldc.i4.1
         cgt
        and
          ldc.i8 0x67f526cc77d16cf8
          ldc.r8 float64(0x7185761472ee441d)
         pop
        conv.u2
       add
      conv.i2
     conv.u8
          ldc.i4.m1
         conv.i4
        conv.u8
       conv.i
      conv.r.un
     conv.i8
    and
   conv.u
          ldc.i8 0x3c35717e380c1575
         conv.i2
        conv.u2
          ldc.i4.8
          ldc.i4.5
         pop
          ldc.i4.3
          ldc.i4.1
         add
        mul
       mul
      conv.i
     conv.u1
    conv.r8
   conv.i8
  stloc 58
 sub
clt.un
	dup
	sub
ldc.i4 100
add
	ret
}
}
