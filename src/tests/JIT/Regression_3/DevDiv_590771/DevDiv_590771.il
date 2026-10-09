// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_590771 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_590771
       extends [System.Runtime]System.Object
{
  .method static int8 ILGEN_METHOD(int64, char, native unsigned int, unsigned int32, char, native int)
  {
    .maxstack  174
    .locals init (float64, int32, unsigned int32, native unsigned int, unsigned int32, int16, int64, native unsigned int, int32)
    IL_0000: ldarg 0x0000
    IL_0004: conv.i8
    IL_0005: conv.ovf.u2.un
    IL_0006: dup
    IL_0007: ldarg.s 0x00
    IL_0009: conv.ovf.u4.un
    IL_000a: mul.ovf.un
    IL_000b: not
    IL_000c: neg
    IL_000d: shr
    IL_000e: neg
    IL_000f: conv.ovf.u8
    IL_0010: not
    IL_0011: conv.r.un
    IL_0012: ldloc.s 0x05
    IL_0014: ldarg.s 0x02
    IL_0016: sub.ovf
    IL_0017: stloc.s 0x05
    IL_0019: neg
    IL_001a: ldc.i8 0xf5848c133a994bd5
    IL_0023: ldc.i8 0x3744a843932f26f5
    IL_002c: not
    IL_002d: add
    IL_002e: ldc.i8 0xf869128fbacc441f
    IL_0037: conv.ovf.u1.un
    IL_0038: conv.ovf.i8.un
    IL_0039: rem.un
    IL_003a: conv.ovf.i4
    IL_003b: conv.r8
    IL_003c: ckfinite
    IL_003d: ceq
    IL_003f: ldloc 0x0003
    IL_0043: dup
    IL_0044: clt.un
    IL_0046: mul
    IL_0047: nop
    IL_0048: brtrue 
    IL_00a2
    IL_004d: ldc.i8 0xcfa1a742b8879a20
    IL_0056: ldarg.s 0x00
    IL_0058: ldarg.s 0x01
    IL_005a: ldarg 0x0001
    IL_005e: shl
    IL_005f: conv.u8
    IL_0060: conv.ovf.u8
    IL_0061: conv.i8
    IL_0062: xor
    IL_0063: ldc.i8 0x971a2deac17412a0
    IL_006c: clt.un
    IL_006e: conv.ovf.i8.un
    IL_006f: ldc.i4 0x4e48156f
    IL_0074: not
    IL_0075: neg
    IL_0076: conv.ovf.i8
    IL_0077: ldc.r8 float64(0x6d33412277487d96)
    IL_0080: ldloc 0x0000
    IL_0084: add
    IL_0085: neg
    IL_0086: conv.ovf.i8
    IL_0087: rem.un
    IL_0088: ldc.r8 float64(0x5945c246ce0853b1)
    IL_0091: conv.u8
    IL_0092: conv.ovf.i4
    IL_0093: shr
    IL_0094: pop
    IL_0095: ldarg 0x0000
    IL_0099: conv.u8
    IL_009a: ldloc.s 0x06
    IL_009c: pop
    IL_009d: div
    IL_009e: clt
    IL_00a0: conv.r.un
    IL_00a1: pop
    IL_00a2: ldc.r8 float64(0x8ef6409240a112dc)
    IL_00ab: ckfinite
    IL_00ac: conv.ovf.u1.un
    IL_00ad: ret
  }


  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       36 (0x24)
    .maxstack  6
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
			IL_0001:  nop
			IL_0002:  ldc.i4.s   100
			IL_0004:  conv.i8
			IL_0005:  ldc.i4.2
			IL_0006:  ldc.i4.1
			IL_0007:  conv.i
			IL_0008:  ldc.i4.2
			IL_0009:  ldc.i4.4
			IL_000a:  ldc.i4.s   10
			IL_000c:  conv.i
			IL_000d:  call       int8 DevDiv_590771::ILGEN_METHOD(int64,
																														 char,
																														 native unsigned int,
																														 unsigned int32,
																														 char,
																														 native int)

      IL_0014:  pop
      IL_0015:  nop
      IL_0016:  leave.s    IL_001d

    }  // end .try
    catch [System.Runtime]System.Object 
    {
      IL_0018:  pop
      IL_0019:  nop
      IL_001a:  nop
      IL_001b:  leave.s    IL_001d

    }  // end handler
    IL_001d:  ldc.i4.s   100
    IL_001f:  stloc.0
    IL_0020:  br.s       IL_0022

    IL_0022:  ldloc.0
    IL_0023:  ret
  } // end of method DevDiv_590771::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DevDiv_590771::.ctor

} // end of class DevDiv_590771
