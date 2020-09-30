// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib
{ }
.assembly extern common {}
.assembly 'test'
{ }
.class private auto ansi Test extends [mscorlib]System.Object
{
.method private hidebysig static int32 Main() il managed
{
	.entrypoint
	.maxstack  8
    .locals init (
	  class [common]TestUtil.TestLog testLog
	)
    newobj     instance void [common]TestUtil.TestLog::.ctor()
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StartRecording()
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
    callvirt   instance void [common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()

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
