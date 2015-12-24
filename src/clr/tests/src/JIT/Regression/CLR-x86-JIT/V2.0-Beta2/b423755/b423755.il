
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}

.assembly 'test' {
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 )  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
}

.method public static int32 Main()
{
    .entrypoint
    ldc.i4 0
    box int8
    unbox.any int8
    pop

    ldstr "Test SUCCESS"
    call void [System.Console]System.Console::WriteLine(string)

    ldc.i4 100 
    ret
}
