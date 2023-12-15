// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b15222'
{
}


.class public ILGEN_4017387519 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
 .locals ( int32, float64, int32, int32, float32, int32, int32, float64, float64, int32, int64, int32, int64, int32, int64, float64, int32, int32, int64, int32, int64, float64, float64, int32, float32, float64, int32, float64, int32, float64, int64, int32, float64, int32, int32, int32, int64, int64, float64, int64, int64, int32, int64, int32, int64, int64, float32, int64, int32, int64, int32, float64, float32, float32, int64, int64, int64, int32, float64, float64, int32, int32, float32, int32)
  ldc.i4 0x6bf91b17
  stloc 0
  ldc.r8 float64(0x19a2210b72f51d4)
  stloc 1
  ldc.i4 0x2835418b
  stloc 2
  ldc.i4 0x2aa85eb5
  stloc 3
  ldc.r4 float32(0x20922ba8)
  stloc 4
  ldc.i4 0x3f5d26c1
  stloc 5
  ldc.i4 0x8cd2643
  stloc 6
  ldc.r8 float64(0x12bf5f5a571e5f28)
  stloc 7
  ldc.r8 float64(0x379a282127337eef)
  stloc 8
  ldc.i4 0x58f61411
  stloc 9
  ldc.i8 0x733765ba72a4a99
  stloc 10
  ldc.i4 0x7c5a5d64
  stloc 11
  ldc.i8 0x7af99d872e06b5
  stloc 12
  ldc.i4 0x56d56c28
  stloc 13
  ldc.i8 0x231e5c7f5abe7852
  stloc 14
  ldc.r8 float64(0x1f484aa6238a56cd)
  stloc 15
  ldc.i4 0x5eee2db3
  stloc 16
  ldc.i4 0x5408717b
  stloc 17
  ldc.i8 0xc22438322ed76f2
  stloc 18
  ldc.i4 0x3a032b53
  stloc 19
  ldc.i8 0xc663a085c892db
  stloc 20
  ldc.r8 float64(0x2e8b1e8377c81572)
  stloc 21
  ldc.r8 float64(0x38ec88f50a06976)
  stloc 22
  ldc.i4 0x4ff37e0c
  stloc 23
  ldc.r4 float32(0x4d094a99)
  stloc 24
  ldc.r8 float64(0x6bac24e21e16444)
  stloc 25
  ldc.i4 0x5e304c4c
  stloc 26
  ldc.r8 float64(0xff172e4722f6c)
  stloc 27
  ldc.i4 0x46872cd1
  stloc 28
  ldc.r8 float64(0x6ab13fd96060311)
  stloc 29
  ldc.i8 0xe9031e6690d4016
  stloc 30
  ldc.i4 0x7139ba0
  stloc 31
  ldc.r8 float64(0x1a831c4111242840)
  stloc 32
  ldc.i4 0x408f2e20
  stloc 33
  ldc.i4 0x40c4781f
  stloc 34
  ldc.i4 0x591c2507
  stloc 35
  ldc.i8 0x7581f5411377ee
  stloc 36
  ldc.i8 0x546169161cdd561b
  stloc 37
  ldc.r8 float64(0x171531f6a1f4e4a)
  stloc 38
  ldc.i8 0x5dac145a49b61103
  stloc 39
  ldc.i8 0x6430755e72c82fa8
  stloc 40
  ldc.i4 0x2a5364ab
  stloc 41
  ldc.i8 0x79cf1bfd52b3a42
  stloc 42
  ldc.i4 0x456a1eb3
  stloc 43
  ldc.i8 0x4d017845390f5fca
  stloc 44
  ldc.i8 0x719023076f697d63
  stloc 45
  ldc.r4 float32(0x5016767c)
  stloc 46
  ldc.i8 0x55db129735c41c5a
  stloc 47
  ldc.i4 0x62204003
  stloc 48
  ldc.i8 0x1afd5b7e41e924c0
  stloc 49
  ldc.i4 0x203597b
  stloc 50
  ldc.r8 float64(0x41de20fb17cfb95)
  stloc 51
  ldc.r4 float32(0x5fab1832)
  stloc 52
  ldc.r4 float32(0x2bcd994)
  stloc 53
  ldc.i8 0x1c9e1e732f064272
  stloc 54
  ldc.i8 0x4ebe1e8d6ac038fa
  stloc 55
  ldc.i8 0x40a41fd54461268e
  stloc 56
  ldc.i4 0x73a2ee0
  stloc 57
  ldc.r8 float64(0x292f6da55a6d72b2)
  stloc 58
  ldc.r8 float64(0x1eab4bee16c459b0)
  stloc 59
  ldc.i4 0x33056976
  stloc 60
  ldc.i4 0x67f43fb6
  stloc 61
  ldc.r4 float32(0x2aa224f8)
  stloc 62
  ldc.i4 0x69a99b7
  stloc 63

          ldc.i4.7
         conv.i2
        conv.i8
          ldloc 32
         conv.r4
          ldc.i4.s -59
         conv.r4
        sub
       stloc 62
      conv.i
     conv.u2
          ldc.r4 float32(0x18418ea)
          ldc.i4.1
         pop
        ckfinite
       conv.u4
          ldc.i4.8
         conv.i1
        neg
          ldc.i4.1
         conv.i2
          ldc.i4.3
         conv.r8
        stloc 59
       stloc 9
      pop
          ldc.i4.3
          ldc.i4.m1
         add
          ldloc 37
          ldc.i4.5
         pop
        pop
          ldloc 55
          ldc.i8 0x288f67c47125580
         mul
          ldloc 55
          ldc.i8 0x131d45835103a4f
         add
        sub
       pop
      conv.r4
     stloc 53
    sub
          ldc.r8 float64(0x20526a0e2f3b21ca)
         neg
        conv.u4
          ldc.i4.2
          ldloc 23
         stloc 19
          ldloc 8
         conv.u4
        stloc 28
       pop
      nop
          ldc.i4.6
         conv.u2
        conv.i
       conv.u2
          ldc.r4 float32(0x390a4728)
         conv.u4
          ldc.i4.2
          ldc.r4 float32(0x5a7a2b67)
         stloc 62
        stloc 33
          ldloc 32
         conv.i4
          ldc.r8 float64(0x58a33a63529e180e)
         nop
        pop
       stloc 34
      stloc 11
     or
          ldc.r4 float32(0x3df57f9c)
         conv.i
        conv.i4
          ldc.i4.s -70
         conv.r8
          ldloc 38
          ldc.r8 float64(0x4ef5c4f20e04b13)
         mul
        mul
       pop
      conv.u8
     conv.r.un
    pop
   stloc 5
          ldc.r4 float32(0x17c62f33)
          ldc.r4 float32(0x8c93579)
         pop
          ldc.i4.4
         conv.r8
        stloc 8
          ldc.r8 float64(0x4f821f832d3f1751)
          ldc.r8 float64(0x5ef49995bf2250)
         pop
          ldloc 60
          ldloc 53
         stloc 52
        stloc 60
       stloc 38
          ldc.i8 0x56781caa5e7a5fab
          ldloc 36
         pop
          ldc.r4 float32(0x56672752)
         nop
        pop
          ldc.i8 0x461c25c83eb267af
         conv.u
        conv.r4
       stloc 46
      stloc 12
     conv.i
    conv.u
   conv.u2
  or
          ldloc 12
         conv.i8
          ldc.i8 0x2dc21c57c9471f6
          ldloc 0
         stloc 33
        add
       nop
          ldc.i4.5
          ldc.i4.3
         mul
          ldc.i4.5
         conv.i
        pop
       neg
      stloc 63
          ldc.r4 float32(0x578c5166)
          ldc.r8 float64(0x3412270dda7b1b)
         stloc 32
        neg
          ldc.r4 float32(0x8944af1)
          ldloc 60
         pop
          ldc.r4 float32(0x77984034)
         conv.r4
        pop
       add
          ldc.i8 0x8d62b9c2313672f
          ldc.r8 float64(0x69dc161811c4a5c)
         stloc 22
        nop
       conv.u4
      pop
     stloc 4
          ldloc 19
          ldc.i4.s -118
         stloc 61
          ldc.i8 0x498e583522c132af
          ldloc 56
         stloc 18
        pop
          ldc.i4.6
          ldc.i4 0x11179a8
         and
        conv.i
       add
      conv.r4
     neg
    pop
   not
          ldc.i8 0x5ce4468666d3a7e
         conv.r.un
          ldc.i4.6
          ldc.i4 0x38c46d89
         and
        stloc 13
       ckfinite
      conv.i4
          ldc.i4.7
         conv.u
          ldc.i4.m1
          ldloc 62
         stloc 24
        or
       conv.u2
          ldloc 18
          ldloc 30
         mul
        conv.u1
          ldloc 3
         conv.u2
          ldloc 41
         conv.i1
        stloc 13
       and
      mul
     stloc 23
    conv.r.un
          ldc.i8 0x6e703e1e345d76a2
         conv.r8
          ldc.r8 float64(0x1a491e4b76bc7164)
          ldc.r4 float32(0x5ca74203)
         stloc 53
        stloc 29
          ldloc 12
         conv.r4
          ldc.i4 0x7a854728
          ldc.i4.0
         stloc 48
        stloc 5
       pop
          ldc.i4.s -96
          ldc.r8 float64(0x22217e4469256371)
         pop
        conv.r8
       conv.r4
      stloc 24
     ckfinite
          ldloc 10
          ldc.i8 0x2a33c2560188c0
         stloc 36
          ldc.i8 0x5e0b15c934ca6a2f
         not
        xor
       conv.i2
      conv.i4
     conv.u2
    stloc 50
   stloc 7
  pop
 pop
conv.u1

    ldc.i4 7
    sub
ldc.i4 100
add
	ret
}
}
