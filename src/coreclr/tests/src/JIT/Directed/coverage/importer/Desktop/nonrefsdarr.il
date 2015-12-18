

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly legacy library arrlen {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.class private auto ansi beforefieldinit nonrefsdarr
       extends [mscorlib]System.Object
{
  .method public static void f() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.1
    IL_0003:  ldlen
    IL_0004:  conv.i4
    IL_0005:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0010:  ret
  }
  .method public static int32 Main() cil managed
  {
    .entrypoint
    .maxstack 5
    .try
    {
	call void nonrefsdarr::f()
        leave.s FAIL
    }
    catch [mscorlib]System.Security.VerificationException
    {
        pop
        leave.s PASS
    }
    FAIL:
	ldstr "Should have caught System.Security.VerificationException"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "FAILED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
	ret
    PASS:
	ldstr "Caught System.Security.VerificationException as expected"
	call void [System.Console]System.Console::WriteLine(string)
	ldstr "PASSED"
	call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
	ret
  }
}