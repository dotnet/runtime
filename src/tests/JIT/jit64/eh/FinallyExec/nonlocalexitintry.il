// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly 'nonlocalexitintry' {}


.class public auto ansi nonlocalexitintry {
.method public static int32 Main()
{
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
  ldstr      "in try"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "  in try 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "  in finally 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "  in try 2"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "  in finally 2"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "in catch"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
  stloc.s    testLog

  ldloc.s    testLog
  callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

  call       void Run()
  
  ldloc.s    testLog
  callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
  ldloc.s    testLog
  callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
  
  ret
}
}


.method public static void  Run()
{
  .maxstack  1
  .try
  {
    ldstr      "in try"
    call       void [System.Console]System.Console::WriteLine(string)
      
    .try
    {
      TRY_1:
      ldstr      "  in try 1"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s    TRY_2
    }
    finally
    {
      ldstr      "  in finally 1"
      call       void [System.Console]System.Console::WriteLine(string)
      endfinally
    }

    ldstr      "*** UNREACHABLE ***"
    call       void [System.Console]System.Console::WriteLine(string)

    .try
    {
      TRY_2:
      ldstr      "  in try 2"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s    TRY_END
    }
    finally
    {
      ldstr      "  in finally 2"
      call       void [System.Console]System.Console::WriteLine(string)
      endfinally
    }
    
    TRY_END:
    newobj     instance void [mscorlib]System.Exception::.ctor()
    throw
    leave.s    DONE

  }
  catch [mscorlib]System.Exception 
  {
    pop
    ldstr      "in catch"
    call       void [System.Console]System.Console::WriteLine(string)
    leave.s    DONE
  }

  DONE:  ret
}
