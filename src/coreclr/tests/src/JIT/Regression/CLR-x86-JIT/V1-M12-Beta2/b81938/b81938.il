// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly a { }
.method public static int32 main(string[] args) il managed
{
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
