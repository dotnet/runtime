// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'Rethrow2'
{
}


//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.method public static int32 ?test@@YAHXZ() il managed
{
  // Method begins at RVA 0x1000
  // Code size       120 (0x78)
  .maxstack  4
  .locals (int32 V_0,
           int32 V_1,
           int32 V_2,
           class [mscorlib]System.Exception V_3)

  IL_0000:  ldc.i4     0x1
  IL_0005:  stloc.0
  IL_0006:  ldc.i4     0x3
  IL_000b:  stloc.1
  IL_000c:  ldstr "In Test"//ldptr      D_00003020
  IL_0011:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
  IL_0016:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
  .try
  {
    IL_001b:  ldstr "In Try"//ldptr      D_00003034
    IL_0020:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0025:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_002a:  ldloc.1
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4     0x1
    IL_0031:  sub
    IL_0032:  div
    IL_0033:  stloc.1
    IL_0034:  ldstr "!!!AFTER THROW"//ldptr      D_00003044
    IL_0039:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_003e:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_0043:  ldc.i4     0x1
    IL_0048:  stloc.2
    IL_0049:  leave      IL_0070

  }  // end .try
  catch [mscorlib]System.Exception 
  {
    IL_004e:  stloc.3
    IL_004f:  ldstr "In Catch"//ldptr      D_00003064
    IL_0054:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0059:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_005e:  ldloc.3
    	      stloc.3
    IL_005f:  rethrow //throw

    IL_0060://  leave      IL_0065

  }  // end handler
  IL_0065:  ldc.i4     0x0
  IL_006a:  ret

  IL_006b:  br         IL_0072

  IL_0070:  ldloc.2
  IL_0071:  ret

  IL_0072:  ldc.i4     0x0
  IL_0077:  ret
  // Exception count 1
} // end of global method '?test@@YAHXZ'

.class public auto ansi rethrow2 {
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  // Method begins at RVA 0x10a0
  // Code size       145 (0x91)
  .entrypoint
  .maxstack  3
  .locals (int32 V_0,
           class [mscorlib]System.Exception V_1,
           int32 V_2,
           int32 V_3,
           int32 V_4)

  IL_0000:  ldc.i4     0x2
  IL_0005:  stloc.0
  IL_0006:  ldstr "Nested Test"//ldptr      D_0000307C
  IL_000b:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
  IL_0010:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
  .try
  {
    IL_0015:  ldstr "Going into test"//ldptr      D_00003098
    IL_001a:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_001f:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_0024:  call       int32 ?test@@YAHXZ()
    IL_0029:  stloc.0
    IL_002a:  ldstr "!!!AFTER TEST CALL"//ldptr      D_000030B8
    IL_002f:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0034:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_0039:  leave      IL_0084

  }  // end .try
  catch [mscorlib]System.Exception 
  {
    IL_003e:  stloc.1
    IL_003f:  ldloc.0
    IL_0040:  ldc.i4     0x2
    IL_0045:  bne.un     IL_0066

    IL_004a:  ldstr "GREAT!"//ldptr      D_000030E0
    IL_004f:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0054:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_0059:  ldc.i4     0x64
    IL_005e:  stloc.2
    IL_005f:  ldloc.2
    IL_0060:  stloc.3
    IL_0061:  leave      IL_0089

    IL_0066:  ldstr "!!! NOT GOOD"//ldptr      D_000030F0
    IL_006b:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0070:  call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
    IL_0075:  ldc.i4     0x1
    IL_007a:  stloc.s    V_4
    IL_007c:  ldloc.s    V_4
    IL_007e:  stloc.3
    IL_007f:  leave      IL_0089

  }  // end handler
  IL_0084:  br         IL_008b

  IL_0089:  ldloc.3
  IL_008a:  ret

  IL_008b:  ldc.i4     0x0
  IL_0090:  ret
  // Exception count 1
} // end of global method 'main'
}

