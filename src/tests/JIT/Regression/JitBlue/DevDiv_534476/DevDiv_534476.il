// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console {auto}
.assembly DevDiv_534476 {}
.assembly extern xunit.core {}

// The test showed a problem with stack level calculations during morph phase
// that did not reflect code movements in the later phases.

// In this case a helper call, created for IL_002c, will create a put_arg, but it will be moved from IL_0026 to IL_0014.
// This  put_arg movement will affect throw edge at IL_0021, that will have incorrect stack level.

.class public auto ansi beforefieldinit DevDiv_534476.ILGEN_CLASS
       extends [System.Runtime]System.Object
{
  .method private static float32 
          ILGEN_METHOD(uint64 e) cil managed
  {
	.maxstack  46
	.locals init (native unsigned int, bool)
	IL_0000: ldarg 0x0000
			 ldc.i4.1
	IL_0012: conv.ovf.u2.un
	IL_0014: shr.un
	IL_001b: ldarg 0x0000
	IL_0021: conv.ovf.u4.un // the first throw block edge.
	IL_0023: ldloc.s 0x01
	IL_0025: mul // mul value is known to be null.
	IL_0026: shl // long decomposition will replace IL_0026 with IL_0014.
	IL_002c: conv.r4 // will be transformed into helper call, with put_arg before IL_0021.
   	IL_0009: ckfinite // to create the second throw edge.

	IL_002d: ret
  } // end of method ILGEN_CLASS::ILGEN_METHOD

.method public hidebysig static int32  Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       30 (0x1e)
  .maxstack  6
  .locals init (int32 V_0)
  IL_0000:  nop
  IL_000d:  ldc.i4.1
  IL_000e:  conv.i8
  IL_0011:  call       float32 DevDiv_534476.ILGEN_CLASS::ILGEN_METHOD(uint64)
  IL_0016:  pop
  IL_0017:  ldc.i4.s   100
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_001c
  IL_001c:  ldloc.0
  IL_001d:  ret
} // end of method ILGEN_CLASS::Main


} // end of class DevDiv_534476.ILGEN_CLASS
