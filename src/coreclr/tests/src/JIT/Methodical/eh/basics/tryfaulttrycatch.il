// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// try fault, error case


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern common {}
.assembly test {}
.module test.exe

.method public static int32 main() {
    .entrypoint
    .maxstack  2
    .locals init (
	  class [mscorlib]System.IO.StringWriter expectedOut,
	  class [common]TestUtil.TestLog testLog
	)
    
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try - catch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try - fault"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In fault"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In catch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StartRecording()

    .try {
      ldstr      "In try - catch"
      call       void [System.Console]System.Console::WriteLine(string)

      .try
      {
	ldstr      "In try - fault"
        call       void [System.Console]System.Console::WriteLine(string)
        newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
        throw
        leave.s    done

      }  
      fault
      {
        ldstr      "In fault"
        call       void [System.Console]System.Console::WriteLine(string)
        endfault
      }  
    } 
    catch [mscorlib]System.Exception 
    {
      ldstr      "In catch"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s done
    }
  done:
    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()
    
    ret
}
