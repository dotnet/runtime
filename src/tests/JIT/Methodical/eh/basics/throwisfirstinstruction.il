// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// throw first instruction in catch block - NDPWhidbey 10777


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly 'throwisfirstinstruction' {}
.assembly extern xunit.core {}

.namespace Test_throwisfirstinstruction_il
{
.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .field private static class [eh_common]TestUtil.TestLog testLog
  .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
  {
    .maxstack  2
    .locals init (
      class [mscorlib]System.IO.StringWriter expectedOut
    )
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "in try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "after try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stsfld     class [eh_common]TestUtil.TestLog Test_throwisfirstinstruction_il.test::testLog
    ret
  } 

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

      ldsfld     class [eh_common]TestUtil.TestLog Test_throwisfirstinstruction_il.test::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

      .try
      {
        ldstr      "in try"
        call       void [System.Console]System.Console::WriteLine(string)
        leave IL_end
      }

      catch [mscorlib]System.Exception
      {
        throw
      }
	

    IL_end:  
      ldstr      "after try"
      call       void [System.Console]System.Console::WriteLine(string)

      ldsfld     class [eh_common]TestUtil.TestLog Test_throwisfirstinstruction_il.test::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
      ldsfld     class [eh_common]TestUtil.TestLog Test_throwisfirstinstruction_il.test::testLog
      callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
      
      ret
  }

}
}
