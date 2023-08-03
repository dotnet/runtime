// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.namespace hello_deadcodeincatch_deadcode_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .field private static class [eh_common]TestUtil.TestLog testLog
    .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
        {
            .maxstack  3
            .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
            newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
            stloc.s    expectedOut
            ldloc.s    expectedOut                  
            ldstr      "In try"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "In catch"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "hello_deadcodeincatch_deadcode_il"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "In finally"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "Pass"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
            stsfld     class [eh_common]TestUtil.TestLog hello_deadcodeincatch_deadcode_il.Class1::testLog
            ret
    } // end of method Class1::.cctor

    .method public hidebysig static void 
            inTry() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      "In try"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Class1::inTry

    .method public hidebysig static void 
            inCatch() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      IL_0000:  ldstr      "In catch"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Class1::inCatch

    .method public hidebysig static void 
            inFinally() cil managed
    {
      // Code size       1 (0x1)
      .maxstack  0
      IL_0000:  ret
    } // end of method Class1::inFinally

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      ldstr   "hello_deadcodeincatch_deadcode_il"
      call    int32 hello_deadcodeincatch_deadcode_il.Class1::Run(string)
      ret
    }
    
    .method public hidebysig static int32 
            Run(string arg) cil managed
    {
      // Code size       74 (0x4a)
      .maxstack  2
      .locals init (string V_0,
               class [mscorlib]System.Exception V_1,
               string V_2,
               int32 V_3)
             
    starttest:
      // Start recording
      ldsfld     class [eh_common]TestUtil.TestLog hello_deadcodeincatch_deadcode_il.Class1::testLog
      callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      ldarg.0
      stloc.0
      .try
      {
        call       void hello_deadcodeincatch_deadcode_il.Class1::inTry()
        newobj     instance void [mscorlib]System.Exception::.ctor()
        throw
        leave.s    done

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        stloc.1
        .try
        {

          ldstr      "Hello"
          ldloc.0
          call       string [mscorlib]System.String::Concat(string,
                                                                    string)
          stloc.2
          call       void hello_deadcodeincatch_deadcode_il.Class1::inCatch()
          ldloc.0
          call       void [System.Console]System.Console::WriteLine(string)

          br.s donetry

          ldloc.0
          callvirt   instance int32 [mscorlib]System.String::get_Length()
          ldc.i4.2	
          blt.s  donetry

          ldloc.2
          call       void [System.Console]System.Console::WriteLine(string)
          
        donetry:
          leave.s    donecatch
        } finally {
           ldstr      "In finally"
           call       void [System.Console]System.Console::WriteLine(string)
           endfinally
        }
      donecatch:
        leave.s doneok
      }  // end handler
      doneok:  
        ldstr      "Pass"
        call       void [System.Console]System.Console::WriteLine(string)
        
        // stop recoding
        ldsfld     class [eh_common]TestUtil.TestLog hello_deadcodeincatch_deadcode_il.Class1::testLog
        callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
      
        // verify output
        ldsfld     class [eh_common]TestUtil.TestLog hello_deadcodeincatch_deadcode_il.Class1::testLog
        callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
        stloc.3
       done:
        ldloc.3
        ret
    } // end of method Class1::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Class1::.ctor

  } // end of class Class1
} // end of namespace hello_deadcodeincatch_deadcode_il
