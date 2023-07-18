// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// the simplest dead EH region that will not also have dead nonlocal exit


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
.assembly extern eh_common{}

.assembly ASSEMBLY_NAME
{
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001

.class public auto ansi Test_severaldeadehregions extends [mscorlib] System.Object {
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
    ldstr      "Done!"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
    ldc.i4     0xffff63c0
    stloc.0
    
    .try {
        ldstr "In try"
        call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        br skip

    .try {
    L:
       ldstr "In inner try, unreached"
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
       endfinally
       .try {
         ldstr "In finally, unreached"
         call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       L2:
         br.s L2
       } catch [mscorlib]System.Exception {
         leave.s L3
       }
       L3:
       endfinally
    }

    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }


    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }

    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }


    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }


    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }


    .try {
       ldstr "Unreached"
       call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
       leave.s done
    } catch [mscorlib]System.Exception {
       leave.s done
    }

skip:
      leave.s done
    } catch [mscorlib]System.Object {
      ldstr "In catch, unreached"
      call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      leave.s done
    }
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
