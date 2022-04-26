// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// trybody in between catch handlers


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
.assembly 'trybodyinbetweencatchhandlers'
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.class public auto ansi Test_trybodyinbetweencatchhandlers extends [mscorlib] System.Object {
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
    ldstr      "In try 1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In try 2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In handler2_2 "
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally2_1 "
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally1a_1 "
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "Done!"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

        ldc.i4     0xffff63c0
        stloc.0
    br start_try1a


    start_try1a:
        br.s start_try1


    start_handler1_1:
	ldstr "In handler 1_1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave    done
    end_handler1_1: 

    .try {
       leave.s start_try1a
    } finally {
       endfinally
    }

    start_try1:
	ldstr "In try 1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    	br start_try2a

	ldstr "Unreached"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        loop2:
        br loop2

    start_finally2_1:
	ldstr "In finally2_1 "
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        endfinally
    end_finally2_1:

	ldstr "Unreached"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

    start_try2a:
        br start_try2

    start_handler2_1:
	ldstr "In handler 2_1"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
    end_handler2_1: 

    done3: 
	ldstr "In done 3"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s	done

    start_try2:
    	ldc.i4     0xffff63c0
	ldstr "In try 2"
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        newobj     instance void [mscorlib]System.Exception::.ctor()
        throw
    	leave.s    done2
        leave.s done2
    end_try2: 

    start_handler2_2:
        pop
        ldc.i4     0xffff63c0
	ldstr "In handler2_2 "
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        leave.s    done
        leave.s done2
    end_handler2_2: 

    done2: 
	ldstr "In done2 "
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        br	done3

    end_try2a:

    end_try1:

    end_try1a:

    start_finally1a_1:
	ldstr "In finally1a_1 "
	call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        endfinally
    end_finally1a_1:

    .try start_try2 to end_try2
    catch [mscorlib]System.ArithmeticException handler start_handler2_1 to end_handler2_1
    catch [mscorlib]System.Exception handler start_handler2_2 to end_handler2_2

    .try start_try2a to end_try2a
    finally handler start_finally2_1 to end_finally2_1

    .try start_try1 to end_try1
    catch [mscorlib]System.Object handler start_handler1_1 to end_handler1_1

    .try start_try1a to end_try1a
    finally handler start_finally1a_1 to end_finally1a_1


    done: 
		ldstr "Done!"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldloc.s    testLog
	    callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
	    ldloc.s    testLog
	    callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
	    ret
}
}
