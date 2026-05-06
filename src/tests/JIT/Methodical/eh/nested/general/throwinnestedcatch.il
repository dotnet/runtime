// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Throwing an exception from an inner catch guarded with multiple catch blocks.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}


.namespace Test_throwinnestedcatch_il
{
.class public auto ansi beforefieldinit test extends [mscorlib]System.Object
{
  .field private static class [eh_common]TestUtil.TestLog testLog
  .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
  {
    .maxstack  2
    .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
      
    ldloc.s    expectedOut
    ldstr      "try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "\ttry"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "\tcatch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "catch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stsfld     class [eh_common]TestUtil.TestLog Test_throwinnestedcatch_il.test::testLog
    ret
  } 

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    ldsfld     class [eh_common]TestUtil.TestLog Test_throwinnestedcatch_il.test::testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      
    .try
    {
      ldstr "try"
      call       void [System.Console]System.Console::WriteLine(string)
      .try
      {
        ldstr "\ttry"
        call       void [System.Console]System.Console::WriteLine(string)
        newobj     instance void [mscorlib]System.Exception::.ctor()
        throw
      }
      catch [mscorlib]System.Exception
      {
        pop
        ldstr "\tcatch"
        call       void [System.Console]System.Console::WriteLine(string)
        newobj     instance void [mscorlib]System.DivideByZeroException::.ctor()
        throw
        leave IL_INNER
      }
      IL_INNER: leave IL_END
    }
    catch [mscorlib]System.DivideByZeroException
    {
      pop
      ldstr "catch"
      call       void [System.Console]System.Console::WriteLine(string)
      leave IL_END
    }
    catch [mscorlib]System.Exception
    {
      pop
      ldstr "Fail"
      call       void [System.Console]System.Console::WriteLine(string)
      leave IL_END
    }
    IL_END:
    ldsfld     class [eh_common]TestUtil.TestLog Test_throwinnestedcatch_il.test::testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
      
    ldsfld     class [eh_common]TestUtil.TestLog Test_throwinnestedcatch_il.test::testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

    ret
  }  
}
}
