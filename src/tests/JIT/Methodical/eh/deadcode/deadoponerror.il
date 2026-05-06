// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib
{ }
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME
{ }
.assembly extern xunit.core {}
.class public auto ansi Test_deadoponerror extends [mscorlib]System.Object
{
.method public hidebysig static int32 Main() il managed
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  8
    .locals init (
	  class [eh_common]TestUtil.TestLog testLog
	)
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor()
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
	.try {
		ldc.i4 1
		leave endtry2
	} catch [mscorlib]System.Exception {
		pop
		leave endtry2
	}

endtry1:

	.try {
		ldc.i4 2
		leave endtry2
	} catch [mscorlib]System.Exception {
		pop
		leave endtry2
	}

endtry2:
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

	ret
} // end of method 'Test::Main'

.method public hidebysig specialname rtspecialname 
        instance void .ctor() il managed
{
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
} // end of method 'Test::.ctor'

} // end of class 'Test'
