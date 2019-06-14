
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'dim-reabstraction-generics'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'dim-reabstraction-generics.exe' // GUID = {198880C7-E25B-497F-AE09-6C555BBBAB42}


  .class interface public auto ansi abstract I1`1<T>
  {

    // method line 1
    .method public virtual hidebysig newslot abstract 
           instance default int32 M1 ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method I1`1::M1

    // method line 2
    .method public virtual hidebysig newslot 
           instance default int32 M2 ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 1
	.locals init (
		int32	V_0)
	IL_0000:  nop 
	IL_0001:  ldc.i4.0 
	IL_0002:  stloc.0 
	IL_0003:  br.s IL_0005

	IL_0005:  ldloc.0 
	IL_0006:  ret 
    } // end of method I1`1::M2

  } // end of class I1`1

  .class interface public auto ansi abstract I2`1<T>
  	implements class I1`1<!0>  {

    // method line 3
    .method private final virtual hidebysig abstract 
           instance default int32 'I1<T>.M1' ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method I2`1::I1<T>.M1

    // method line 4
    .method public virtual hidebysig newslot 
           instance default int32 M2 ()  cil managed 
    {
        // Method begins at RVA 0x2064
	// Code size 11 (0xb)
	.maxstack 1
	.locals init (
		int32	V_0)
	IL_0000:  nop 
	IL_0001:  ldc.i4 200
	IL_0006:  stloc.0 
	IL_0007:  br.s IL_0009

	IL_0009:  ldloc.0 
	IL_000a:  ret 
    } // end of method I2`1::M2

  } // end of class I2`1

  .class private auto ansi beforefieldinit Test1
  	extends [mscorlib]System.Object
  	implements class I2`1<int32>, class I1`1<int32>  {

    // method line 5
    .method private static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x207c
	.entrypoint
	// Code size 54 (0x36)
	.maxstack 2
	.locals init (
		class I1`1<int32>	V_0,
		bool	V_1,
		int32	V_2,
		bool	V_3)
	IL_0000:  nop 
	IL_0001:  newobj instance void class Test1::'.ctor'()
	IL_0006:  stloc.0 
	IL_0007:  ldloc.0 
	IL_0008:  callvirt instance int32 class I1`1<int32>::M1()
	IL_000d:  ldc.i4.s 0x64
	IL_000f:  ceq 
	IL_0011:  ldc.i4.0 
	IL_0012:  ceq 
	IL_0014:  stloc.1 
	IL_0015:  ldloc.1 
	IL_0016:  brfalse.s IL_001d

	IL_0018:  nop 
	IL_0019:  ldc.i4.m1 
	IL_001a:  stloc.2 
	IL_001b:  br.s IL_0034

	IL_001d:  ldloc.0 
	IL_001e:  callvirt instance int32 class I1`1<int32>::M2()
	IL_0023:  ldc.i4.0 
	IL_0024:  cgt.un 
	IL_0026:  stloc.3 
	IL_0027:  ldloc.3 
	IL_0028:  brfalse.s IL_0030

	IL_002a:  nop 
	IL_002b:  ldc.i4.s 0xfffffffe
	IL_002d:  stloc.2 
	IL_002e:  br.s IL_0034

	IL_0030:  ldc.i4.0 
	IL_0031:  stloc.2 
	IL_0032:  br.s IL_0034

	IL_0034:  ldloc.2 
	IL_0035:  ret 
    } // end of method Test1::Main

    // method line 6
    .method private final virtual hidebysig newslot 
           instance default int32 'I1<System.Int32>.M1' ()  cil managed 
    {
        // Method begins at RVA 0x20c0
	.override method instance int32 class I1`1<int32>::M1()
	// Code size 8 (0x8)
	.maxstack 1
	.locals init (
		int32	V_0)
	IL_0000:  nop 
	IL_0001:  ldc.i4.s 0x64
	IL_0003:  stloc.0 
	IL_0004:  br.s IL_0006

	IL_0006:  ldloc.0 
	IL_0007:  ret 
    } // end of method Test1::I1<System.Int32>.M1

    // method line 7
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20d4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method Test1::.ctor

  } // end of class Test1

