// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// rethrow out of finally
// expect throw System.Security.VerificationException

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly '15266' {}

.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
	
	.try
	{
		call       void test::test2()
		ldstr "Fail 0"
		call       void [System.Console]System.Console::WriteLine(string)

		leave FAIL
	}

	catch [mscorlib]System.InvalidCastException
	{
		pop
		ldstr "Pass"
		call       void [System.Console]System.Console::WriteLine(string)
		leave PASS
	}
	catch [mscorlib]System.DivideByZeroException
	{
		pop
		ldstr "Fail 2"
		call       void [System.Console]System.Console::WriteLine(string)
		leave FAIL
	}

		
FAIL:
    ldc.i4 42
    br END
PASS:
    ldc.i4 100
END:
    ret
  }

.method public hidebysig static void  test2() cil managed
  {
	.try
	{
		newobj     instance void [mscorlib]System.DivideByZeroException::.ctor()
		throw
	}
	finally
	{
		newobj     instance void [mscorlib]System.InvalidCastException::.ctor()
		throw
	}
	ret   
  }

}
