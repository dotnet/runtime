// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern retargetable mscorlib
{
  .publickeytoken = (96 9D B8 05 3D 33 22 AC )                         // ....=3".
  .ver 0:0:0:0
}
.assembly extern mscorlib as mscorlib_1
{
  .ver 2:0:0:0
}
.assembly extern BarImpl
{
  .ver 0:0:0:0
}
.assembly extern MyBar
{
  .ver 0:0:0:0
}
.assembly CMain
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {01C3371C-004D-0000-A745-03831C37C341}
.imagebase 0x034f0000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit CMain
       extends [mscorlib_1]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       106 (0x6a)
    .maxstack  2
    .locals init (bool V_0,
             class [BarImpl]BarImpl V_1,
             class [MyBar]MyBar V_2,
             int32 V_3)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  newobj     instance void [BarImpl]BarImpl::.ctor()
    IL_0007:  stloc.1
    IL_0008:  ldc.i4.2
    IL_0009:  ldloc.1
    IL_000a:  callvirt   instance int32 [MyBar]MyBar::DoBar()
    IL_000f:  beq.s      IL_001d

    IL_0011:  ldstr      "FAIL: expected BarImpl.DoBar to execute, but anoth"
    + "er method was executed instead."
    IL_0016:  call       void [mscorlib_1]System.Console::WriteLine(string)
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.0
    IL_001d:  ldloc.1
    IL_001e:  stloc.2
    IL_001f:  ldc.i4.2
    IL_0020:  ldloc.2
    IL_0021:  callvirt   instance int32 [MyBar]MyBar::DoBar()
    IL_0026:  beq.s      IL_0034

    IL_0028:  ldstr      "FAIL: expected BarImpl.DoBar to execute, but anoth"
    + "er method was executed instead."
    IL_002d:  call       void [mscorlib_1]System.Console::WriteLine(string)
    IL_0032:  ldc.i4.0
    IL_0033:  stloc.0
    IL_0034:









    IL_0049:  ldloc.0
    IL_004a:  brtrue.s   IL_005b

    IL_004c:  ldc.i4.s   101
    IL_004e:  stloc.3
    IL_004f:  ldstr      "FAIL"
    IL_0054:  call       void [mscorlib_1]System.Console::WriteLine(string)
    IL_0059:  br.s       IL_0068

    IL_005b:  ldc.i4.s   100
    IL_005d:  stloc.3
    IL_005e:  ldstr      "PASS"
    IL_0063:  call       void [mscorlib_1]System.Console::WriteLine(string)
    IL_0068:  ldloc.3
    IL_0069:  ret
  } // end of method CMain::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib_1]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CMain::.ctor

} // end of class CMain


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
