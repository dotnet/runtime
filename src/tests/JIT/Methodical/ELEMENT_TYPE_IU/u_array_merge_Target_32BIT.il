// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly extern mscorlib
{ }
.assembly ASSEMBLY_NAME
{ }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.class public auto ansi Test_u_array_merge_Target_32BIT extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() il managed
{
        // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.Is32BitProcess))]
        .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                      string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                    6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                    2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                    65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                    43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                    20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                    6E 75 6C 6C 01 00 00 00 0E 49 73 33 32 42 69 74   // null.....Is32Bit
                                                                                                                    50 72 6F 63 65 73 73 00 00 )                      // Process..
	.entrypoint
	.maxstack  8
	.locals (unsigned int32[0...], native unsigned int[0...], native unsigned int, native unsigned int)
	ldc.i4 16
	newobj instance void native unsigned int[0...]::.ctor(int32)
	stloc.0
	ldc.i4 16
	newobj instance void unsigned int32[0...]::.ctor(int32)
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
