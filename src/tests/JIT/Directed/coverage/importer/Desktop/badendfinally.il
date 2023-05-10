.assembly extern mscorlib {}
.assembly extern xunit.core {}
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
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
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