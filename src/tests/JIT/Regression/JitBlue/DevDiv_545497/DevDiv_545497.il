// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The bug that this test captures was a case where arm LSRA built wrong uses order for
// the IL_0090 shift operand.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'DevDiv_545497' {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_545497
       extends [System.Runtime]System.Object
{
  .method private hidebysig static uint8 
          ILGEN_METHOD(float32 a,
                       uint32 b,
                       uint64 c) cil managed
  {
	.maxstack  194
	.locals init (float64, bool, unsigned int64)
	IL_0000: ldloc.s 0x01
	IL_0002: conv.r.un
	IL_0028: conv.ovf.u8.un
	IL_002c: ldloc.s 0x00
	IL_002e: conv.ovf.i8.un
	IL_002f: stloc 0x0002
	IL_0033: ldarg 0x0002
	IL_003f: ldloc 0x0001
	IL_0043: ldloc.s 0x01
	IL_0045: shl
	IL_0047: shr.un
	IL_0049: ldarg.s 0x01
	IL_005b: conv.i1
	IL_005d: conv.ovf.u
	IL_005e: ldloc 0x0001
	IL_0063: ldc.i4 0
	IL_0071: ldloc 0x0002
	IL_007a: conv.ovf.i1.un
	IL_007b: stloc 0x0001
	IL_007f: conv.i1
	IL_0081: rem.un
	IL_0082: ldarg 0x0002
	IL_0087: dup
	IL_0088: add.ovf.un
	IL_008a: ldloc.s 0x01
	IL_008c: ldloc 0x0001
	IL_0090: shr           // This shift moves loc0x01 >> loc0x01 and exposed the original issue.
	IL_0091: conv.ovf.i8
	IL_0092: mul
	IL_0093: ldloc.s 0x01
	IL_0095: ldarg.s 0x01
	IL_0097: div.un
	IL_0098: starg 0x0001
	IL_009c: ldloc 0x0002
	IL_00b0: clt
	IL_00b2: conv.ovf.i1.un
	IL_00b3: sub.ovf.un
	IL_00b7: sub.ovf
	IL_00b9: shr.un
	IL_00ba: ldloc.s 0x01
	IL_00bc: conv.ovf.u8.un
	IL_00bd: and
	IL_00bf: clt.un
	IL_00c1: ret
  } // end of method DevDiv_545497::ILGEN_METHOD

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.r4     0.0
      IL_0007:  ldc.i4.0
      IL_0008:  ldc.i4.0
      IL_0009:  conv.i8
      IL_000a:  call       uint8 DevDiv_545497::ILGEN_METHOD(float32,
                                                             uint32,
                                                             uint64)
      IL_000f:  pop
      IL_0010:  nop
      IL_0011:  leave.s    IL_0018

    }  // end .try
    catch [System.Runtime]System.Object 
    {
      IL_0013:  pop
      IL_0014:  nop
      IL_0015:  nop
      IL_0016:  leave.s    IL_0018

    }  // end handler
    IL_0018:  ldc.i4.s   100
    IL_001a:  stloc.0
    IL_001b:  br.s       IL_001d

    IL_001d:  ldloc.0
    IL_001e:  ret
  } // end of method DevDiv_545497::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DevDiv_545497::.ctor

} // end of class DevDiv_545497
