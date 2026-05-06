// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b70354' {}
.class public auto ansi Test_b70354 extends [mscorlib] System.Object {
.method public static int32 f1() cil managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack 1
	br start_try
	OUT:
	    ldc.i4 100
		ret
	start_handler:
		leave.s OUT
	end_handler:  //

	start_try:
		leave.s OUT
	end_try: //

	.try start_try to end_try
	catch [mscorlib]System.Exception handler start_handler to end_handler
}
}
