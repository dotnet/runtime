// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
//
.assembly extern mscorlib
{ }
.assembly 'test'
{ }
.class private auto ansi Test extends [mscorlib]System.Object
{
.method private hidebysig static int32 Main() il managed
{
	.entrypoint
	.maxstack  4
	.locals (unsigned int32, native unsigned int, native unsigned int)
	ldc.i4.0
	dup
	stloc.0
	stloc.1

	ldc.i4 0x12345678
	stloc.2

	ldc.i4.0
	br.s loop_begin
loop_begin:
	dup
	brfalse branch1
	ldloca 0
	ldstr "branch1"
	call void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
	br.s merge1
branch1:
	ldstr "branch2"
	call void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
	ldloca 1
merge1:
	ldloca 2
	ldind.i
	stind.i4
	ldc.i4.1
	add.ovf.un
	dup
	ldc.i4.2
	ceq
	brfalse.s loop_begin
	pop
	
	ldloc.0
	ldloc.1
	add.ovf.un
	ldloca 2
	ldind.i
	sub.ovf.un
	ldloc.2
	sub.ovf.un
	ldc.i4 0x64
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
