// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// branch backwards from try body (a try / finally struct) 



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly extern eh_common{}
.assembly ASSEMBLY_NAME
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001









.namespace hello_branchbackwardswithfinally_leaves_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static void 
            inTry() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "In try"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static void 
            inTry2() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "In try2"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static void 
            inFinally2() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "In finally 2"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static void 
            inFinally() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "In finally"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals init (int32 V_0,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [eh_common]TestUtil.TestLog testLog
					)
	newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "before try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally 2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "before try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "done"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      ldc.i4.0
      stloc.0

    before:
      ldstr      "before try"
      call       void [System.Console]System.Console::WriteLine(string)
      ldloc.0
      ldc.i4.1
      beq done

      .try
      {
        call       void hello_branchbackwardswithfinally_leaves_il.Class1::inTry()
        .try
        {
          call       void hello_branchbackwardswithfinally_leaves_il.Class1::inTry2()
          ldc.i4.1
          stloc.0
          leave.s    before

        }  
        finally
        {
          call       void hello_branchbackwardswithfinally_leaves_il.Class1::inFinally()
          endfinally
        }  

      }  
      finally
      {
        call       void hello_branchbackwardswithfinally_leaves_il.Class1::inFinally2()
        endfinally
      }  
    done:
		ldstr      "done"
		call       void [System.Console]System.Console::WriteLine(string)
        ldloc.s    testLog
		callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
		ldloc.s    testLog
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
