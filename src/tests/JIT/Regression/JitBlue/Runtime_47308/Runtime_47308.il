// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}

.assembly Test1 {}
.assembly extern mscorlib{auto}
.class FullProof {
.method static int32 Test() {
.maxstack 500

IL_352:       ldc.i8 1
IL_353:       conv.ovf.u8.un

IL_417:       ldc.i8 1

IL_428:       conv.ovf.u8.un
IL_429:       ldc.r8 -55.5
IL_434:       conv.r4
IL_435:       nop
IL_436:       ldc.r4 55.5
IL_437:       ldc.r4 55.5
IL_438:       ldc.r8 0.0
IL_439:       nop
IL_440:       beq IL_457
IL_441:       conv.r8
IL_450:       nop
IL_451:       pop
IL_452:       pop
IL_453:       conv.ovf.u8
IL_454:       ldc.r8 -55.5
IL_455:       ldc.r4 -0.0
IL_456:       nop
IL_457:       add


IL_490:       pop
IL_491:       conv.ovf.i8.un
              pop
IL_500:       ret
}

.method public static int32  Main()
{
.entrypoint
.maxstack 3
   .try
   {
      call int32 FullProof::Test()
      pop
      leave.s WithoutEH
   }
   catch [mscorlib]System.Exception 
   {
      call       void [System.Console]System.Console::WriteLine(object)
      leave.s AfterEH
   }
   
WithoutEH:
   ldstr      "PASS"
   call       void [System.Console]System.Console::WriteLine(string)
   ldc.i4.s   100
   br RETURN
   
AfterEH:

   ldstr      "FAIL"
   call       void [System.Console]System.Console::WriteLine(string)
   ldc.i4.s   101
   br RETURN

RETURN:

   ret
}
}
