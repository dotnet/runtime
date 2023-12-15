// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b06595'
{
}


.class public ILGEN_939264028 {
 //(Sep 30 1999)14:45:45
 //  spew2 /L /d15
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	16
 .locals ( int32, int64, float32, float64, int32)

               ldc.i4.0
     conv.i8
               ldloc 0
        conv.i8
               ldc.i4.6
            conv.i8
         conv.i8
               ldloc 1
             conv.u8
            conv.i4
               ldc.i4.s -48
               ldc.i4.7
              conv.u2
             cgt.un
               ldc.r8 float64(0x796d4c845a1f73f7)
             conv.i
            cgt.un
           ceq
         conv.u
               ldc.r4 float32(0x46313870)
               ldc.i4.6
              stloc 0
               ldc.i4.8
              conv.r4
             stloc 2
            conv.r4
               ldc.i4.6
            conv.r4
           cgt.un
          conv.i4
               ldloc 3
          conv.u
         clt
        cgt
       stloc 4
      mul
     conv.u8
    or
               ldc.i8 0x67a529f334b9258a
               ldc.i4.1
            conv.r8
               ldloc 1
            conv.r.un
           clt
          conv.r4
         stloc 2
               ldloc 0
               ldc.i4.8
              cgt.un
               ldloc 3
              conv.u
             add
               ldloc 1
             conv.i4
            stloc 0
               ldc.r8 float64(0xe373b14780f694a)
              conv.u
               ldc.i4.s -87
ldc.i4 4
stloc 4
             mul
               ldc.i4 0x727b79a8
              conv.r4
             conv.i
            mul
           clt
          conv.i8
               ldloc 1
              conv.u2
               ldc.i8 0x22475642350177e0
              stloc 1
               ldc.r8 float64(0x5a623b2049023b6e)
             conv.u
            ceq
               ldc.i4.s 52
               ldc.i4.8
             clt
           ceq
          conv.i8
         sub
               ldc.i8 0x6aae3ec54bb5522
              conv.r.un
            conv.r4
               ldloc 2
              conv.r8
             conv.r4
               ldc.i4.1
             conv.r4
            cgt
           stloc 0
          conv.r4
        pop
       sub
      conv.i8
               ldloc 3
       conv.u8
               ldloc 1
             conv.r.un
           conv.r8
               ldc.i8 0x1ee04ce6aad3071
          stloc 1
         conv.i
               ldc.i4.s -127
           conv.r4
               ldloc 1
              conv.r8
           conv.r4
               ldloc 2
               ldc.r8 float64(0x25f271e46ea57284)
               ldc.r8 float64(0x5d312e9e6b602db8)
             cgt.un
               ldc.r8 float64(0x3ae6dfd77a261c1)
              conv.u
               ldloc 4
             cgt.un
            pop
           stloc 4
          sub
         sub
        stloc 2
       conv.i4
      stloc 4
               ldc.i8 0x24ea28dea3bbc0
            conv.u
          conv.r4
               ldc.r8 float64(0x44355f872f765a41)
               ldc.i4.7
              stloc 4
             conv.i4
               ldloc 2
             conv.i2
            clt.un
          conv.r4
         stloc 2
               ldc.i4.8
              conv.u8
             conv.r4
               ldc.i4.0
               ldc.i8 0x3a0f51afb6a40ec
              stloc 1
            conv.r.un
           pop
          conv.r4
        mul
               ldloc 2
              conv.i
             conv.u
               ldc.r8 float64(0x4a0d1c179cd588d)
               ldloc 3
              cgt
               ldc.r4 float32(0x688a2ccf)
              conv.u4
             ceq
            conv.i2
          cgt.un
         conv.i
        conv.u8
       stloc 1
               ldc.r4 float32(0x7e305d52)
              conv.r4
               ldc.i4.2
             conv.r4
               ldc.i4.s 99
              conv.r.un
         stloc 3
        stloc 2
      stloc 2
     pop
    and
   clt.un
               ldc.i4 0x44f8262b
              conv.r4
           conv.u8
               ldc.i4.6
              conv.u2
           stloc 4
               ldc.i4.7
          conv.i8
               ldloc 4
               ldloc 0
              clt.un
               ldloc 3
               ldc.r8 float64(0x363a72e9392624b3)
              cgt.un
             sub
               ldloc 4
              conv.u4
               ldc.i8 0x231c5b14241d4e3d
              stloc 1
               ldc.i4.s -6
               ldc.r4 float32(0x7c1e752d)
              conv.u
             cgt
            conv.i2
           cgt.un
          stloc 4
         pop
        and
               ldloc 1
              conv.i
             conv.i8
               ldc.i4.8
               ldc.i8 0x525448f76e787535
              stloc 1
             conv.r.un
            stloc 3
               ldc.r8 float64(0x5d6c482f38421d0c)
               ldc.i4.5
              stloc 0
             conv.i
           conv.i2
          stloc 0
               ldloc 3
              conv.u2
            conv.r.un
               ldc.i8 0x7db647872c0d4e34
               ldc.i8 0x4d6c2e633c8aa41
              conv.u8
             cgt
           stloc 0
         stloc 3
               ldc.i4.8
              conv.r8
             conv.i
            conv.u
               ldc.i4.8
              conv.r8
             conv.u4
               ldc.i4.7
               ldc.i4.6
              clt.un
            add
           pop
          conv.i2
               ldc.r4 float32(0x5b7170ef)
               ldc.i8 0x2bb05c9d6f5a3d6e
              stloc 1
             conv.r8
            conv.u2
               ldc.i8 0x50f5521c537c1b6b
               ldc.i8 0x591e155e276f56ad
              clt.un
           mul
         ceq
        stloc 0
       ceq
      conv.i8
     conv.u
               ldc.r8 float64(0x45ec6b083faa4d44)
             conv.i4
            conv.i8
               ldc.i8 0x63db5717516fa85
           stloc 1
          conv.r4
               ldloc 1
               ldc.i4.3
               ldc.i4.m1
              stloc 4
             conv.r8
            conv.i1
           stloc 0
          conv.i2
         pop
        conv.r4
       conv.u1
      conv.r4
     conv.i4
    mul
   conv.u4
  clt
 conv.u1
               ldloc 0
               ldc.i4.3
               ldc.i4.6
               ldc.i8 0x1d3b670c4bb0570a
              stloc 1
             clt.un
            xor
               ldc.i4.s -73
            conv.i2
           stloc 0
          conv.i4
         conv.u8
               ldc.i8 0x7d223bb56d8b3db5
               ldloc 2
               ldc.r4 float32(0x67006d3c)
              clt
             conv.r4
            stloc 2
           conv.i8
         conv.u
        pop
       conv.u1
      conv.u8
     conv.u8
    conv.r4
               ldloc 2
             conv.i2
            conv.u2
               ldc.i4.m1
              conv.r4
               ldloc 2
            cgt
           mul
          conv.u
               ldc.r8 float64(0x37155d182296a0a)
            conv.u8
           conv.u8
               ldc.r8 float64(0x368f79b266f34537)
              conv.r8
             conv.r8
               ldc.i4.0
               ldloc 3
               ldc.r8 float64(0x14bc67f028803323)
              ceq
             clt.un
            pop
           conv.r4
          stloc 2
         stloc 1
               ldc.i4.s -38
              conv.u4
               ldloc 2
               ldc.r4 float32(0x1af365d2)
              clt.un
             conv.i2
               ldc.r4 float32(0x6374618e)
              conv.i4
             conv.u
            mul
           add
               ldc.i8 0x24313df26cf8592
               ldloc 1
               ldc.i8 0x48af52b33e33545f
              clt
               ldc.r4 float32(0x336aff4)
              conv.u2
             or
            pop
          stloc 1
         conv.r8
        pop
       conv.i2
      conv.r4
               ldc.r4 float32(0x3d3559ce)
              conv.r4
             conv.i2
           conv.r.un
               ldloc 1
              conv.r8
               ldc.i4.7
              conv.r8
             cgt
            conv.u8
               ldc.i4.2
             conv.u
            conv.u8
           clt.un
          stloc 4
         conv.u8
       conv.u8
     stloc 1
               ldc.i4.s -96
               ldc.i8 0x4bd6634b6d9028d7
              conv.r4
             stloc 2
            conv.i1
           conv.r.un
               ldloc 1
              conv.r4
               ldloc 2
              conv.r4
             clt
               ldloc 1
               ldc.i8 0x767097c7a17764d
              ceq
            cgt.un
               ldc.r8 float64(0x1f915cdb6dbf5bd9)
              conv.u4
               ldc.i4.2
             conv.u2
            cgt.un
           cgt.un
          pop
         conv.u
       conv.i1
               ldloc 3
             conv.r4
               ldc.i8 0x48676cfd2f065b1
              conv.u8
               ldc.i4.0
              conv.u1
             conv.u8
            cgt
           conv.i8
               ldloc 3
              conv.i8
             conv.u8
            conv.r4
           conv.i8
          cgt
               ldc.i8 0x5a376d91446612c9
              conv.i
               ldc.r4 float32(0x74ec353f)
              conv.u2
             cgt
               ldc.i4.s -119
            cgt.un
           conv.u1
               ldloc 2
              conv.u1
             conv.r.un
           conv.u
          cgt
         mul
        pop
       conv.i1
      stloc 0
    pop
   mul
               ldc.i4.s 94
         conv.u8
               ldc.i4.4
             conv.r8
               ldc.r8 float64(0x7082460e72ce5447)
            mul
          conv.i8
               ldc.i8 0x46c031db1b7d3182
               ldc.r4 float32(0x56fe2286)
               ldc.r4 float32(0x7d8910e0)
              stloc 2
             pop
               ldc.i8 0x224338d430e858be
               ldc.i8 0x6f24c0732ff3cd1
             stloc 1
            clt.un
           conv.i1
          conv.u8
         mul
        clt
       conv.u8
      conv.r8
     conv.u8
               ldc.i4.7
        conv.u
               ldloc 0
              conv.u
               ldc.i4.8
              conv.i
             clt
           conv.i2
          conv.r4
               ldc.i4.s 31
               ldc.i4.4
              stloc 0
               ldloc 3
             conv.i
            cgt
          conv.r4
         pop
               ldc.i4.s -46
             conv.r4
           conv.r4
       cgt.un
      stloc 0
               ldc.i4.m1
          conv.u1
               ldloc 2
            conv.r4
           conv.i
               ldc.r8 float64(0x2f224556335272df)
              conv.r4
               ldc.r4 float32(0x3600469)
             clt
           conv.u1
          clt.un
         ceq
      conv.i4
     stloc 4
    pop
               ldc.i4.0
             conv.i2
           conv.i2
               ldc.r4 float32(0x5591420a)
               ldloc 1
              conv.r8
               ldc.i4 0x3e574a47
              conv.r8
             cgt
            stloc 0
           conv.u
          ceq
         conv.r8
        conv.r4
       conv.r4
               ldc.i4.7
              conv.u
               ldc.i4.m1
              conv.i
             clt.un
               ldloc 2
               ldc.i4.3
              stloc 4
            conv.i2
           sub
               ldloc 3
           conv.u
          clt.un
               ldc.i4.6
              conv.r8
               ldc.i4.8
              conv.r.un
             clt.un
               ldc.r8 float64(0x39c9764a5ad24424)
               ldc.r8 float64(0x54196aca67b27562)
            clt.un
           and
          conv.u4
         cgt
        conv.i8
       conv.r4
      stloc 2
               ldloc 2
               ldc.i8 0x4424e4365c716bc
               ldloc 1
               ldc.i4.1
              conv.r4
            conv.u2
           stloc 4
          sub
         pop
               ldloc 1
          conv.u
               ldc.i4 0x2007534a
             conv.i4
               ldloc 2
            conv.i2
               ldloc 2
              conv.u1
             conv.r4
               ldloc 2
               ldc.r4 float32(0x9526f3b)
              cgt.un
             conv.r4
            add
           conv.i2
          cgt.un
         add
        ceq
               ldloc 1
              conv.r8
               ldloc 4
             conv.r8
          cgt.un
               ldc.r8 float64(0x12d15fd76bce1887)
               ldloc 3
            conv.r.un
           cgt.un
               ldc.r8 float64(0x4da1465172163764)
           conv.i
          cgt
         mul
       add
      pop
     clt
   pop
  stloc 1
pop

ldc.i4 100
add
	ret
}
}

