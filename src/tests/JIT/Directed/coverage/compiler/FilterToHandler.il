// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern common {}
.assembly test {}
.module test.exe

.method public static int32 MoveBetweenFilterAndHandler() {
  .maxstack  2

  .try {
try_start:  
      ldstr      "In try, throwing the first obj"
      call       void [System.Console]System.Console::WriteLine(string)
      newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
      throw

filter_begin:
      ldstr "In filter"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s handler_begin
      throw
      ldc.i4.1 
      endfilter

handler_begin:  
      pop
      ldstr "In handler, should be unreached"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s done2

handler_end:
  } catch [mscorlib]System.Exception {
    .try try_start to filter_begin filter filter_begin handler handler_begin to handler_end
      ldstr      "In catch"
      call       void [System.Console]System.Console::WriteLine(string)
      leave.s done2
  }

done2: 
  ldstr      "Done"
  call       void [System.Console]System.Console::WriteLine(string)

  ldc.i4 0x64  
  ret
}


.method static int32 Main() {
.entrypoint
.maxstack	20
  .try
  {
    call int32 MoveBetweenFilterAndHandler()
    leave FAIL
  }
  catch [mscorlib]System.InvalidProgramException
  {
    leave PASS
  }

PASS:
  ldstr      "PASS"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4     100
  ret

FAIL:
  ldstr      "FAIL"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  ret
}
