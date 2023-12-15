// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}





// Metadata version: v4.0.x86chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly CPropOverflow
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {68FF9260-9E27-4CCF-88D3-5AE07292CE94}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01070000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Sample1
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          OverflowTest(int32 a) cil managed noinlining
  {
    // Code size       25 (0x19)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4     0x40000000
    IL_0007:  sub.ovf
    IL_0008:  starg.s    a
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4     0x40000000
    IL_0010:  sub.ovf
    IL_0011:  starg.s    a
    IL_0013:  ldarg.0
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method Sample1::OverflowTest

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       103 (0x67)
    .maxstack  2
    .locals init (bool V_0,
             int32 V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  ldc.i4.s   100
    IL_0005:  stloc.1
    .try
    {
      IL_0006:  nop
      IL_0007:  ldloc.1
      IL_0008:  call       int32 Sample1::OverflowTest(int32)
      IL_000d:  stloc.1
      IL_000e:  ldstr      "a={0}"
      IL_0013:  ldloc.1
      IL_0014:  box        [mscorlib]System.Int32
      IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_001e:  nop
      IL_001f:  nop
      IL_0020:  leave.s    IL_003a

    }  // end .try
    catch [mscorlib]System.OverflowException 
    {
      IL_0022:  pop
      IL_0023:  nop
      IL_0024:  ldstr      "Overflow test failed a={0}"
      IL_0029:  ldloc.1
      IL_002a:  box        [mscorlib]System.Int32
      IL_002f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0034:  nop
      IL_0035:  ldc.i4.1
      IL_0036:  stloc.0
      IL_0037:  nop
      IL_0038:  leave.s    IL_003a

    }  // end handler
    IL_003a:  nop
    IL_003b:  ldloc.0
    IL_003c:  ldc.i4.0
    IL_003d:  ceq
    IL_003f:  stloc.3
    IL_0040:  ldloc.3
    IL_0041:  brtrue.s   IL_0054

    IL_0043:  nop
    IL_0044:  ldstr      "Test Failed"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  nop
    IL_004f:  ldc.i4.s   101
    IL_0051:  stloc.2
    IL_0052:  br.s       IL_0065

    IL_0054:  nop
    IL_0055:  ldstr      "Passed"
    IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005f:  nop
    IL_0060:  ldc.i4.s   100
    IL_0062:  stloc.2
    IL_0063:  br.s       IL_0065

    IL_0065:  ldloc.2
    IL_0066:  ret
  } // end of method Sample1::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Sample1::.ctor

} // end of class Sample1


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file CPropOverflow.res
