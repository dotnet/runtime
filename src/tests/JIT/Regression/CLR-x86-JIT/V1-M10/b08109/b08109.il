// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b08109
{
}
.class public ILGEN_941132142 {
 //  spew2 /L16 /d10
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
 .locals ( float64, int32, float64, float32, int32, float64, int32, int64, int64, int64, int32, int32, int32, float32, int32, int64)
  ldc.r8 float64(0x11977df818424f1d)
  stloc 0
  ldc.i4 0x133f2fe1
  stloc 1
  ldc.r8 float64(0x43b5683435761161)
  stloc 2
  ldc.r4 float32(0x12e23f2)
  stloc 3
  ldc.i4 0x662f2d18
  stloc 4
  ldc.r8 float64(0x4ee235045c12d57)
  stloc 5
  ldc.i4 0x199c651f
  stloc 6
  ldc.i8 0x5d271bd574de106a
  stloc 7
  ldc.i8 0x6d9b2f6d1ec91cfd
  stloc 8
  ldc.i8 0x7a141ee368c9c30
  stloc 9
  ldc.i4 0x5403c7b
  stloc 10
  ldc.i4 0x2ee711e
  stloc 11
  ldc.i4 0x22925b2
  stloc 12
  ldc.r4 float32(0x2b35631a)
  stloc 13
  ldc.i4 0x19c283f
  stloc 14
  ldc.i8 0x347f41ae74c05146
  stloc 15

          ldloc 5
          ldc.i4.3
         stloc 11
          ldc.i4.1
         conv.u2
        stloc 4
          ldc.i8 0x60d8126c484f5a0d
          ldloc 8
         pop
        conv.r8
       pop
      nop
     conv.r8
    conv.i8
          ldc.i4.5
         conv.i
          ldloc 13
         conv.i4
        mul
       conv.u8
          ldloc 8
          ldloc 9
         sub
        conv.i4
       conv.u8
      sub
          ldloc 11
          ldc.i4.7
         add
          ldc.i4.2
          ldc.i4.2
         cgt.un
        add
       conv.u
      conv.i4
     stloc 10
          ldloc 15
          ldloc 0
         stloc 0
          ldc.r4 float32(0x1f253dfa)
          ldc.i4.0
         pop
        stloc 3
          ldloc 7
          ldloc 15
         pop
          ldc.i8 0x69fb66ee323f4561
          ldloc 0
         stloc 2
        stloc 7
       stloc 7
          ldloc 7
         conv.r4
        nop
          ldc.i8 0x276d265930713225
         conv.u4
          ldloc 5
          ldc.r8 float64(0x5988593c2bea3472)
         ceq
        cgt.un
       pop
      pop
          ldc.r4 float32(0x69a8358f)
         conv.r8
        neg
          ldloc 15
         not
        not
       stloc 8
      neg
     pop
    or
   xor
  conv.i1
 conv.u4
          ldloc 9
         conv.i4
        conv.r4
       conv.i8
          ldloc 9
         conv.i
          ldloc 8
         not
        stloc 9
          ldc.i4.0
         conv.i
        conv.u
       cgt
      stloc 11
          ldloc 1
         conv.i8
        nop
       neg
          ldloc 2
         conv.r4
          ldc.r4 float32(0x792817ed)
          ldc.r4 float32(0x3fb05f46)
         mul
        add
          ldc.r4 float32(0x60391592)
         neg
          ldc.i4.5
          ldc.i4.4
         clt
        stloc 6
       ceq
      stloc 1
     mul
    conv.r4
          ldloc 2
         conv.r4
          ldloc 12
         conv.r8
        stloc 5
          ldc.r4 float32(0x2b8265ee)
          ldloc 7
         pop
          ldloc 7
         conv.i8
        pop
       cgt.un
      conv.r4
          ldc.r4 float32(0x682f4238)
          ldc.r4 float32(0xbb87586)
         sub
        conv.r4
       neg
      conv.i8
     stloc 15
          ldloc 7
          ldloc 8
         ceq
        conv.u2
       neg
          ldloc 4
         conv.i8
        conv.i8
       conv.i2
      stloc 12
     conv.u1
    stloc 12
   pop
          ldc.i8 0x1f5251e835c4486
         nop
          ldloc 9
          ldc.i8 0x255a651025c34deb
         clt.un
        stloc 1
       neg
          ldc.i8 0x2c53325d3b3c7a
          ldc.r4 float32(0x67e523c4)
         pop
          ldc.i8 0x4ecf5ed937f94cd6
          ldc.r4 float32(0x55e238da)
         pop
        and
          ldc.i8 0x378062b171e01284
          ldc.i8 0x3fca23d01fca77a
         xor
          ldc.i8 0x1f9a540766ce6ea
          ldloc 8
         clt.un
        stloc 11
       or
      clt
     conv.r8
    nop
   conv.r4
  ceq
          ldc.i8 0xd304fa011547afc
         conv.i
        conv.r.un
       neg
      neg
          ldc.i4.8
          ldc.i4.7
         xor
        conv.i
          ldc.i4.6
         conv.r8
          ldloc 5
         conv.u4
        pop
       pop
      conv.r.un
     clt
    conv.i
   conv.r4
          ldloc 10
         conv.r8
          ldloc 7
         conv.r8
        sub
          ldloc 5
         conv.i8
        conv.r8
       pop
          ldc.i4.2
         conv.i
        conv.r.un
          ldloc 5
          ldc.i8 0x778752b83395645b
         pop
          ldloc 13
         conv.r8
        mul
       sub
      mul
          ldloc 13
         neg
       conv.i8
      nop
     conv.r8
    pop
   conv.i
  pop
 pop
sub
ldc.i4 119
sub

ldc.i4 100
add
	ret
}
}
