// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b15617' {}
.class public EHTest
{
	.field private static int64 fld1
	.field private static int32 fld2

	.method public static int32 f1() cil managed
	{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack 50

	start_try: 
		leave OUT
	end_try: //

	start_catch: 
		ldc.i4 19
		ldc.i4 14
		shr
		neg
		ldc.i4 22
		shl
		not
		newarr     [mscorlib]System.Int64
		ldc.i4 93
		stsfld int32 EHTest::fld2
		ldc.i4 53
		ldelem.i8
		stsfld int64 EHTest::fld1
		leave OUT
	end_catch: //

	OUT:
		ldc.i4 100
	ret
	.try start_try to end_try catch [mscorlib]System.Exception handler start_catch to end_catch
	}
}
