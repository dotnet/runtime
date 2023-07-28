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
.assembly extern eh_common{}
.assembly ASSEMBLY_NAME
{
  .ver 0:0:0:0
}
.assembly extern xunit.core {}


.class public auto ansi beforefieldinit Test_badcodeafterfilter
       extends [mscorlib]System.Object
{

  .method static object GetObj() cil managed
  {
    .locals init (string str)

  try_begin:
    ldstr      "pass"
    stloc.0
    leave      try_end
  try_end:
  
    ldloc.0
    ret
    
  filter_begin:
    throw
    ldc.i4.1
    endfilter
    
  handler_begin:
    leave      try_end
  handler_end:

    ldnull             
    ret
      
    .try try_begin to try_end 
      filter filter_begin handler handler_begin to handler_end
  }


  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (object V_0,
	  class [mscorlib]System.IO.StringWriter expectedOut,
	  class [eh_common]TestUtil.TestLog testLog
    )
    
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "pass"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

    call       object Test_badcodeafterfilter::GetObj()
    call       void [System.Console]System.Console::WriteLine(object)
    
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
    
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    
    ret
  }
  
}
