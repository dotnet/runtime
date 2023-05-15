// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// a try catch where there is a leave statement whose destination is inside the catch handler



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








.namespace hello_leaveinsameregion_leaves_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static void 
            inTry() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "in Try\n"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_000f:  throw
    } 

    .method public hidebysig static void 
            inCatch() cil managed
    {
      .maxstack  1
      IL_0000:  ldstr      "in Catch\n"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static void 
            inFinally() cil managed
    {
      .maxstack  0
      IL_0000:  ret
    } 

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack 2
      .locals init (class [mscorlib]System.Exception V_0,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [eh_common]TestUtil.TestLog testLog
					)
	newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "in Try\n"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Catch\n"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      
      .try
      {
        IL_0000:  call       void hello_leaveinsameregion_leaves_il.Class1::inTry()
        IL_0005:  leave.s    IL_0015

      }  
      catch [mscorlib]System.Exception 
      {
        IL_0007:  stloc.0
        IL_0008:  ldloc.0
                  leave.s IL_0009
                
        IL_0009:  ldloc.0
        IL_000e:  call       void hello_leaveinsameregion_leaves_il.Class1::inCatch()
        IL_0013:  leave.s    IL_0015

      }  
      IL_0015:  
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
