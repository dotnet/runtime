// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// dead region in funclet


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly extern common{}
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
    .maxstack  400
    .locals init (int32 V_0,
				  int32 V_1,
				  class [mscorlib]System.IO.StringWriter expectedOut,
				  class [common]TestUtil.TestLog testLog
			)	
    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "In try"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "Done!"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [common]TestUtil.TestLog::StartRecording()
    ldc.i4     0xffff63c0
    stloc.0
    
    .try {
    L:
       ldstr "In try"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

       leave    done

try_start: ldstr "In try"
           call       void [System.Console]System.Console::WriteLine(string)
           leave      done
           .try
           {
             ldstr      "In try"
             call       void [System.Console]System.Console::WriteLine(string)
             leave      done

           }  
           fault
           {
             ldstr      "In fault"
             call       void [System.Console]System.Console::WriteLine(string)
             endfault
           }  


filter_begin:  pop
            ldstr "In filter"
            call       void [System.Console]System.Console::WriteLine(string)
            ldc.i4.1 
            endfilter

handler_begin:  pop
            ldstr "In handler"
            call       void [System.Console]System.Console::WriteLine(string)
            leave.s    done
handler_end:

  .try try_start to filter_begin filter filter_begin handler handler_begin to handler_end

       leave.s done
    } finally {
       leave.s L5
       .try { 
       .try {
         ldstr "In finally, unreached"
         call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       L2:
         br.s L2
       } catch [mscorlib]System.Exception {
         leave.s L4
       }
       } finally {
         endfinally
       }
L4:
       .try {
           leave.s L5
       } fault {
           endfault
       } 
L5:
       endfinally
    }
    done: 
		ldstr "Done!"
		call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		ldloc.s    testLog
		callvirt   instance void [common]TestUtil.TestLog::StopRecording()
  
		ldloc.s    testLog
		callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()
	    ret
}
