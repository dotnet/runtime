// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b29583' { }
.assembly extern xunit.core {}

.namespace BoxTest
{
  .class auto ansi App extends [mscorlib]System.Object
  {
    .method private static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  3
		.locals (class [mscorlib]System.Object obj, float64 F)
		
		ldc.r8  0.1
		stloc  F
		
		ldloc  F
		box  [mscorlib]System.Double
		stloc	obj
		
		ldloca	obj
		mkrefany	[mscorlib]System.Object
		dup
		refanytype
		pop
		refanyval	[mscorlib]System.Object
		ldind.ref
		unbox 		[mscorlib]System.Double
		
		ldind.r8
		ldc.r8  0.1
		beq     ok
		
		ldc.i4.1
		ldstr "Failed"
		br done
ok:	
		ldc.i4 100
		ldstr "Passed"
done:
		call   void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ret
    }
  }
}
