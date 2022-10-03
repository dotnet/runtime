// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// try catch, with leave statements from the catch back into trybody (allowed by spec)



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly extern eh_common{}
.assembly 'leaveintotrybody'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
.namespace hello_leaveintotrybody_leaves_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static void 
            inTry() cil managed
    {
      .maxstack  8
      ldstr      "throwing ..."
      call       void [System.Console]System.Console::WriteLine(string)
      newobj     instance void [mscorlib]System.Exception::.ctor()
      throw
    } 

    .method public hidebysig static void 
            inCatch() cil managed
    {
      .maxstack  8
      IL_0000:  ldstr      "in Catch\n"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig static void 
            inFinally() cil managed
    {
      .maxstack  8
      IL_0000:  ret
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
					class [mscorlib]System.Exception V_1,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [eh_common]TestUtil.TestLog testLog
					)

    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "in Try ..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "throwing ..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Catch\n"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Try ... (again)"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "throwing ..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Catch\n"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Try ... (again)"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "throwing ..."
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Catch\n"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "in Try ... (again), leaving"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      .try
      {
        ldstr      "in Try ..."
        call       void [System.Console]System.Console::WriteLine(string)
        call       void hello_leaveintotrybody_leaves_il.Class1::inTry()

      cont1:
        ldstr      "in Try ... (again)"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void hello_leaveintotrybody_leaves_il.Class1::inTry()

      cont2:
        ldstr      "in Try ... (again)"
        call       void [System.Console]System.Console::WriteLine(string)
        call       void hello_leaveintotrybody_leaves_il.Class1::inTry()

      cont3:
        ldstr      "in Try ... (again), leaving"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    IL_001b

      }  
      catch [mscorlib]System.Exception 
      {
        pop
        call       void hello_leaveintotrybody_leaves_il.Class1::inCatch()
        ldloc.0
        brfalse.s zero
        ldc.i4.1
        ldloc.0
        beq.s first
        leave.s cont3

      zero:
        ldloc.0
        ldc.i4.1
        add
        stloc.0
        leave.s  cont1

      first:
        ldloc.0
        ldc.i4.1
        add
        stloc.0
        leave.s  cont2

      }  
      IL_001b:  
        ldloc.s    testLog
		callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
		ldloc.s    testLog
		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
		ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
