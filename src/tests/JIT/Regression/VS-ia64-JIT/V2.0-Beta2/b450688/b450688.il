// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

// Metadata version: v2.0.50131
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'b450688'
{
}

.corflags 0x00000001    //  ILONLY

.method static object Test() cil managed
{
	.locals init (string retVal,
		class [mscorlib]System.Exception nestedException)

try_begin:
	ldstr "pass"
	stloc.0
	leave try_end
try_end:
	ldloc.0
	ret
catch_begin:
	stloc.1
	ldloc.1
	callvirt instance string [mscorlib]System.Object::ToString()
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "fail"
	stloc.0
	ldloc.0
	ldloc.1
	newobj instance void [mscorlib]System.Exception::.ctor(string, class [mscorlib]System.Exception)
	throw
catch_end:
	ldloc.1
	ret	
	.try try_begin to try_end catch [mscorlib]System.Exception handler catch_begin to catch_end
}

.class public auto ansi Test_b450688 extends [mscorlib] System.Object {
.method public static int32 Main() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint

	call object Test()
	call void [System.Console]System.Console::WriteLine(object)

	ldstr "Test SUCCESS"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100
	ret
}
}
