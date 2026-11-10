// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test description

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly DevDiv_591210 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_591210
       extends [System.Runtime]System.Object
{
  .method private hidebysig static uint64 
          ILGEN_METHOD(int64 a,
                       uint64 b,
                       bool c,
                       int16 d) cil managed
  {
	.maxstack  215
      .locals init (int64, int32, unsigned int8, int8, unsigned int64, unsigned int16, int16, native int, bool, float64, unsigned int8, bool, int16, bool, float32)

	IL_0000: ldloc 0x0009
	IL_0004: conv.ovf.i8
	IL_0005: ldloc 0x000a
	IL_0009: conv.r8
	IL_000a: conv.ovf.i8
	IL_000b: xor
	IL_000c: ldarg.s 0x00
	IL_000e: pop
	IL_000f: ldloc.s 0x0c
	IL_0011: ldloc 0x0006
	IL_0015: mul.ovf
	IL_0016: conv.i8
	IL_0017: not
	IL_0018: pop
	IL_0019: neg
	IL_001a: ldloc.s 0x0e
	IL_001c: ldloc.s 0x09
	IL_001e: add
	IL_001f: ldc.r8 float64(0x283484984cfb905b)
	IL_0028: rem
	IL_0029: ldloc.s 0x09
	IL_002b: ceq
	IL_002d: ldloc.s 0x09
	IL_002f: ldloc 0x0009
	IL_0033: clt
	IL_0035: conv.ovf.u8
	IL_0036: pop
	IL_0037: pop
	IL_0038: nop
	IL_0039: ldloc.s 0x09
	IL_003b: ckfinite
	IL_003c: ckfinite
	IL_003d: conv.u8
	IL_003e: conv.ovf.i8
	IL_003f: ldloc.s 0x04
	IL_0041: pop
	IL_0042: stloc 0x0000
	IL_0046: ldloc 0x000e
	IL_004a: conv.i2
	IL_004b: ldloc.s 0x0d
	IL_004d: neg
	IL_004e: conv.r8
	IL_004f: ldloc.s 0x09
	IL_0051: cgt.un
	IL_0053: xor
	IL_0054: ldloc.s 0x09
	IL_0056: conv.ovf.u1
	IL_0057: mul
	IL_0058: ldc.r8 float64(0x53a4ba0aaef9974f)
	IL_0061: ldc.r8 float64(0xaf4298fd3c695a11)
	IL_006a: cgt.un
	IL_006c: rem
	IL_006d: neg
	IL_006e: conv.ovf.i8
	IL_006f: conv.ovf.i8
	IL_0070: bgt 
	IL_009b
	IL_0075: ldloc.s 0x0e
	IL_0077: ldloc 0x0009
	IL_007b: div
	IL_007c: conv.ovf.u8.un
	IL_007d: ldloc.s 0x08
	IL_007f: not
	IL_0080: shr
	IL_0081: conv.r.un
	IL_0082: conv.r8
	IL_0083: neg
	IL_0084: neg
	IL_0085: conv.ovf.i1
	IL_0086: ldarg 0x0002
	IL_008a: conv.r4
	IL_008b: conv.r8
	IL_008c: dup
	IL_008d: dup
	IL_008e: div
	IL_008f: clt
	IL_0091: ldloc.s 0x0c
	IL_0093: starg.s 0x03
	IL_0095: pop
	IL_0096: neg
	IL_0097: conv.r8
	IL_0098: nop
	IL_0099: conv.ovf.i4.un
	IL_009a: pop
	IL_009b: ldc.r8 float64(0x98c5ef60f979ba96)
	IL_00a4: ldloc.s 0x09
	IL_00a6: ldloc.s 0x09
	IL_00a8: rem
	IL_00a9: rem
	IL_00aa: ckfinite
	IL_00ab: ckfinite
	IL_00ac: pop
	IL_00ad: ldc.i8 0x4780e434f6aa6979
	IL_00b6: ldloc.s 0x0e
	IL_00b8: conv.u8
	IL_00b9: ldloc 0x0004
	IL_00bd: xor
	IL_00be: conv.ovf.u8.un
	IL_00bf: ldloc 0x0000
	IL_00c3: ldloc 0x0009
	IL_00c7: conv.ovf.u2
	IL_00c8: ldloc 0x0002
	IL_00cc: mul.ovf
	IL_00cd: neg
	IL_00ce: shr
	IL_00cf: conv.r4
	IL_00d0: pop
	IL_00d1: clt
	IL_00d3: neg
	IL_00d4: not
	IL_00d5: conv.i8
	IL_00d6: ret
  } // end of method DevDiv_591210::ILGEN_METHOD

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       30 (0x1e)
    .maxstack  4
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4.s   100
      IL_0004:  conv.i8
      IL_0005:  ldc.i4.0
      IL_0006:  conv.i8
      IL_0007:  ldc.i4.0
      IL_0008:  ldc.i4.1
      IL_0009:  call       uint64 DevDiv_591210::ILGEN_METHOD(int64,
                                                              uint64,
                                                              bool,
                                                              int16)
      IL_000e:  pop
      IL_000f:  nop
      IL_0010:  leave.s    IL_0017

    }  // end .try
    catch [System.Runtime]System.Object 
    {
      IL_0012:  pop
      IL_0013:  nop
      IL_0014:  nop
      IL_0015:  leave.s    IL_0017

    }  // end handler
    IL_0017:  ldc.i4.s   100
    IL_0019:  stloc.0
    IL_001a:  br.s       IL_001c

    IL_001c:  ldloc.0
    IL_001d:  ret
  } // end of method DevDiv_591210::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DevDiv_591210::.ctor

} // end of class DevDiv_591210


