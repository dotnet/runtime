// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_605447 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_605447
       extends [System.Runtime]System.Object
{
	.method static unsigned int16 ILGEN_METHOD(unsigned int32, unsigned int32, int32, char, float32)
	{
		.maxstack  177
		.locals init (unsigned int8, bool, int8, unsigned int32, int8, unsigned int64, int8)

		IL_0000: ldloc 0x0001
		IL_0004: stloc 0x0004
		IL_0008: ldc.r8 float64(0xfa1caf5352b592b5)
		IL_0011: conv.ovf.i4
		IL_0012: ldarg 0x0003
		IL_0016: conv.ovf.u8
		IL_0017: ldarg 0x0001
		IL_001b: conv.ovf.i8.un
		IL_001c: conv.u8
		IL_001d: neg
		IL_001e: neg
		IL_001f: not
		IL_0020: ldloc 0x0000
		IL_0024: conv.r4
		IL_0025: conv.u8
		IL_0026: neg
		IL_0027: cgt.un
		IL_0029: conv.r8
		IL_002a: pop
		IL_002b: conv.ovf.i8.un
		IL_002c: ldloc 0x0002
		IL_0030: conv.ovf.u1
		IL_0031: conv.ovf.u8.un
		IL_0032: conv.u8
		IL_0033: ldloc.s 0x05
		IL_0035: sub.ovf.un
		IL_0036: conv.ovf.u8
		IL_0037: ldloc.s 0x05
		IL_0039: ldc.i8 0x3ef77ff626f9ded8
		IL_0042: or
		IL_0043: xor
		IL_0044: or
		IL_0045: not
		IL_0046: ldarg 0x0004
		IL_004a: ldarg 0x0004
		IL_004e: neg
		IL_004f: conv.r.un
		IL_0050: mul
		IL_0051: neg
		IL_0052: ckfinite
		IL_0053: pop
		IL_0054: conv.r8
		IL_0055: neg
		IL_0056: ldarg 0x0004
		IL_005a: ldloc.s 0x05
		IL_005c: ldarg.s 0x04
		IL_005e: conv.ovf.u8
		IL_005f: and
		IL_0060: pop
		IL_0061: add
		IL_0062: ldloc 0x0004
		IL_0066: conv.ovf.i8
		IL_0067: ldloc.s 0x00
		IL_0069: conv.ovf.i8
		IL_006a: dup
		IL_006b: conv.u2
		IL_006c: shr
		IL_006d: div
		IL_006e: conv.r4
		IL_006f: ldloc.s 0x05
		IL_0071: ldloc.s 0x05
		IL_0073: div.un
		IL_0074: conv.r4
		IL_0075: neg
		IL_0076: add
		IL_0077: neg
		IL_0078: ldarg.s 0x04
		IL_007a: ldarg 0x0004
		IL_007e: clt
		IL_0080: ldloc 0x0000
		IL_0084: sub.ovf
		IL_0085: conv.r4
		IL_0086: conv.r.un
		IL_0087: ckfinite
		IL_0088: conv.r4
		IL_0089: add
		IL_008a: dup
		IL_008b: ckfinite
		IL_008c: cgt
		IL_008e: ldc.r8 float64(0x9c1be4140fda2146)
		IL_0097: ckfinite
		IL_0098: conv.ovf.u8.un
		IL_0099: ldarg 0x0004
		IL_009d: conv.i2
		IL_009e: shr.un
		IL_009f: conv.ovf.i8
		IL_00a0: conv.ovf.i.un
		IL_00a1: ldloc.s 0x04
		IL_00a3: neg
		IL_00a4: not
		IL_00a5: ldloc.s 0x00
		IL_00a7: xor
		IL_00a8: shr
		IL_00a9: neg
		IL_00aa: rem
		IL_00ab: nop
		IL_00ac: conv.r8
		IL_00ad: mul
		IL_00ae: conv.ovf.i4.un
		IL_00af: shr.un
		IL_00b0: ret   
	}

	.method public hidebysig static int32  Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		// Code size       33 (0x21)
		.maxstack  5
		.locals init (int32 V_0)
		IL_0000:  nop
		.try
		{
			IL_0001:  nop
			IL_0002:  ldc.i4.1
			IL_0003:  ldc.i4.2
			IL_0004:  ldc.i4.3
			IL_0005:  ldc.i4.s   52
			IL_0007:  ldc.r4     6.
			IL_000c:  call       uint16 DevDiv_605447::ILGEN_METHOD(uint32,
																														uint32,
																														int32,
																														char,
																														float32)
			IL_0011:  pop
			IL_0012:  nop
			IL_0013:  leave.s    IL_001a
		}  // end .try
		catch [System.Runtime]System.Object 
		{
			IL_0015:  pop
			IL_0016:  nop
			IL_0017:  nop
			IL_0018:  leave.s    IL_001a
		}  // end handler
		IL_001a:  ldc.i4.s   100
		IL_001c:  stloc.0
		IL_001d:  br.s       IL_001f
		IL_001f:  ldloc.0
		IL_0020:  ret
	} // end of method DevDiv_605447::Main


  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DevDiv_605447::.ctor

} // end of class DevDiv_605447
