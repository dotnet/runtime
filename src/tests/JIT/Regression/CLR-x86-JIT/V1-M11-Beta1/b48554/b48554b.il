// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b48554b'
{
}


.class public ILGEN_1235399248 {
 //(Nov 12 1999)15:27:27
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
 .locals ( float32, float32, float32, int32, int64, int32, int32, int32, float64, int32, int32, float32, float64, float32, int64, float32, int32, float64, float64, float64, int32, float64, int32, int32, int32, float64, int32, int64, int32, float32, int64, int32, int32, float32, int32, int32, float64, float64, int64, float32, int32, int32, float64, int32, int32, int32, float64, float32, int32, float32, float32, int32, int64, int32, int32, int32, float32, int32, int32, int32, int64, float64, int64, int32)
  ldc.r4 float32(0x4f7e7b2a)
  stloc 0
  ldc.r4 float32(0x6c0f74ba)
  stloc 1
  ldc.r4 float32(0x4a1727e4)
  stloc 2
  ldc.i4 0x692670db
  stloc 3
  ldc.i8 0x1986250b48493c6f
  stloc 4
  ldc.i4 0x4e4e38be
  stloc 5
  ldc.i4 0x2c613869
  stloc 6
  ldc.i4 0x56fa618e
  stloc 7
  ldc.r8 float64(0x374c249156411f4d)
  stloc 8
  ldc.i4 0x713e4088
  stloc 9
  ldc.i4 0x60437cc
  stloc 10
  ldc.r4 float32(0x6d747fed)
  stloc 11
  ldc.r8 float64(0x25b4521072f2283d)
  stloc 12
  ldc.r4 float32(0x5ad7bc2)
  stloc 13
  ldc.i8 0x62694a366c7a3d4d
  stloc 14
  ldc.r4 float32(0x651f6c0c)
  stloc 15
  ldc.i4 0x75f051dd
  stloc 16
  ldc.r8 float64(0x8994b027d503444)
  stloc 17
  ldc.r8 float64(0x4fca5ede1abf383e)
  stloc 18
  ldc.r8 float64(0x48b365bc511849c)
  stloc 19
  ldc.i4 0x34374f99
  stloc 20
  ldc.r8 float64(0x1a983d923c26405b)
  stloc 21
  ldc.i4 0x34a86094
  stloc 22
  ldc.i4 0x640f4ec6
  stloc 23
  ldc.i4 0x1ac8453c
  stloc 24
  ldc.r8 float64(0x6baf6d4c18b95a5a)
  stloc 25
  ldc.i4 0x74323aa0
  stloc 26
  ldc.i8 0x3108e983df963f
  stloc 27
  ldc.i4 0x8c359bc
  stloc 28
  ldc.r4 float32(0x287034b7)
  stloc 29
  ldc.i8 0x1a3862e861053e75
  stloc 30
  ldc.i4 0x2123104f
  stloc 31
  ldc.i4 0x30fd7fd1
  stloc 32
  ldc.r4 float32(0x2ebe73c3)
  stloc 33
  ldc.i4 0x169e2157
  stloc 34
  ldc.i4 0x52d06884
  stloc 35
  ldc.r8 float64(0x57f86f622cf05464)
  stloc 36
  ldc.r8 float64(0x6312476e3eb411e7)
  stloc 37
  ldc.i8 0x3a0b65f3cc651d
  stloc 38
  ldc.r4 float32(0x5b97710)
  stloc 39
  ldc.i4 0x6cf31034
  stloc 40
  ldc.i4 0x24dc735d
  stloc 41
  ldc.r8 float64(0x51883a391554321b)
  stloc 42
  ldc.i4 0x1fc36492
  stloc 43
  ldc.i4 0x7ff853b6
  stloc 44
  ldc.i4 0x53b478cc
  stloc 45
  ldc.r8 float64(0xd9335561a6e7843)
  stloc 46
  ldc.r4 float32(0x1c14272)
  stloc 47
  ldc.i4 0x5f1025fa
  stloc 48
  ldc.r4 float32(0x50531dff)
  stloc 49
  ldc.r4 float32(0x9ba22e4)
  stloc 50
  ldc.i4 0x4942280d
  stloc 51
  ldc.i8 0x47e364b27cf5303
  stloc 52
  ldc.i4 0x17522db2
  stloc 53
  ldc.i4 0x436c6db6
  stloc 54
  ldc.i4 0x7b5f3f98
  stloc 55
  ldc.r4 float32(0x3af07e98)
  stloc 56
  ldc.i4 0x67bc36cc
  stloc 57
  ldc.i4 0x1d911d2
  stloc 58
  ldc.i4 0x4ab07a3d
  stloc 59
  ldc.i8 0x691850522f135e
  stloc 60
  ldc.r8 float64(0xb758cd47e92c79)
  stloc 61
  ldc.i8 0x64ce5df5637e2ca7
  stloc 62
  ldc.i4 0x81033f7
  stloc 63

          ldloc 38
         conv.u8
        conv.r4
          ldloc 0
          ldloc 13
         stloc 39
        nop
       cgt.un
      conv.i8
     conv.r8
    conv.i
   conv.u8
          ldc.i8 0x1b86514c2e0857e
         neg
          ldc.i4.2
          ldc.i4.4
         stloc 54
        stloc 32
       neg
          ldc.i4.s 12
         conv.i8
        nop
          ldloc 47
         conv.i8
        neg
       and
      pop
          ldc.i4.3
         conv.u8
          ldc.r8 float64(0x6a2e74ad60356f47)
         conv.i8
        add
          ldc.i8 0x510d77b1375976e0
          ldloc 62
         or
        neg
       add
      conv.i8
     and
          ldc.i4 0x5a012d88
          ldc.r4 float32(0x4ba348d0)
         stloc 13
        conv.u1
       conv.u8
      conv.u
          ldloc 47
          ldc.r8 float64(0x6d643d766aea321d)
         pop
          ldc.r8 float64(0x2f9a230b78fa4b97)
          ldc.r8 float64(0x7fa71b3c7edf4555)
         pop
        stloc 37
          ldloc 39
         conv.r4
          ldloc 62
          ldc.i4.4
         stloc 53
        pop
       stloc 11
          ldloc 33
         neg
          ldc.r8 float64(0x417b2a394acd3457)
          ldc.i4.0
         pop
        pop
          ldloc 4
          ldloc 30
         mul
        not
       pop
      cgt
     cgt
    stloc 57
          ldloc 33
          ldc.i4.s 10
         pop
        conv.u4
       conv.u8
          ldc.i4.0
          ldc.i4.s -112
         xor
        conv.u1
       conv.i1
      stloc 53
     nop
    not
   xor
  stloc 38
 conv.u1
nop
ldc.i4.1
sub
ldc.i4 100
add
	ret
}
}
