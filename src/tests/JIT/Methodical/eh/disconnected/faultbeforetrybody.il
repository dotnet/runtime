// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// fault before trybody

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
.assembly ASSEMBLY_NAME
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.class public auto ansi Test_faultbeforetrybody extends [mscorlib] System.Object {
.method public static int32 main() {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  400
    .locals init (int32 V_0,
				 int32 V_1,
				 class [mscorlib]System.IO.StringWriter expectedOut,
				class [eh_common]TestUtil.TestLog testLog
	)
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
        ldc.i4     0xffff63c0
        stloc.0
    br start_try1

    start_fault:
	ldstr "In fault -- unreached"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        endfault
    end_fault: 

    start_try1:
	ldstr "In try"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s done
    end_try1:

    .try start_try1 to end_try1
    fault handler start_fault to end_fault

    done: 
        ldloc.s    testLog
	    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
		ldloc.s    testLog
		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
	    ret
}
}
