// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// try block with both filter/handler and typed handler
// two filter and two catches, the 2nd filter handles the exception
// if there are more than one handlers that is suitable, make sure that the
// handler that appears first is the one executed



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
.namespace hello_filterfiltercatchcatch_mixedhandler_il
{
  .class public auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static void 
            inTry() cil managed
    {
      .maxstack  8
      IL_0000:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0005:  throw
    } 

    .method public hidebysig static void 
            inCatch() cil managed
    {
      .maxstack  8
      IL_0000:  ret
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
      .locals init (class [mscorlib]System.DivideByZeroException V_0,
					class [mscorlib]System.ArithmeticException V_1,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [eh_common]TestUtil.TestLog testLog
					)
	newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In filter1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In filter2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In handler2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
      start_try:
        ldstr      "In try"
        call       void [System.Console]System.Console::WriteLine(string)
        newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
        throw
      end_try:

      filter_begin:  
        pop
        ldstr      "In filter1"
        call       void [System.Console]System.Console::WriteLine(string)

        ldc.i4.0
        endfilter

      handler_begin:  
        pop
        ldstr "In handler"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    done
      handler_end:

      filter2_begin:  
        pop
        ldstr      "In filter2"
        call       void [System.Console]System.Console::WriteLine(string)

        ldc.i4.1 
        endfilter

      handler2_begin:  
        pop
        ldstr "In handler2"
        call       void [System.Console]System.Console::WriteLine(string)
        leave.s    done
      handler2_end:
      div_by_zero:
        stloc.0
        ldstr      "Caught"
        call       void [System.Console]System.Console::WriteLine(string)
        ldloc.0
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s    done
      end_div_by_zero:

      catch_arith:
        stloc.1
        ldstr      "Caught"
        call       void [System.Console]System.Console::WriteLine(string)
        ldloc.1
        call       void [System.Console]System.Console::WriteLine(object)
        leave.s    done
      end_catch_arith:


      .try start_try to end_try filter filter_begin handler handler_begin to handler_end
      .try start_try to end_try filter filter2_begin handler handler2_begin to handler2_end
      .try start_try to end_try 
      catch [mscorlib]System.DivideByZeroException handler div_by_zero to end_div_by_zero
      .try start_try to end_try 
      catch [mscorlib]System.ArithmeticException handler catch_arith to end_catch_arith

      done:  
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
