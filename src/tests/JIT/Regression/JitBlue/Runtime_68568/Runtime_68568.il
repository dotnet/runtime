// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly 'Runtime_68568'
{
}

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Runtime_68568
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       79 (0x4f)
    .maxstack  2
    .locals init (bool V_0)

// Code equivalent to the following, except without the
// int cast that C# adds on switch cases.
//
// switch (unchecked((nuint)0x100000000))
// {
//     case 0:
//         pass = sizeof(nint) == 4;
//         break;
//     case 1:
//         pass = false;
//         break;
//     default:
//         pass = sizeof(nint) == 8;
//         break;
// }
//
// if (pass)
// {
//     Console.WriteLine("PASS");
//     return 100;
// }
//
// Console.WriteLine("FAIL");
// return -1;


    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i8     0x100000000
    IL_000b:  conv.u
    IL_000c:  switch     (
                          IL_0025,
                          IL_0031)
    IL_0019:  sizeof     [System.Runtime]System.IntPtr
    IL_001f:  ldc.i4.8
    IL_0020:  ceq
    IL_0022:  stloc.0
    IL_0023:  br.s       IL_0033

    IL_0025:  sizeof     [System.Runtime]System.IntPtr
    IL_002b:  ldc.i4.4
    IL_002c:  ceq
    IL_002e:  stloc.0
    IL_002f:  br.s       IL_0033

    IL_0031:  ldc.i4.0
    IL_0032:  stloc.0
    IL_0033:  ldloc.0
    IL_0034:  brfalse.s  IL_0043

    IL_0036:  ldstr      "PASS"
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  ldc.i4.s   100
    IL_0042:  ret

    IL_0043:  ldstr      "FAIL"
    IL_0048:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004d:  ldc.i4.m1
    IL_004e:  ret
  } // end of method Runtime_68568::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_68568::.ctor

} // end of class Runtime_68568


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
