.assembly extern mscorlib {}
.assembly badendfinally { }
.method public static int32 f() noinlining
{
endfinally
ldc.i4 100
ret
}
.class public auto ansi badendfinally {
.method public static int32 Main()
{
.entrypoint
.try
{
	call int32 f()
	leave.s FAIL
}
catch [mscorlib]System.InvalidProgramException
{
	leave.s PASS
}
PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4 1
	ret
}
}