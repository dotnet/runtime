// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test {}
.class public test
{
.method public static void f1() cil managed
{
.maxstack 20
br start_try1
start_finally1: 
finally1_out: endfinally
end_finally1: //

start_try1: 
try1_out:
leave OUT0
end_try1: //

OUT: ret
OUT0:
br start_try2
start_finally2: 
finally2_out: endfinally
end_finally2: //


start_try2: 
try2_out:
newobj instance void [mscorlib]System.Exception::.ctor()
throw
end_try2: //

OUT1:
leave OUT
.try start_try2 to end_try2 finally handler start_finally2 to end_finally2
.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.try
	{
	call void test::f1()
	leave RET
	}
	catch [mscorlib]System.Exception
	{
	leave RET
	}
	RET: ldc.i4 100
	ldstr "PASSED"
	call void [System.Console]System.Console::WriteLine(string)
	ret
	}
}
