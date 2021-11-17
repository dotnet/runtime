// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{ }
.assembly 'test'
{ }
.assembly extern xunit.core {}
.class private auto ansi Test_i_array_merge_i386 extends [mscorlib]System.Object
{
.method private hidebysig static int32 Main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  8
	.locals (int32[0...], native int[0...], native int, native int)
	ldc.i4 16
	newobj instance void native int[0...]::.ctor(int32)
	stloc.0
	ldc.i4 16
	newobj instance void int32[0...]::.ctor(int32)
	stloc.1

	ldc.i4 0x12345678
	stloc.2

	ldc.i4.1
	stloc.3
loop_begin:
	//
	//	This switch determines type of array (int32 or native int)
	ldloc.3
	switch (err1,a1,a1,a1,a1,a2,a2,a2,a2)
err1:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
a1:
	ldloc.0
	ldstr "branch1.1"
	br.s end_a
a2:
	ldloc.1
	ldstr "branch1.2"
end_a:
	call void [System.Console]System.Console::WriteLine(string)
	dup
	//	This switch determines opcode stelem.i or stelem.i4
	ldloc.3
	dup
	dup
	switch (err2,b1,b2,b1,b2,b1,b2,b1,b2)
err2:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
b1:
	stelem.i
	ldstr "branch2.1"
	br end_b
b2:
	stelem.i4
	ldstr "branch2.2"
end_b:
	call void [System.Console]System.Console::WriteLine(string)

	dup
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.Object)
	ldloc.3
	
	dup
	switch (err3,c1,c1,c2,c2,c1,c1,c2,c2)
err3:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
err4:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
c1:
	ldelem.i
	ldstr "branch3.1"
	br end_c
c2:
	ldelem.i4
	ldstr "branch3.2"
end_c:
	call void [System.Console]System.Console::WriteLine(string)
	ldloc.3
	ceq
	brfalse err4

	ldloc.3
	ldc.i4.1
	add
	dup
	stloc.3
	ldc.i4 9
	ceq
	brfalse loop_begin
	
	ldc.i4 0x64
	ret
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
