// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly 'rethrowinfinallyinsidecatch' {}
.assembly extern xunit.core {}


.namespace Test_rethrowinfinallyinsidecatch_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
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
      ldstr      "\ttry - throwing outer exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "\tcatch - Outer Exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "\t\ttry - throwing inner exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "\t\tcatch - Inner Exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "\t\tfinally - Rethrowing Outer Exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "catch - Outer Exception"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

      ldloc.s    expectedOut
      newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
      stsfld     class [eh_common]TestUtil.TestLog Test_rethrowinfinallyinsidecatch_il.Class1::testLog
      ret
    } 

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals init (class [mscorlib]System.Exception eo,
               class [mscorlib]System.Exception ei,
               class [mscorlib]System.Exception e,
               int32 V_3)

      ldsfld     class [eh_common]TestUtil.TestLog Test_rethrowinfinallyinsidecatch_il.Class1::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

      .try
      {
        ldstr      "try"
        call       void [System.Console]System.Console::WriteLine(string)
        .try
        {
          ldstr      "\ttry - throwing outer exception"
          call       void [System.Console]System.Console::WriteLine(string)
          ldstr      "Outer Exception"
          newobj     instance void [mscorlib]System.Exception::.ctor(string)
          throw

        }  
        catch [mscorlib]System.Exception 
        {
          stloc.0
          ldstr      "\tcatch - "
          ldloc.0
          callvirt   instance string [mscorlib]System.Exception::get_Message()
          call       string [mscorlib]System.String::Concat(string,
                                                            string)
          call       void [System.Console]System.Console::WriteLine(string)
          .try
          {
            .try
            {
              ldstr      "\t\ttry - throwing inner exception"
              call       void [System.Console]System.Console::WriteLine(string)
              ldstr      "Inner Exception"
              newobj     instance void [mscorlib]System.Exception::.ctor(string)
              throw

            }  
            catch [mscorlib]System.Exception 
            {
              stloc.1
              ldstr      "\t\tcatch - "
              ldloc.1
              callvirt   instance string [mscorlib]System.Exception::get_Message()
              call       string [mscorlib]System.String::Concat(string,
                                                                string)
              call       void [System.Console]System.Console::WriteLine(string)
              leave.s    L1

            }  
            L1:  
              leave.s    L2

          }  
          finally
          {
            ldstr      "\t\tfinally - Rethrowing Outer Exception"
            call       void [System.Console]System.Console::WriteLine(string)
            rethrow
          }  
          L2:  
            br.s       L2

        }  
      }  
      catch [mscorlib]System.Exception 
      {
        stloc.2
        ldstr      "catch - "
        ldloc.2
        callvirt   instance string [mscorlib]System.Exception::get_Message()
        call       string [mscorlib]System.String::Concat(string,
                                                          string)
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    L3

      }  
      L3:  
      ldsfld     class [eh_common]TestUtil.TestLog Test_rethrowinfinallyinsidecatch_il.Class1::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
      
      ldsfld     class [eh_common]TestUtil.TestLog Test_rethrowinfinallyinsidecatch_il.Class1::testLog
      callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

      ret
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
