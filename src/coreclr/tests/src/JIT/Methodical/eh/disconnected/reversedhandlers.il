// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly extern common {}
.assembly test
{
  .ver 0:0:0:0
}
.module test.exe
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.method public static int32 main() {
    .entrypoint
    .maxstack  2
    .locals init (	int32 V_0,
					int32 V_1,
					class [mscorlib]System.IO.StringWriter expectedOut,
					class [common]TestUtil.TestLog testLog
				)

    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try 1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In handler 2 (correct)"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StartRecording()

        ldc.i4     0xffff63c0
        stloc.0
    br start_try1

    start_try1:
	ldstr "In try 1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        newobj     instance void [mscorlib]System.NullReferenceException::.ctor()
        throw
    end_try1:

    start_handler0:
	ldstr "In handler 0"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
    end_handler0: 

    start_handler1:
	ldstr "In handler 1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
    end_handler1: 

    start_handler1a:
	ldstr "In handler 1a"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
    end_handler1a: 

    start_handler2:
	ldstr "In handler 2 (correct)"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
    end_handler2: 

    .try start_try1 to end_try1
    catch [mscorlib]System.NullReferenceException handler start_handler2 to end_handler2

    .try start_try1 to end_try1
    catch [mscorlib]System.SystemException handler start_handler1 to end_handler1

    .try start_try1 to end_try1
    catch [mscorlib]System.Exception handler start_handler1a to end_handler1a

    .try start_try1 to end_try1
    catch [mscorlib]System.Object handler start_handler0 to end_handler0

    done: 
         ldloc.s    testLog
	    callvirt   instance void [common]TestUtil.TestLog::StopRecording()
  
	    ldloc.s    testLog
	    callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()
	    ret
}
