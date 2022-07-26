// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'u_ref'// as "avg"
{
}
.assembly extern xunit.core {}
// MVID: {BCA6096F-DF11-4FA3-BF16-EEDA01729535}
.namespace AvgTest_u_ref
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		// Code size       48 (0x30)
		.maxstack  5
		.locals(native unsigned int)
		ldc.i4.0
		stloc.0
		ldloca 0
		mkrefany [mscorlib]System.UIntPtr
		ldc.i4.m1
		stloc.0
		refanyval  [mscorlib]System.UIntPtr
		ldind.i
		ldc.i4.1
		add
		ldc.i4 0x64
		add
		ret
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

} // end of namespace 'AvgTest_u_ref'

//*********** DISASSEMBLY COMPLETE ***********************
