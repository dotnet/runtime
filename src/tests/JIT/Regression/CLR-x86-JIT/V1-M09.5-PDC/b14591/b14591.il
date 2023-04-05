// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b14591' {}

.class public ILGEN_3109600104 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
 .locals ( float32, int32, int64, int32, int32, int32, int32, float64, int64, int32, float64, int32, float64, int64, int32, int64, int64, int32, int32, int32, int32, int64, float32, int32, int32, int64, float64, int32, int32, int32, float64, int32, float32, float32, int32, float32, float32, int32, float64, int32, int32, int32, int32, float32, float32, int32, float64, int64, float32, int32, float64, float32, int32, int32, int32, float64, float64, float32, int32, float64, int32, int32, int32, int32)
  ldc.r4 float32(0x8ec2a50)
  stloc 0
  ldc.i4 0x5b646e9f
  stloc 1
  ldc.i8 0x19b36b85228a142a
  stloc 2
  ldc.i4 0x7a2637a
  stloc 3
  ldc.i4 0x24c774d
  stloc 4
  ldc.i4 0x6a13743f
  stloc 5
  ldc.i4 0x6b2280f
  stloc 6
  ldc.r8 float64(0x6b32444323841883)
  stloc 7
  ldc.i8 0x70d3538f78f12f8f
  stloc 8
  ldc.i4 0x27bc5d5b
  stloc 9
  ldc.r8 float64(0x651516484c1641de)
  stloc 10
  ldc.i4 0x654078b2
  stloc 11
  ldc.r8 float64(0x521652bb321b7311)
  stloc 12
  ldc.i8 0x5058da83b43ff
  stloc 13
  ldc.i4 0x46fe625d
  stloc 14
  ldc.i8 0x7796532c498ce3d
  stloc 15
  ldc.i8 0x6459661843fc2aa6
  stloc 16
  ldc.i4 0x65026f0b
  stloc 17
  ldc.i4 0x4dfadaa
  stloc 18
  ldc.i4 0x68831064
  stloc 19
  ldc.i4 0x723445be
  stloc 20
  ldc.i8 0x42b0713831ba18f
  stloc 21
  ldc.r4 float32(0x59375e9f)
  stloc 22
  ldc.i4 0x3293ff3
  stloc 23
  ldc.i4 0x5c272278
  stloc 24
  ldc.i8 0x50d55a9912ce30fa
  stloc 25
  ldc.r8 float64(0x44714d3864e84fb)
  stloc 26
  ldc.i4 0x6cbc34f8
  stloc 27
  ldc.i4 0x26114594
  stloc 28
  ldc.i4 0xb34140
  stloc 29
  ldc.r8 float64(0x78b86c9e62f57a87)
  stloc 30
  ldc.i4 0x180f435a
  stloc 31
  ldc.r4 float32(0xbe237d0)
  stloc 32
  ldc.r4 float32(0x7cc2ede)
  stloc 33
  ldc.i4 0x3153313a
  stloc 34
  ldc.r4 float32(0x6aeb50b8)
  stloc 35
  ldc.r4 float32(0x8606cdc)
  stloc 36
  ldc.i4 0x281e58fd
  stloc 37
  ldc.r8 float64(0x543249ffbf23882)
  stloc 38
  ldc.i4 0x152b603a
  stloc 39
  ldc.i4 0x20ff2066
  stloc 40
  ldc.i4 0x714d170b
  stloc 41
  ldc.i4 0x3766182
  stloc 42
  ldc.r4 float32(0xed857e4)
  stloc 43
  ldc.r4 float32(0x6d9243df)
  stloc 44
  ldc.i4 0x5f352d3
  stloc 45
  ldc.r8 float64(0xd3a77327e661a)
  stloc 46
  ldc.i8 0x42c45b852b767f4
  stloc 47
  ldc.r4 float32(0x7a6439a5)
  stloc 48
  ldc.i4 0x620679b5
  stloc 49
  ldc.r8 float64(0x5d76ffe18833573)
  stloc 50
  ldc.r4 float32(0x7755782)
  stloc 51
  ldc.i4 0x2d11d04
  stloc 52
  ldc.i4 0x1ebd27ee
  stloc 53
  ldc.i4 0x1115ace
  stloc 54
  ldc.r8 float64(0x39117bed1205b17)
  stloc 55
  ldc.r8 float64(0x56732ab141616c3c)
  stloc 56
  ldc.r4 float32(0x5ed8764c)
  stloc 57
  ldc.i4 0x6dff789d
  stloc 58
  ldc.r8 float64(0x5ccc219d214ef8)
  stloc 59
  ldc.i4 0x5baa80d
  stloc 60
  ldc.i4 0x4464a67
  stloc 61
  ldc.i4 0x43df3b01
  stloc 62
  ldc.i4 0x122313d8
  stloc 63

          ldloc 49
          ldloc 37
         xor
        conv.u1
       conv.u
          ldc.i4.3
          ldc.i4.8
         sub
          ldc.r8 float64(0x313347f815e04840)
         conv.r8
        stloc 7
          ldc.i4.m1
          ldc.i4.m1
         add
          ldc.i4.7
         conv.i8
        stloc 2
       sub
      add
     conv.u1
          ldc.i4.m1
         conv.u
        conv.i8
       conv.u2
      conv.u8
     conv.u2
    xor
          ldc.i4.1
         conv.r4
        conv.u4
       conv.r.un
      conv.i
          ldc.i4.0
          ldc.i4.s -4
         pop
        not
          ldc.i4.8
          ldc.i4.0
         stloc 31
          ldc.i4.7
         conv.u4
        and
       stloc 28
          ldc.i8 0x42a14eeb58c63da6
          ldc.r4 float32(0x75971eff)
         stloc 48
        conv.i
       conv.u1
      xor
     sub
    conv.u2
   pop
          ldc.i4.7
          ldc.i4.6
         stloc 45
        conv.u
          ldloc 62
          ldc.i4.4
         sub
          ldc.r4 float32(0x39864a13)
          ldloc 28
         stloc 17
        stloc 36
       pop
      conv.r.un
          ldc.i8 0x30d077c266d956c6
          ldc.i4.m1
         stloc 37
          ldc.i4.6
         conv.u4
        pop
       conv.i2
      conv.u
     pop
          ldloc 59
          ldloc 55
         add
        conv.i
          ldloc 35
         ckfinite
        conv.i
       xor
      conv.i
     conv.i4
    pop
          ldc.i4.1
         conv.r8
        ckfinite
          ldc.r8 float64(0x28ae4ce172d675d2)
         ckfinite
        nop
       sub
      ckfinite
          ldc.i4.7
         conv.r4
        conv.r8
          ldc.r8 float64(0x4dd85551ff7fea)
         conv.r8
          ldloc 32
         ckfinite
        stloc 36
       sub
      neg
     sub
          ldc.i4.4
         conv.i
          ldc.i4.1
         conv.u2
        add
          ldc.i4.0
         conv.u8
        conv.u1
       stloc 61
      conv.u2
          ldloc 46
         conv.i
          ldc.i4.2
          ldc.i4.7
         add
        xor
       conv.u4
          ldc.i4.4
          ldc.r4 float32(0x4d093ab7)
         stloc 32
        conv.u2
          ldc.i4.1
         conv.r4
          ldc.i8 0x18537034d6f69a3
         nop
        pop
       stloc 36
      xor
     stloc 54
    stloc 18
   add
  stloc 10
 conv.i1
          ldloc 2
          ldc.i8 0x220758687af227bf
         stloc 8
          ldc.r8 float64(0x4efc454f7418643)
          ldloc 28
         pop
        pop
          ldc.i8 0x1a3f66693636635c
         conv.i8
          ldc.i8 0x76e29e97454355d
          ldc.r8 float64(0x60525d4e66ff3304)
         pop
        pop
       xor
          ldc.i4.3
          ldloc 40
         stloc 60
          ldloc 15
         conv.u2
        and
       conv.i8
      stloc 15
     conv.i
    conv.r8
   neg
          ldc.i4.m1
          ldloc 27
         add
        conv.i
          ldc.r8 float64(0x443e49e2266692d)
         conv.i8
        conv.i2
       mul
      conv.r4
          ldc.r4 float32(0x7e9d62ba)
         conv.u4
          ldloc 52
          ldc.i4.4
         or
        and
       conv.i
          ldc.r4 float32(0x5dcf569d)
          ldloc 47
         pop
        nop
          ldloc 59
         conv.r4
        conv.r4
       mul
      pop
     pop
          ldloc 26
          ldc.r8 float64(0x7e994daf142d6fd8)
         pop
        conv.i
       conv.r8
          ldloc 38
          ldloc 55
         mul
          ldc.r8 float64(0x3dfc431857bc2cd)
          ldc.i4.4
         stloc 23
        mul
          ldloc 38
          ldloc 38
         stloc 12
          ldc.i4.5
         conv.u1
        stloc 19
       mul
      stloc 12
     conv.r4
    sub
          ldc.i4.8
         conv.i8
          ldc.i4.2
          ldc.r4 float32(0x2c125aa1)
         stloc 44
        pop
          ldloc 25
         conv.i8
        conv.i2
       stloc 5
          ldloc 9
          ldc.i4.0
         stloc 45
          ldc.r4 float32(0x69a1a71)
         conv.r4
        stloc 22
       conv.i8
      mul
          ldloc 46
         conv.r4
        ckfinite
       ckfinite
          ldc.r8 float64(0x43021d8c775076aa)
         conv.i8
          ldc.i8 0x73c3366d5ec8180f
         not
        mul
       conv.r4
      stloc 0
     stloc 43
    conv.i
   pop
  pop
 conv.i4
and

	dup
	sub
ldc.i4 100
add
	ret
}
}
