// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'native' { }
.namespace JitTest
{
  .class private auto ansi Test extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() il managed
    {
		.entrypoint
		.maxstack  8
		.locals (unsigned int64 LOC)
		
		ldc.i4.0
		conv.i8
		stloc LOC
		
		ldloca LOC
		mkrefany unsigned int64
		refanyval unsigned int64
		mkrefany unsigned int64
		pop
		
		ldc.i4 100
		ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
    {
		.maxstack  8
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
