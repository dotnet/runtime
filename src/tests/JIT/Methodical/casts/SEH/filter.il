// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'filter_SEH_casts' { }
.method public static int32 main() cil managed
{
	.maxstack  8
	.entrypoint

Begin_1:
	newobj		instance void [mscorlib]System.DivideByZeroException::.ctor()
	throw
	leave.s		End_1
Filter_1:
	castclass	[mscorlib]System.DivideByZeroException
	call		void [System.Console]System.Console::WriteLine(object)
	ldc.i4.1
	endfilter
Handler_1:
	pop
	leave.s		End_1
End_1:
.try Begin_1 to Filter_1 filter Filter_1 handler Handler_1 to End_1

Begin_2:
	newobj		instance void [mscorlib]System.DivideByZeroException::.ctor()
	throw
	leave.s		End_2
Filter_2:
	isinst		[mscorlib]System.DivideByZeroException
	call		void [System.Console]System.Console::WriteLine(object)
	ldc.i4.1
	endfilter
Handler_2:
	pop
	leave.s		End_2
End_2:
.try Begin_2 to Filter_2 filter Filter_2 handler Handler_2 to End_2

	ldc.i4.s	100
	ret
}
