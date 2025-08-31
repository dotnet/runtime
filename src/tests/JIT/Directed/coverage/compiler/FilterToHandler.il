// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly extern mscorlib {}
.assembly extern common {}
.assembly 'FilterToHandler' {}
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


.class public auto ansi FilterToHandler {
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
    string('https://github.com/dotnet/runtime/issues/114902')
    type([TestLibrary]TestLibrary.PlatformDetection)
    string[1] ('IsAppleMobile')
}
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
}
