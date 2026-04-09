// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "qsort"
{ }
.assembly extern xunit.core {}
.namespace Test_u_vfld
{
  .class public value sealed auto ansi AA extends [mscorlib]System.ValueType
  {
	.field public native unsigned int m_i
	.field public static native unsigned int s_i
	.field public native unsigned int[0...] m_ai

    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  8
		.locals (value class Test_u_vfld.AA pinned, native unsigned int[0...], native unsigned int, int32, native unsigned int[0...] pinned)
		newobj instance void Test_u_vfld.AA::.ctor()
		stloc.0

		ldloc.0
		ldfld native unsigned int[0...] Test_u_vfld.AA::m_ai
		stloc 4

		ldc.i4 18
		stloc.2

		ldc.i4 19
		newobj instance void native unsigned int[0...]::.ctor(int32)
		dup
		dup
		stloc.1
		dup

		ldc.i4 16
		ldsflda native unsigned int Test_u_vfld.AA::s_i
		stelem.i
		ldc.i4 17
		ldloca 0
		ldflda native unsigned int Test_u_vfld.AA::m_i
		stelem.i
		ldc.i4 18
		ldloca 2
		stelem.i

		ldc.i4.0
		stloc.3
	begin_init_loop:
		ldloc.1
		ldloc.3
		ldloca 0
		ldfld native unsigned int[0...] Test_u_vfld.AA::m_ai
		ldloc.3
		ldelema [mscorlib]System.IntPtr
		stelem.i
		
		ldloc.3
		ldc.i4.1
		add
		dup
		stloc.3
		ldc.i4 16
		ceq
		brfalse begin_init_loop
		
		ldc.i4.0
		dup
		stloc.3
	begin_sum_loop:
		ldloc.1
		ldloc.3
		ldelem.i
		ldind.i
		add
		
		ldloc.3
		ldc.i4.1
		add
		dup
		stloc.3
		ldc.i4 19
		ceq
		brfalse begin_sum_loop

		dup
		call void [System.Console]System.Console::WriteLine(int32)
		ldc.i4 248
		beq stop
		
		ldc.i4.1
		ldstr "failed"
		br exit
	stop:
		ldc.i4 0x64
		ldstr "passed"
	exit:
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ret
    } // end of method 'AA::Main'

    .method public static hidebysig specialname rtspecialname void .cctor() il managed
    {
	      .maxstack  8
	      ldc.i4 100
	      stsfld native unsigned int Test_u_vfld.AA::s_i
	      ret
    } // end of method 'AA::.ctor'
    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
	{
		.maxstack  8
		.locals (int32)
		//ldarg.0
		//call       instance void [mscorlib]System.Object::.ctor()
		ldarg.0
		ldc.i4 10
		stfld native unsigned int Test_u_vfld.AA::m_i
		ldarg.0
		ldc.i4 16
		newobj instance void native unsigned int[0...]::.ctor(int32)
		stfld native unsigned int[0...] Test_u_vfld.AA::m_ai

		ldc.i4.0
		br begin_loop
	begin_loop:
		dup
		stloc.0
		ldarg.0
		ldfld native unsigned int[0...] Test_u_vfld.AA::m_ai
		ldloc.0
		dup
		stelem.i
		ldc.i4.1
		add
		dup
		ldc.i4 16
		ceq
		brfalse begin_loop
	end_loop:
		pop
		ret
	} // end of method 'AA::.ctor'

  } // end of class 'AA'

} // end of namespace 'Test_u_vfld'

//*********** DISASSEMBLY COMPLETE ***********************
