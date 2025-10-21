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
.assembly extern TestLibrary {}

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
    // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                  string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                6E 75 6C 6C 01 00 00 00 11 49 73 56 61 72 41 72   // null.....IsVarAr
                                                                                                                67 53 75 70 70 6F 72 74 65 64 00 00 )             // gSupported..
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
