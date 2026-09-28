// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly 'filter' { }
.namespace JitTest
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() cil managed aggressiveinlining
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
          string('https://github.com/dotnet/runtimelab/issues/155: Non-exception throws')
          type([TestLibrary]TestLibrary.Utilities)
          string[1] ('IsNativeAot')
      }
      .entrypoint
      // Code size       120 (0x78)
      .maxstack  2

        TRY_1:
			ldstr      "inside 1st try"
			call       void [System.Console]System.Console::WriteLine(string)
			ldc.r4 1.0
			box float32
			throw
			leave.s END_1

		FILTER_1:
  			ldstr      "entering filter"
  			call       void [System.Console]System.Console::WriteLine(string)
			isinst [mscorlib]System.Single
			br MAKE_JIT_WORK

		MAKE_JIT_WORK:
			unbox float32
			ldind.r4
			ldc.r4 1.0
			ceq
			brtrue  OK_1

			ldstr      "bad value in filter!"
			call       void [System.Console]System.Console::WriteLine(string)
			ldc.i4.0
			br.s MERGE_1

		OK_1:
			ldstr      "leaving filter"
			call       void [System.Console]System.Console::WriteLine(string)
			ldc.i4.1

		MERGE_1:
			endfilter

		HANDLER_1:
  			ldstr      "inside _except"
  			call       void [System.Console]System.Console::WriteLine(string)
			pop
			leave.s END_1

        END_1:
			ldc.i4 100
      		ret
        .try TRY_1 to FILTER_1 filter FILTER_1 handler HANDLER_1 to END_1
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest
