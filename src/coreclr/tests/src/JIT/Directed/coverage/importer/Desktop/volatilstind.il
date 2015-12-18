.assembly extern mscorlib {}
.assembly volatilstind {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.method public static void f()
{
.locals init (int32* V_0)
ldloc.0
ldc.i4 1
volatile. stind.i4
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call void f()
leave FAIL
}
catch [mscorlib]System.NullReferenceException
{
leave PASS
}
FAIL:
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}