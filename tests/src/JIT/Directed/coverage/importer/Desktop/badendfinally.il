.assembly extern mscorlib {}
.assembly badendfinally {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.method public static int32 f() noinlining
{
endfinally
ldc.i4 100
ret
}
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