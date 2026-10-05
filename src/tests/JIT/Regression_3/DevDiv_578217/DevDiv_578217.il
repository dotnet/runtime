// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test originally triggered incorrect assert that fgGetPtrArgCntMax calculated during morph was lower
// than maxStackLevel calculated during StackLevelSetter. It happens because long decomposition 
// is able to move PUTARG_STK.

// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'DevDiv_578217' {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_578217.Program
       extends [System.Runtime]System.Object
{
	.method static int16 ILGEN_METHOD(unsigned int16, float32, native unsigned int, unsigned int16, unsigned int8, int16, unsigned int32)
	{
		.maxstack  142
		.locals init (unsigned int64)
		IL_0000: ldc.i8 0x20fa631bf0010bd2
		IL_0009: ldarg.s 0x00
		IL_000b: conv.u8
		IL_000c: ldarg 0x0001
		IL_0010: pop
		IL_0011: ldc.i8 0x30cdeb7bedc062f5
		IL_001a: conv.r4
		IL_001b: conv.ovf.i1.un
		IL_001c: shr
		IL_001d: not
		IL_001e: ldc.i8 0x0881617fcdbacd3b
		IL_0027: ldloc.s 0x00
		IL_0029: conv.ovf.u8
		IL_002a: add.ovf
		IL_002b: ldloc.s 0x00
		IL_002d: conv.i4
		IL_002e: ldloc 0x0000
		IL_0032: conv.ovf.u8
		IL_0033: ldarg 0x0001
		IL_0037: conv.ovf.i8.un
		IL_0038: cgt
		IL_003a: mul
		IL_003b: shr
		IL_003c: mul.ovf.un
		IL_003d: ldc.i8 0xfda22ae3eca9bad9
		IL_0046: ldloc.s 0x00
		IL_0048: cgt.un
		IL_004a: starg 0x0005
		IL_004e: bgt 
		IL_005d
		IL_0053: ldc.i8 0x601e3816def227ec
		IL_005c: pop
		IL_005d: ldarg.s 0x03
		IL_005f: ldarg.s 0x02
		IL_0061: not
		IL_0062: conv.u1
		IL_0063: shr.un
		IL_0064: ldc.i8 0x8e78692b130a7a32
		IL_006d: conv.r8
		IL_006e: conv.r8
		IL_006f: neg
		IL_0070: conv.ovf.i4
		IL_0071: not
		IL_0072: ldarg.s 0x00
		IL_0074: not
		IL_0075: shr
		IL_0076: shr
		IL_0077: ldarg.s 0x05
		IL_0079: shr
		IL_007a: ldloc.s 0x00
		IL_007c: ldloc 0x0000
		IL_0080: or
		IL_0081: neg
		IL_0082: pop
		IL_0083: ldc.i4 0xf7cd9099
		IL_0088: conv.ovf.u8
		IL_0089: conv.u8
		IL_008a: conv.ovf.u2.un
		IL_008b: starg.s 0x02
		IL_008d: ret   
	} // end of method Program::ILGEN_METHOD

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
		IL_000d:  call       int16 DevDiv_578217.Program::ILGEN_METHOD(uint16,
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

} // end of class DevDiv_578217.Program

