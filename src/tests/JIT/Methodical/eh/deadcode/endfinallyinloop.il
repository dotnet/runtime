// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly extern eh_common{}

.assembly 'endfinallyinloop'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001


.namespace Test_endfinallyinloop_il
{
.class public auto ansi beforefieldinit foo
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
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
    ldstr      "In try..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "Done!"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
    .try
    {
      IL_0000:  ldstr      "In try..."
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  leave.s    IL_0030

    }  
    finally
    {
      IL_000c:  ldstr      "In finally..."
      IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0016:  endfinally

      IL_0017:  ldstr      "unreached!"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(string)

      IL_0021:  br         IL_000c

    }  

    IL_0026:  ldstr      "unreached 2!"
    IL_002b:  call       void [System.Console]System.Console::WriteLine(string)

    IL_0030:  ldstr      "Done!"
    IL_0035:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0039:  ldloc.s    testLog
			  callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
			  ldloc.s    testLog
			  callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    IL_003a:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
}
