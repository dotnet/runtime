// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// try/finally in catch/try

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
.assembly 'tryfinallyincatchtry'
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.class public auto ansi Test_tryfinallyincatchtry extends [mscorlib] System.Object {
.method public static int32 main() {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  400
    .locals init (	int32 V_0,
					int32 V_1,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [eh_common]TestUtil.TestLog testLog
					)

    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In outer try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try 1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try 2, throwing exception"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally 2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In handler 1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In outer finally"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

     ldc.i4     0xffff63c0
     stloc.0

    .try {
      ldstr "In outer try"
      call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      br.s start_try1

      start_handler0:
	ldstr "In handler 1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
      end_handler0: 

      start_try1:
	ldstr "In try 1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        .try {
          ldstr "In try 2, throwing exception"
	  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
          newobj     instance void [mscorlib]System.Exception::.ctor()
          throw
        } finally {
          ldstr "In finally 2"
	  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)          
          endfinally
        }
      end_try1:

    } finally {
      .try start_try1 to end_try1
       catch [mscorlib]System.Object handler start_handler0 to end_handler0
       ldstr "In outer finally"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       endfinally
    }

    done: 
        ldloc.s    testLog
	    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
	
		ldloc.s    testLog
		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
	    ret
}
}
