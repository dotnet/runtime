// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly dev10_531793
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi N`1<- X>
{
} // end of class N`1

.class private auto ansi T1
       extends [mscorlib]System.Object
       implements class N`1<class N`1<class T1>>
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method T1::.ctor

} // end of class T1

.class public auto ansi sealed Module1
       extends [mscorlib]System.Object
{
  .method public static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init ([0] class T1 t, [1] class N`1<class T1> v, int32)

	nop
    newobj     instance void T1::.ctor()
    stloc.0
	.try
	{
	    ldloc.0
	    castclass  class N`1<class T1>
	    stloc.1
		ldstr      "FAIL! System.InvalidCastException was not thrown."
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s 1
		stloc.2
		leave.s ENDOFTEST
	}
	catch [mscorlib]System.InvalidCastException
	{
		pop
		ldstr      "PASS! System.InvalidCastException caught."
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s 100
		stloc.2
		leave.s ENDOFTEST
	}

ENDOFTEST:
	ldloc.2
	ret		

  } // end of method Module1::Main

} // end of class Module1

// =============================================================

