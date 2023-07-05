// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly DevDiv_578214 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_578214.Program
       extends [System.Runtime]System.Object
{
.method static int16 ILGEN_METHOD(unsigned int16, float32, native unsigned int, unsigned int16, unsigned int8, int16, unsigned int32)
{
	.maxstack  192
	.locals init (unsigned int64)

	IL_0000: ldc.r8 float64(0x1091e4a00cee077c)
	IL_0009: ckfinite
	IL_000a: conv.i4
	IL_000b: ldarg.s 0x04
	IL_000d: ldloc 0x0000
	IL_0011: conv.i1
	IL_0012: sub.ovf.un
	IL_0013: add.ovf
	IL_0014: ldarg.s 0x01
	IL_0016: starg 0x0001
	IL_001a: nop
	IL_001b: brtrue 
	IL_003c
	IL_0020: ldarg.s 0x01
	IL_0022: ldarg 0x0000
	IL_0026: ldarg 0x0003
	IL_002a: clt.un
	IL_002c: ldarg 0x0004
	IL_0030: xor
	IL_0031: conv.r8
	IL_0032: neg
	IL_0033: starg 0x0001
	IL_0037: conv.ovf.u4.un
	IL_0038: starg 0x0005
	IL_003c: ldc.i8 0x26f19f75aa0243b7
	IL_0045: conv.i8
	IL_0046: conv.r8
	IL_0047: ldarg.s 0x01
	IL_0049: conv.r.un
	IL_004a: pop
	IL_004b: ldarg.s 0x03
	IL_004d: ldarg 0x0006
	IL_0051: ceq
	IL_0053: conv.r.un
	IL_0054: cgt.un
	IL_0056: ldloc 0x0000
	IL_005a: conv.ovf.i8.un
	IL_005b: brtrue 
	IL_006b
	IL_0060: ldarg 0x0001
	IL_0064: pop
	IL_0065: ldarg 0x0001
	IL_0069: ckfinite
	IL_006a: pop
	IL_006b: ldarg.s 0x05
	IL_006d: neg
	IL_006e: conv.ovf.i8.un
	IL_006f: ldloc.s 0x00
	IL_0071: ceq
	IL_0073: dup
	IL_0074: div.un
	IL_0075: ldarg 0x0001
	IL_0079: neg
	IL_007a: ckfinite
	IL_007b: starg.s 0x01
	IL_007d: bgt 
	IL_0086
	IL_0082: ldloc.s 0x00
	IL_0084: conv.ovf.u2.un
	IL_0085: pop
	IL_0086: ldarg.s 0x04
	IL_0088: conv.ovf.u4.un
	IL_0089: conv.ovf.u8.un
	IL_008a: ldarg.s 0x01
	IL_008c: conv.ovf.u1
	IL_008d: shr
	IL_008e: conv.r4
	IL_008f: ldarg 0x0006
	IL_0093: conv.r.un
	IL_0094: conv.r.un
	IL_0095: ckfinite
	IL_0096: ckfinite
	IL_0097: clt
	IL_0099: ldc.i8 0x643c1542f946b6ef
	IL_00a2: conv.r8
	IL_00a3: ldc.r8 float64(0x8bd4c4eb52d70435)
	IL_00ac: conv.r.un
	IL_00ad: conv.ovf.u8
	IL_00ae: not
	IL_00af: conv.r8
	IL_00b0: conv.ovf.u1
	IL_00b1: starg 0x0004
	IL_00b5: ldc.i4 0x7771fb8b
	IL_00ba: conv.r8
	IL_00bb: cgt.un
	IL_00bd: pop
	IL_00be: neg
	IL_00bf: ret   
}

.method public hidebysig static int32 
  Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	// Code size       34 (0x22)
	.maxstack  7
	.locals init (int32 V_0)
	IL_0000:  nop
	.try
	{
	IL_0001:  nop
	IL_0002:  ldc.i4.0
	IL_0003:  ldc.r4     0.0
	IL_0008:  ldc.i4.0
	IL_0009:  ldc.i4.0
	IL_000a:  ldc.i4.0
	IL_000b:  ldc.i4.0
	IL_000c:  ldc.i4.0
	IL_000d:  call       int16 DevDiv_578214.Program::ILGEN_METHOD(uint16,
																 float32,
																 native unsigned int,
																 uint16,
																 uint8,
																 int16,
																 uint32)
	IL_0012:  pop
	IL_0013:  nop
	IL_0014:  leave.s    IL_001b

	}  // end .try
	catch [System.Runtime]System.Object 
	{
	IL_0016:  pop
	IL_0017:  nop
	IL_0018:  nop
	IL_0019:  leave.s    IL_001b

	}  // end handler
	IL_001b:  ldc.i4.s   100
	IL_001d:  stloc.0
	IL_001e:  br.s       IL_0020

	IL_0020:  ldloc.0
	IL_0021:  ret
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

} // end of class DevDiv_578214.Program
