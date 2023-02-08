// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b55216'{}
.class public auto ansi fault_class extends ['mscorlib']System.Object {

//+test_10_fault
//+desc: fault block before try block
.method public static void test_10_fault(){
.locals(int32 locInt1)

//init locals
  ldc.i4.1
  stloc locInt1

// print a message
  ldstr      "Test is running"
  call       void [System.Console]System.Console::WriteLine(class System.String) 

//exception handler goes here
br lbl_10_startTry

lbl_10_startFault:
 ldstr      "inside fault"
  call       void [System.Console]System.Console::WriteLine(class System.String) 


   endfault
lbl_10_endFault:

lbl_10_startTry:

  ldstr      "throwing excepion ... \n"
  call       void [System.Console]System.Console::WriteLine(class System.String) 

   newobj     instance void [mscorlib]System.Exception::.ctor()
   throw

  leave lbl_10_exitSEH
lbl_10_endTry:



.try  lbl_10_startTry to lbl_10_endTry fault  handler lbl_10_startFault to lbl_10_endFault

lbl_10_exitSEH:

  ldstr      "finishes ... \n"
  call       void [System.Console]System.Console::WriteLine(class System.String) 



  ret
}

.method public static int32 main()
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
	.try {
		call void fault_class::test_10_fault()
		leave END
	} catch [mscorlib]System.Exception {
		pop
		leave END
	}
END:
	ldc.i4 100
    ret
}
}
