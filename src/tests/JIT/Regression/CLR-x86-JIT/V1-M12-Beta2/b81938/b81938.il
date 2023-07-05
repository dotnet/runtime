// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b81938' { }
.class public auto ansi Test_b81938 extends [mscorlib] System.Object {
.method public static int32 main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  8
	br.s IL_TRY
IL_FILTER:
	pop
	ldc.i4.1
	endfilter
IL_HANDLER:
	leave.s IL_END
IL_END:
	ldc.i4.s 100
	ret
IL_TRY:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
IL_END_TRY:
	.try IL_TRY to IL_END_TRY filter IL_FILTER handler IL_HANDLER to IL_END
}
}
