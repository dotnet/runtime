// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b15244
{
}
.class public auto ansi AA extends [mscorlib]System.Object
{


	.method public specialname rtspecialname instance void .ctor() il managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public auto ansi MainClass extends [mscorlib]System.Object
{
	.method static int32 Main()
	{
		.entrypoint
		.maxstack 4
		.locals (int32)

		ldc.i4.1
		stloc.0
		
	try_enter:
		ldc.i4 2
		newarr class AA
		dup

		ldc.i4 0
		newobj instance void AA::.ctor()
		stelem.ref

		ldc.i4 0
		ldelem.ref
		pop
		
		ldc.i4 -2075281454
		ldc.i4 239230248
		sub.ovf
		pop
		leave	beyond
		
	catch_handler:
		pop
		ldc.i4.0
		stloc.0
		leave beyond
		
	beyond:
		ldloc.0
		ldc.i4 100
		add
		ret
		.try try_enter to catch_handler catch [mscorlib]System.OverflowException handler catch_handler to beyond
	}
}
