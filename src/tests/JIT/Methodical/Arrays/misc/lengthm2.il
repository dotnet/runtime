// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'lengthm2'
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {23E3D49B-6CFD-4DA0-8C9D-A611CE0BA103}
.namespace JitTest_lengthm2_misc_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		// Code size       38 (0x26)
		.maxstack  4
		.locals (int32[0...], int32)
		ldc.i4.2
		stloc.1
		.try {
			ldc.i4	-2
			newobj		instance void int32[0...]::.ctor(int32)
			stloc.0
			ldc.i4	100
			stloc.1
			ldstr	"TEST FAILED (1)"
			call 	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
			leave next1
		} catch [mscorlib]System.OverflowException {
			pop
			ldloc.1
			ldc.i4.1
			sub
			stloc.1
			leave next1
		}
	next1:
		.try {
			ldc.i4	-2
			newobj		instance void int32[0...]::.ctor(int32)
			stloc.0
			ldc.i4	100
			stloc.1
			ldstr	"TEST FAILED (2)"
			call 	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
			leave next2
		} catch [mscorlib]System.OverflowException {
			pop
			ldloc.1
			ldc.i4.1
			sub
			stloc.1
			leave next2
		}
	next2:
		ldloc.1
		dup
		brtrue out_of_here
		ldstr	"TEST PASSED"
		call 	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	out_of_here:
		ldc.i4    0x64
		add
		ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_lengthm2_misc_il'

//*********** DISASSEMBLY COMPLETE ***********************
