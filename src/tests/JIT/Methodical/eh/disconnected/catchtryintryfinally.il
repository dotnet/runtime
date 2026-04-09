// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
.class public auto ansi Test_catchtryintryfinally extends [mscorlib] System.Object {
.method public static int32  main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  400
  .locals init (int32 V_0,
           int32 V_1,
			class [mscorlib]System.IO.StringWriter expectedOut,
			class [eh_common]TestUtil.TestLog testLog
	)
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try 1, throwing exception"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In handler 1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()    
  IL_0000:  ldc.i4     0xffff63c0
  IL_0005:  stloc.0

  .try
  {
    ldstr      "In try"
    call       void [System.Console]System.Console::WriteLine(string)
    br         IL_0017

  IL_000b:  
    ldstr      "In handler 1"
    call       void [System.Console]System.Console::WriteLine(string)
    leave.s    IL_0031

  IL_0017:  
    ldstr      "In try 1, throwing exception"
    call       void [System.Console]System.Console::WriteLine(string)
    newobj     instance void [mscorlib]System.Exception::.ctor()
    throw

  }  
  finally
  {
    IL_0027:  ldstr      "In finally"
    .try IL_0017 to IL_0027 catch [mscorlib]System.Object handler IL_000b to IL_0017
    IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
    endfinally
  }  
  IL_0031:  
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    ret
} 
}
