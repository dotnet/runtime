// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{
  .ver 0:0:0:0
}
.assembly extern common{}
.assembly test
{
  .ver 0:0:0:0
}
.module badcodeinsidefinally.exe


.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{

  .method static object GetObj() cil managed
  {
    .locals init (string str)

  try_l0_begin:
  try_l1_begin:
    ldstr      "pass"
    stloc.0
    leave      done
  try_l1_end:
  
  finally_l1_begin:
  try_l2_begin:
    ldnull
    throw
  try_l2_end:
  catch_l2_begin:
    throw
  catch_l2_end:
    pop                
    endfinally
  finally_l1_end:

  try_l0_end:
  
  catch_l0_begin:
    pop
    leave      catch_l0_end
  catch_l0_end:

  done:
    ldloc.0
    ret
    
    .try try_l2_begin to try_l2_end
      catch [mscorlib]System.Exception handler catch_l2_begin to catch_l2_end
    .try try_l1_begin to try_l1_end 
      finally handler finally_l1_begin to finally_l1_end
    .try try_l0_begin to try_l0_end
      catch [mscorlib]System.Exception handler catch_l0_begin to catch_l0_end
  }


  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .locals init (object V_0,
	  class [mscorlib]System.IO.StringWriter expectedOut,
	  class [common]TestUtil.TestLog testLog
    )
    
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "pass"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StartRecording()

    call       object Test::GetObj()
    call       void [System.Console]System.Console::WriteLine(object)
    
    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StopRecording()
    
    ldloc.s    testLog
    callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()
    
    ret
  }
  
}
