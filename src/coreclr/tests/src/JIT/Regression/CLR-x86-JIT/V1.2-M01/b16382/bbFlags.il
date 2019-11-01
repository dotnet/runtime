// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
.assembly extern mscorlib {}
.assembly extern System {}
.assembly bbFlags {}
.class public bbFlags
{
.method public static int32 f() cil managed
{
.maxstack 5
.locals init (int32 V_0)

br start_try1

	start_finally1: 
		endfinally
	end_finally1: 


start_try1: 
	leave OUT0
end_try1: 

OUT: 
	ldc.i4 50
	stloc.0
	ldc.i4 12
	ret
	
OUT0:
	br start_try2
	start_finally2: 
		endfinally
	end_finally2: 

	start_try2: 
		newobj instance void [mscorlib]System.Exception::.ctor()
		throw
	end_try2: 
OUT1:
leave OUT
.try start_try2 to end_try2 finally handler start_finally2 to end_finally2
.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
}

.method public static int32 Main()
{
.maxstack 5
.entrypoint
.try
{
call int32 bbFlags::f()
leave OUT
}
catch [mscorlib]System.Exception
{
leave OUT
}
OUT:
ldc.i4 100
ret
}


}
