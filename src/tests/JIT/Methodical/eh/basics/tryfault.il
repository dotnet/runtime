// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// try fault, non error case


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME {}

.class public auto ansi Test_tryfault extends [mscorlib] System.Object {
.method public static int32 main() {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (
	  class [mscorlib]System.IO.StringWriter expectedOut,
	  class [eh_common]TestUtil.TestLog testLog
	)
    
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

      .try
      {
	ldstr      "In try"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    done

      }  
      fault
      {
        ldstr      "In fault"
        call       void [System.Console]System.Console::WriteLine(string)
        endfault
      }  
  
      done:

      ldloc.s    testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
      ldloc.s    testLog
      callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
      
	  ret
}
}
