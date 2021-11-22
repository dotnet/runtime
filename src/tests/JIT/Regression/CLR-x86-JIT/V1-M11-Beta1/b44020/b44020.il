// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'c'// as "c"
{ }
.assembly extern xunit.core {}
.module 'c.exe'
.namespace JitTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  30
		.locals (int32 V_0)
		ldc.i4.0
		stloc.0
	again:
		ldc.i4.1
		ldloc.0
		brtrue label_2
	
	again1:
		pop
		ldc.i4.1
		ldloc.0
		brtrue again1

		pop

		ldc.i4 100
		ret
	
	label_2:
		pop
		br again
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'
  } // end of class 'Test'
} // end of namespace 'JitTest'
