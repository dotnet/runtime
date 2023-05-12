// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// local variable value should be preserved in all nested handlers


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern eh_common {}
.assembly ASSEMBLY_NAME {}


.class public auto ansi Test_localvarincatch extends [mscorlib] System.Object {
.method public static int32 main() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (
    int32 V_0,
    int32 V1,
    class [mscorlib]System.IO.StringWriter expectedOut,
    class [eh_common]TestUtil.TestLog testLog
  )

  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
  stloc.s    expectedOut

  ldloc.s    expectedOut
  ldstr      "begin"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 0, ldloc.0 is 0"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In catch [mscorlib]System.Exception 0"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 1, ldloc.0 is 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In catch [mscorlib]System.Exception 1"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 2, ldloc.0 is 2"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In catch [mscorlib]System.Exception 2"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In try 3, ldloc.0 is 3"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "In catch [mscorlib]System.Exception 3"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  ldloc.s    expectedOut
  ldstr      "begin"
  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

  ldloc.s    expectedOut
  newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
  stloc.s    testLog

  ldloc.s    testLog
  callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

  ldc.i4 0
  stloc.0
begin:
  ldstr      "begin"
  call       void [System.Console]System.Console::WriteLine(string)
  ldloc.0
  brtrue done
  label0:
  .try {
    ldstr      "In try 0, ldloc.0 is "
    call       void [System.Console]System.Console::Write(string)
    ldloc.0
    dup
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4.1
    add
    stloc.0
    newobj     instance void [mscorlib]System.Exception::.ctor()
    throw
  } catch [mscorlib]System.Exception  {
    pop
    ldstr      "In catch [mscorlib]System.Exception 0"
    call       void [System.Console]System.Console::WriteLine(string)
    label1:
    .try {
      ldstr      "In try 1, ldloc.0 is "
      call       void [System.Console]System.Console::Write(string)
      ldloc.0
      dup
      call       void [System.Console]System.Console::WriteLine(int32)
      ldc.i4.1
      add
      stloc.0
      newobj     instance void [mscorlib]System.Exception::.ctor()
      throw
    } catch [mscorlib]System.Exception  {
      pop
      ldstr      "In catch [mscorlib]System.Exception 1"
      call       void [System.Console]System.Console::WriteLine(string)
      label2:
      .try {
        ldstr      "In try 2, ldloc.0 is "
        call       void [System.Console]System.Console::Write(string)
        ldloc.0
        dup
        call       void [System.Console]System.Console::WriteLine(int32)
        ldc.i4.1
        add
        stloc.0
        newobj     instance void [mscorlib]System.Exception::.ctor()
        throw
      } catch [mscorlib]System.Exception  {
        pop
        ldstr      "In catch [mscorlib]System.Exception 2"
        call       void [System.Console]System.Console::WriteLine(string)
        label3:
        .try {
          ldstr      "In try 3, ldloc.0 is "
          call       void [System.Console]System.Console::Write(string)
          ldloc.0
          dup
          call       void [System.Console]System.Console::WriteLine(int32)
          ldc.i4.1
          add
          stloc.0
          newobj     instance void [mscorlib]System.Exception::.ctor()
          throw
        } catch [mscorlib]System.Exception  {
          pop
          ldstr      "In catch [mscorlib]System.Exception 3"
          call       void [System.Console]System.Console::WriteLine(string)
          ldloc.0
          brfalse.s next0
          ldloc.0
          ldc.i4 1
          beq.s next0a
          leave begin
        next0a:
          ldstr "Unreached !!"
          call       void [System.Console]System.Console::WriteLine(string)
          leave done
          leave label2
        next0:
          leave done
        }
        ldloc.0
        brfalse.s next1
        ldloc.0
        ldc.i4 1
        beq.s next1a
        leave begin
      next1a:
        ldstr "Unreached !!"
        call       void [System.Console]System.Console::WriteLine(string)
        leave done
        leave label1
      next1:
        leave done
      }
      ldloc.0
      brfalse.s next2
      ldloc.0
      ldc.i4 1
      beq.s next2a
      leave begin
    next2a:
      ldstr "Unreached !!"
      call       void [System.Console]System.Console::WriteLine(string)
      leave done
      leave label0
    next2:
      leave done
    }
    ldloc.0
    brfalse.s next3
    ldloc.0
    ldc.i4 1
    beq.s next3a
    leave begin
  next3a:
    ldstr "Unreached !!"
    call       void [System.Console]System.Console::WriteLine(string)
    leave done
    leave label0
  next3:
    leave done
  }

  done:
    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
    ldloc.s    testLog
    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    
    ret
}
}
