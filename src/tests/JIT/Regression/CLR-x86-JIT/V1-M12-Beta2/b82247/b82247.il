// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x8259d717 {}
.assembly extern xunit.core {}
.class ILGEN_0x8259d717 {

.field static	unsigned int32[] field_0x0
.field static	unsigned int64[] field_0x1
.field static	float32[] field_0x2
.field static	unsigned int64[] field_0x3
.field static	unsigned int8 field_0x4
.field static	int64[] field_0x5
.field static	unsigned int64[] field_0x6

.method static unsigned int16  Method_0xea14afe6() {
.maxstack	17

 .locals (unsigned int32[] LOCAL_0x0,float32 LOCAL_0x1)

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x0
ldc.r4 float32(0xdb8ee420) 
stloc LOCAL_0x1

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x8259d717::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x8259d717::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x8259d717::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x8259d717::field_0x3
ldc.i4 0xfd628b6a 
stsfld unsigned int8 ILGEN_0x8259d717::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x8259d717::field_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x8259d717::field_0x6

Start_Orphan_0:
  ldloc LOCAL_0x0
 stsfld unsigned int32[] ILGEN_0x8259d717::field_0x0
End_Orphan_0:
   ldc.i4 0xbdab5db7
  conv.ovf.i
        ldc.i4.m1
       conv.ovf.u1
      conv.r8
     conv.i
        ldc.i4.m1
       conv.r.un
      conv.ovf.i
       ldc.i4 0xc3e769a4
      conv.i
     or
    mul
   conv.ovf.i2.un
      ldsflda unsigned int8 ILGEN_0x8259d717::field_0x4
     ldind.u1
Start_Orphan_10:
       ldsflda unsigned int8 ILGEN_0x8259d717::field_0x4
      pop
End_Orphan_10:
        ldc.i8 0x4b8e82c52db365e4
        ldc.i8 0x44397a46261200ef
       sub.ovf
      conv.r4
     conv.i2
    rem.un
Start_Orphan_12:
Start_Orphan_13:
      ldc.i4.7
     stsfld unsigned int8 ILGEN_0x8259d717::field_0x4
End_Orphan_13:
       ldc.r4 float32(0xdd6e7b2d)
       ldc.r8 float64(0xa0b8a33378e7228d)
      div 
     conv.ovf.u8
    pop
End_Orphan_12:
Start_Orphan_17:
        ldsfld unsigned int32[] ILGEN_0x8259d717::field_0x0
        ldc.i4.7
       ldelema [mscorlib]System.UInt32
      pop
End_Orphan_17:
        ldsflda unsigned int8 ILGEN_0x8259d717::field_0x4
       ldind.u1
      conv.i2
     not
        ldc.i4.s 96
       conv.ovf.u8
      conv.ovf.i4.un
        ldloc LOCAL_0x1
       call float64 [mscorlib]System.Math::Sin(float64)
      conv.i4
     rem
    rem.un
   clt.un
  add
 or
Start_Orphan_1c:
Start_Orphan_1d:
Start_Orphan_1e:
     ldsfld int64[] ILGEN_0x8259d717::field_0x5
     ldc.i4 0x4b43ae30
    ldelema [mscorlib]System.Int64
     ldc.i8 0x4ece4cccbcc35e40
     ldc.i8 0xa9f2afc11d390efb
    rem
   stind.i8
End_Orphan_1e:
      ldc.i4.4
     conv.ovf.u1
    conv.u1
 newarr [mscorlib]System.Single
       ldsfld unsigned int32[] ILGEN_0x8259d717::field_0x0
       ldc.i4.4
      ldelema [mscorlib]System.UInt32
     ldind.u4
      ldsfld unsigned int8 ILGEN_0x8259d717::field_0x4
      ldc.i4.6
     cgt
    shr.un
   ldelem.r4
  pop
End_Orphan_1d:
   ldsfld float32[] ILGEN_0x8259d717::field_0x2
       ldc.r4 float32(0x2507ce8)
      conv.ovf.u8
     conv.r.un
    conv.i8
   conv.ovf.i4
  ldelema [mscorlib]System.Single
       ldc.i4.0
      conv.u1
     conv.ovf.u
    conv.r8
   call float64 [mscorlib]System.Math::Sin(float64)
  call float64 [mscorlib]System.Math::Sin(float64)
 stind.r4
End_Orphan_1c:
Start_Orphan_28:
       ldsfld unsigned int64[] ILGEN_0x8259d717::field_0x1
       ldc.i4.2
      ldelem.u8
     conv.u1
    conv.ovf.i2.un
       ldc.i8 0x2db83a4d2bb3c480
       ldc.i8 0x6ce5df031042f521
      div.un
     conv.ovf.u.un
       ldc.i4.4
       ldc.i4.m1
      mul.ovf
       ldc.i8 0x53e6d6e324508910
      conv.ovf.u.un
     sub.ovf.un
    add.ovf.un
   sub
  pop
End_Orphan_28:
        ldc.i4.8
        ldc.i4 0x6e9ef415
       mul
        ldc.i4.1
       neg
      rem
        ldc.r8 float64(0x3add01e04ffbc416)
        ldc.r8 float64(0xde0aaf0d7cc19ad4)
       rem 
      conv.ovf.u1
     clt
        ldc.r8 float64(0x40c9e52b3e42609e)
        ldc.r8 float64(0xbefd3c00191a0855)
       mul 
      conv.r4
     conv.ovf.u1
    mul.ovf
Start_Orphan_39:
       ldsfld int64[] ILGEN_0x8259d717::field_0x5
       ldc.i4.m1
      ldelema [mscorlib]System.Int64
     pop
End_Orphan_39:
         ldsfld unsigned int64[] ILGEN_0x8259d717::field_0x6
         ldc.i4.m1
        ldelema [mscorlib]System.UInt64
       ldind.u8
      conv.ovf.u8.un
     conv.ovf.i8.un
    conv.i4
   div.un
        ldc.i4.5
        ldc.i4.2
       rem
        ldc.i4.m1
        ldc.i4.s -55
       cgt.un
      mul
        ldc.i4.8
        ldc.i4.0
       rem
      conv.u1
     add
Start_Orphan_3f:
       ldsfld int64[] ILGEN_0x8259d717::field_0x5
       ldc.i4.m1
      ldelema [mscorlib]System.Int64
       ldc.i8 0xf1482bab80f51ecf
       ldc.i4.4
      shr
     stind.i8
End_Orphan_3f:
        ldc.i4.3
       conv.u1
        ldc.i4.3
       conv.ovf.u1.un
      ceq
       ldc.i4.7
        ldc.i4.m1
        ldc.i4.6
       div
      div
     add.ovf
    rem.un
        ldc.i4.s 19
       conv.u8
        ldc.i4.s -45
       conv.ovf.i8.un
      sub.ovf
        ldc.i4.s -75
       conv.i8
        ldc.i8 0xa0ee8efa1bcd05bf
        ldc.i8 0x33678d8aa429c49e
       rem.un
      or
     cgt.un
      ldc.i4.3
     conv.ovf.u1.un
    div.un
   rem
  div.un
 neg
ceq
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	1
.try {
 call unsigned int16  ILGEN_0x8259d717::Method_0xea14afe6()
conv.i4
  leave.s IL_BAD
} catch [mscorlib]System.OverflowException {
  leave.s IL_GOOD
}
IL_GOOD:
ldc.i4.s 100
 ret
IL_BAD:
ldc.i4.s 101
 ret
}

}
