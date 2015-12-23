
.assembly extern legacy library mscorlib {}
.assembly refanytype1 {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.method public static int32 f()
{
ldc.i4 100
ret
}
.method public static int32 Main()
{
.entrypoint
call int32 f()
mkrefany int32
refanytype
pop
ldc.i4 100
ret
}