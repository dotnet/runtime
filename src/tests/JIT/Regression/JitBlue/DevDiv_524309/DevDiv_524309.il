// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly DevDiv_524309 {}
.assembly extern xunit.core {}

// The test showed problem with x86 long decomposition of unused values,  
// DecomposeShift did not set unused on the removed shift's children. 

.class public auto ansi beforefieldinit DevDiv_524309.ILGEN_CLASS
       extends [System.Runtime]System.Object
{
  .method private static bool  ILGEN_METHOD(uint64 a, int16 b) cil managed
  {
	.maxstack  88
	.locals init (char, native unsigned int, float64, int16, native unsigned int, unsigned int8)
	IL0000: ldarg.s 0x00
	IL0002: brtrue 
	IL000f
	IL0007: ldarg 0x0000
	IL000b: starg 0x0000
	IL000f: ldarg 0x0000
	IL0013: ldarg 0x0000
	IL0017: conv.ovf.i8.un
	IL0018: conv.ovf.u8
	IL0019: ldc.i8 0x2b09aaa61c8c032b // will be converted to comma(throw, 0).
	IL0022: conv.ovf.i1 // this cast must throw.
	IL0023: ldarg.s 0x00
	IL0025: conv.i4
	IL0026: shr.un // <- the result is unused, because it is proven to throw before.
	IL0027: shr.un
	IL0028: nop
	IL0029: bgt 
	IL0040
	IL002e: ldarg.s 0x00
	IL0030: ldarg 0x0000
	IL0034: xor
	IL0035: ldloc 0x0000
	IL0039: shl
	IL003a: ldloc.s 0x02
	IL003c: conv.ovf.i8.un
	IL003d: rem.un
	IL003e: pop
	IL003f: nop
	IL0040: ldarg.s 0x00
	IL0042: conv.u4
	IL0043: ldarg 0x0001
	IL0047: ldloc.s 0x02
	IL0049: ldloc 0x0002
	IL004d: clt
	IL004f: ldarg 0x0001
	IL0053: clt.un
	IL0055: shr
	IL0056: rem.un
	IL0057: ret
  } // end of method ILGEN_CLASS::ILGEN_METHOD

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       39 (0x27)
    .maxstack  2
    .locals init (class [System.Runtime]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4.s   100
      IL_0004:  conv.i8
      IL_0005:  ldc.i4.s   10
      IL_0007:  call       bool DevDiv_524309.ILGEN_CLASS::ILGEN_METHOD(uint64,
                                                                        int16)
      IL_000c:  pop
      IL_000d:  nop
      IL_000e:  leave.s    IL_0020

    }  // end .try
    catch [System.Runtime]System.Exception 
    {
      IL_0010:  stloc.0
      IL_0011:  nop
      IL_0012:  nop
      IL_0013:  leave.s    IL_0020

    }  // end handler
    IL_0020:  ldc.i4.s   100
    IL_0022:  stloc.1
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.1
    IL_0026:  ret
  } // end of method ILGEN_CLASS::Main

} // end of class DevDiv_524309.ILGEN_CLASS
