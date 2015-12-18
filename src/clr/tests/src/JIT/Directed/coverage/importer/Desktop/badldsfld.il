
.assembly extern mscorlib {}
.assembly badldslfd {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.class Test extends [mscorlib]System.Object
{
.field int32 i
.method static int32 f()
{
ldsfld int32 Test::i
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call int32 Test::f()
leave.s FAIL
}
catch [mscorlib]System.Security.VerificationException
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