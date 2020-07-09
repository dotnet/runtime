// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly EH1 {}
.method public static int32 f1() cil managed
{
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
