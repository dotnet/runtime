// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test showed an error in `Compiler::optVNConstantPropOnJTrue`, that 
// tried to remove the same tree twice. `IL_0083: brtrue` has known value
// and its side effects is extracted in a separate statement that is placed 
// before the jump, but if this statement has unconditional throw call, then
// it tries to remove the jump during its optimization. 

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Extensions
{
}
.assembly GitHub_18291
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit GitHub_18291
       extends [System.Runtime]System.Object
{
  .method static uint16 ILGEN_METHOD(native unsigned int, int8, native int)
  {
	.maxstack  339
	.locals init (char, float64)
	IL_0000: ldc.i8 0xfd32d272ad594ff4
	IL_0009: ldc.i8 0x64c364b9c2b819ca
	IL_0012: rem
	IL_0013: pop
	IL_0014: ldc.i8 0x1917394023ae4269
	IL_001d: ldc.i8 0x81bdec3ee0298c86
	IL_0026: ldc.i8 0xa89686c29dae8e55
	IL_002f: add.ovf.un
	IL_0030: sub
	IL_0031: conv.r8
	IL_0032: ldc.r8 float64(0xfb02a0dfac22a4d8)
	IL_003b: pop
	IL_003c: neg
	IL_003d: ckfinite
	IL_003e: neg
	IL_003f: ldc.i8 0x8e6b6f2945ce019e
	IL_0048: conv.r8
	IL_0049: pop
	IL_004a: ldloc.s 0x01
	IL_004c: ldloc 0x0001
	IL_0050: mul
	IL_0051: clt.un
	IL_0053: ldc.i8 0xfc97bfa772027108
	IL_005c: ldc.i8 0x233cd8b922d142f1
	IL_0065: clt.un
	IL_0067: ldloc 0x0001
	IL_006b: ldc.r8 float64(0x895c435fe82a3459)
	IL_0074: cgt.un
	IL_0076: ldloc 0x0001
	IL_007a: ldloc.s 0x01
	IL_007c: clt.un
	IL_007e: cgt
	IL_0080: shr.un
	IL_0081: mul
	IL_0082: not
	IL_0083: brtrue 
	IL_00f7
	IL_0088: ldc.i8 0xb04a6130e573d9b7
	IL_0091: ldc.i8 0x30abe634c9c86493
	IL_009a: div.un
	IL_009b: ldc.i8 0x3e2e26d76c796837
	IL_00a4: ldc.i8 0x80df80b810563352
	IL_00ad: cgt
	IL_00af: ldarg 0x0000
	IL_00b3: ldloc.s 0x01
	IL_00b5: ldloc 0x0001
	IL_00b9: clt.un
	IL_00bb: xor
	IL_00bc: shr
	IL_00bd: conv.i2
	IL_00be: shr
	IL_00bf: not
	IL_00c0: ldc.i8 0xc4b53aec3c888995
	IL_00c9: ldc.i8 0xa56d8ec44e7a1509
	IL_00d2: ceq
	IL_00d4: neg
	IL_00d5: ldc.i8 0x327f25ebf903a46f
	IL_00de: conv.ovf.u4
	IL_00df: neg
	IL_00e0: ldarg.s 0x01
	IL_00e2: not
	IL_00e3: ldarg.s 0x00
	IL_00e5: shr.un
	IL_00e6: sub.ovf.un
	IL_00e7: mul.ovf.un
	IL_00e8: conv.ovf.i8.un
	IL_00e9: add.ovf.un
	IL_00ea: nop
	IL_00eb: ldc.i8 0x76e238de4bd30317
	IL_00f4: div
	IL_00f5: conv.r.un
	IL_00f6: pop
	IL_00f7: ldarg.s 0x00
	IL_00f9: conv.ovf.u1.un
	IL_00fa: ldarg.s 0x02
	IL_00fc: ldc.r8 float64(0x2987021098838673)
	IL_0105: conv.ovf.u1.un
	IL_0106: shl
	IL_0107: not
	IL_0108: ceq
	IL_010a: ldc.i4 0x280ab454
	IL_010f: conv.ovf.i2.un
	IL_0110: ldc.i8 0x92a914fa3ddce305
	IL_0119: ldc.i8 0xc0bc06b564a01c98
	IL_0122: and
	IL_0123: not
	IL_0124: ldloc.s 0x01
	IL_0126: ckfinite
	IL_0127: conv.ovf.u8
	IL_0128: cgt
	IL_012a: shl
	IL_012b: conv.r.un
	IL_012c: conv.r8
	IL_012d: ckfinite
	IL_012e: conv.r.un
	IL_012f: ldc.r8 float64(0x0274f44dfcbb9658)
	IL_0138: ldloc 0x0001
	IL_013c: rem
	IL_013d: neg
	IL_013e: clt
	IL_0140: ldarg 0x0001
	IL_0144: neg
	IL_0145: ldc.i8 0x576c1f39a5a88241
	IL_014e: conv.u1
	IL_014f: and
	IL_0150: pop
	IL_0151: shr.un
	IL_0152: ret   
  }


  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4.0
      IL_0003:  ldc.i4.2
      IL_0004:  ldc.i4.3
      IL_0005:  call       uint16 GitHub_18291::ILGEN_METHOD(native unsigned int,
                                                                   int8,
                                                                   native int)
      IL_000a:  pop
      IL_000b:  nop
      IL_000c:  leave.s    IL_0015

    }  // end .try
    catch [System.Runtime]System.Object 
    {
      IL_000e:  pop
      IL_000f:  nop
      IL_0010:  ldc.i4.s   100
      IL_0012:  stloc.0
      IL_0013:  leave.s    IL_0019

    }  // end handler
    IL_0015:  ldc.i4.m1
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class GitHub_18291
