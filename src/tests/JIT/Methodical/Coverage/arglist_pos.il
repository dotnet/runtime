// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

//  arglist should verify within vararg method
.assembly arglist_pos
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.class public auto ansi arglist_pos extends [mscorlib]System.Object
{
  .method public hidebysig static vararg void Test() il managed
  {
    .maxstack  8
    .locals init( [0] value class [mscorlib]System.ArgIterator args )
    ldloca.s   args
    arglist
    call       instance void [mscorlib]System.ArgIterator::.ctor(value class [mscorlib]System.RuntimeArgumentHandle)
    ldloca.s   args
	call       instance int32 [mscorlib]System.ArgIterator::GetRemainingCount()
    call       void [System.Console]System.Console::WriteLine(int32)
    ret
  }

  .method public hidebysig static int32 Main() il managed
  {
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type, string[]) = {
        type([TestLibrary]TestLibrary.PlatformDetection)
        string[1] ('IsVarArgSupported')
    }
	.entrypoint
	.try
	{
		call       vararg void arglist_pos::Test()
		leave.s    PASS
	}
	catch [mscorlib]System.MissingMethodException
	{
		ldstr      "OK. Got MissingMethodException\n"
		call       void [System.Console]System.Console::WriteLine(class System.String)
		leave.s    PASS

	}
	catch [mscorlib]System.Exception 
	{
		callvirt   instance class System.String [mscorlib]System.Exception::ToString()
		ldstr      "WRONG. CHECK TESTING CONDITIONS. Exception:\n"
		call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
		call       void [System.Console]System.Console::WriteLine(class System.String)
		leave.s    FAIL

	}

PASS:
	ldc.i4 100
	ret

FAIL:
	ldc.i4 0
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() il managed
  {
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
  }

}
