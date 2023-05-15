// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

 .module 'rethrow.exe'
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'leave1'
{
  
  
}
.class value explicit ansi sealed $MultiByte$18
{
  .pack 1
  .size 18
} // end of class '$MultiByte$18'

.class value explicit ansi sealed $MultiByte$16
{
  .pack 1
  .size 16
} // end of class '$MultiByte$16'

.class value explicit ansi sealed $MultiByte$32
{
  .pack 1
  .size 32
} // end of class '$MultiByte$32'

.class value explicit ansi sealed $MultiByte$22
{
  .pack 1
  .size 22
} // end of class '$MultiByte$22'

.class value explicit ansi sealed $MultiByte$26
{
  .pack 1
  .size 26
} // end of class '$MultiByte$26'

.class value explicit ansi sealed $MultiByte$30
{
  .pack 1
  .size 30
} // end of class '$MultiByte$30'

.class value explicit ansi sealed $MultiByte$40
{
  .pack 1
  .size 40
} // end of class '$MultiByte$40'

.class public value explicit ansi sealed $MultiByte$28
{
  .pack 1
  .size 28
} // end of class '$MultiByte$28'

//Global fields
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//.field static privatescope value class $MultiByte$18 'unnamed-global-0'
//.field static privatescope value class $MultiByte$16 'unnamed-global-1'
//.field static privatescope value class $MultiByte$32 'unnamed-global-2'
//.field static privatescope value class $MultiByte$22 'unnamed-global-3'
//.field static privatescope value class $MultiByte$26 'unnamed-global-4'
//.field static privatescope value class $MultiByte$30 'unnamed-global-5'
//.field static privatescope value class $MultiByte$40 'unnamed-global-6'
//.field static privatescope value class $MultiByte$16 'unnamed-global-7'
//.field static privatescope value class $MultiByte$28 'unnamed-global-8'
//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.method public static int32 test() il managed
{
  // Code size       92 (0x5c)
  .maxstack  4
  .locals (int32 V_0,
           class [mscorlib]System.Exception V_1,
           int32 V_2,
           int32 V_3,
           class [mscorlib]System.Exception V_4)
  IL_0000:  ldnull
  IL_0001:  stloc.1
  IL_0002:  ldnull
  IL_0003:  stloc.s    V_4
  IL_0005:  ldc.i4.1
  IL_0006:  stloc.3
  IL_0007:  ldc.i4.3
  IL_0008:  stloc.0
	ldstr "In Test"
  IL_0013:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  .try
  {
ldstr "In try"
    IL_0022:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0027:  ldloc.0
    IL_0028:  ldloc.3
    IL_0029:  ldc.i4.1
    IL_002a:  sub
    IL_002b:  div
    IL_002c:  stloc.0
ldstr "!!! AFTER THROW"
    IL_0037:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_003c:  ldc.i4.1
    IL_003d:  stloc.2
    IL_003e:  leave.s    IL_0058

  }  // end .try
  catch [mscorlib]System.Exception 
  {
    IL_0040:  stloc.1
ldstr "In Catch"
    IL_004b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0050:  ldloc.1
    IL_0051:  throw

    IL_0052:  leave.s    IL_0054

  }  // end handler
  IL_0054:  ldc.i4.0
  IL_0055:  ret

  IL_0056:  br.s       IL_005a

  IL_0058:  ldloc.2
  IL_0059:  ret

  IL_005a:  ldc.i4.0
  IL_005b:  ret
} // end of global method 'test'

.class public auto ansi leave1 {
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .vtentry 1 : 1
.entrypoint
  // Code size       110 (0x6e)
  .maxstack  4
  .locals (int32 V_0,
           int32 V_1,
           class [mscorlib]System.Exception V_2,
           class [mscorlib]System.Exception V_3)
  IL_0000:  ldnull
  IL_0001:  stloc.3
  IL_0002:  ldc.i4.2
  IL_0003:  stloc.1
ldstr "Nested Try"
  IL_000e:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  .try
  {
ldstr "Going into test"
    IL_001d:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0022:  call       int32  test()
    IL_0027:  stloc.1
ldstr "!!! AFTER TEST"
    IL_0032:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0037:  leave.s    IL_006a

  }  // end .try
  catch [mscorlib]System.Exception 
  {
    IL_0039:  stloc.2
    IL_003a:  ldloc.1
    IL_003b:  ldc.i4.2
    IL_003c:  bne.un.s   IL_0051

ldstr "GREAT!"
    IL_0048:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_004d:  ldc.i4     0x64
    IL_004e:  stloc.0
    IL_004f:  leave.s    IL_0066

IL_0051:
ldstr "!!! HMM...."
    IL_005b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0060:  ldc.i4.1
    IL_0061:  stloc.0
    IL_0062:  leave.s    IL_0066

  }  // end handler
  IL_0064:  br.s       IL_006a

  IL_0066:  ldloc.0
  IL_0067:  ret

  IL_0068:  br.s       IL_006c

  IL_006a:  ldc.i4.0
  IL_006b:  ret

  IL_006c:  ldc.i4.0
  IL_006d:  ret
} // end of global method 'main'
}