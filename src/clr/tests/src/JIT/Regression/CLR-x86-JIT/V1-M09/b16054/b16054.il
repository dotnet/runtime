// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly b16054
{
}
.class _div {
	.method static	void _div(float64, float64) {
	.maxstack		3
call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
		ldarg	0
		conv.r8
		ldarg	1
		conv.r8
		div
		callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(float64)
		ret
	}

	.method static int32 main(class [mscorlib]System.String[]) {
	.entrypoint
	.maxstack		20
		ldc.r8		float64(0xFFF0000000000000)
		ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
		call	void _div::_div(float64, float64)
		ldc.i4 100
		ret
	}
}
