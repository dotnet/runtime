// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly ASSEMBLY_NAME// as "avg"
{
}
.assembly extern xunit.core {}
.namespace BoxTest_u_box_il
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method private hidebysig static void Test(class [mscorlib]System.Object, native unsigned int&) il managed
    {
		.maxstack  5
    	ldarg.1
    	ldind.i
    	ldarga 0
    	ldind.ref
    	unbox [mscorlib]System.UIntPtr
    	ldind.i
    	beq go
    	newobj instance void [mscorlib]System.Exception::.ctor()
    	throw
    go:
    	ldarg.1
    	dup
    	ldind.i
    	ldc.i4 0xabcdef
    	sub.ovf.un
    	stind.i
    	ret
    }
    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  5
		.locals (native unsigned int)
		ldc.i4 0x00abcdef
		stloc.0
		ldloc 0
		box [mscorlib]System.UIntPtr
		dup
		ldloca 0
		call void BoxTest_u_box_il.Test::Test(class [mscorlib]System.Object, native unsigned int&)
		ldloc.0
		brtrue box_pop_fail
		dup
		dup
		unbox [mscorlib]System.UIntPtr
		call void BoxTest_u_box_il.Test::Test(class [mscorlib]System.Object, native unsigned int&)
		unbox [mscorlib]System.UIntPtr
		ldind.i
		dup
		brtrue int_pop_fail
		ldstr "passed"
	print_and_exit:
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 0x64
		add
		ret
	box_pop_fail:
		pop
		br next
	int_pop_fail:
		pop
	next:
		ldc.i4.1
		ldstr "failed"
		br print_and_exit
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'AvgTest'

