// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.21010
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly b170362_new
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module b170362_new.exe
// MVID: {5699A10B-EE3A-4BFF-B847-74894515E3AE}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x030A0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit MyStruct
       extends [mscorlib]System.ValueType
{
  .field public uint8 x1
  .field public int32 x2
} // end of class MyStruct

.class private auto ansi beforefieldinit MainApp
       extends [mscorlib]System.Object
{
  .field private static uint8 s
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       74 (0x4a)
    .maxstack  3
    .locals init (valuetype MyStruct V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  ldsfld     uint8 MainApp::s
    IL_0008:  stfld      uint8 MyStruct::x1
    IL_000d:  ldloca.s   V_0
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldfld      uint8 MyStruct::x1
    IL_0016:  ldc.i4.1
    IL_0017:  or
   // IL_0018:  conv.u1
    IL_0019:  stfld      uint8 MyStruct::x1
    IL_001e:  ldloca.s   V_0
    IL_0020:  ldfld      uint8 MyStruct::x1
    IL_0025:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_002a:  nop
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldfld      uint8 MyStruct::x1
    IL_0032:  ldc.i4.1
    IL_0033:  ceq
    IL_0035:  ldc.i4.0
    IL_0036:  ceq
    IL_0038:  stloc.2
    IL_0039:  ldloc.2
    IL_003a:  brtrue.s   IL_0042

    IL_003c:  nop
    IL_003d:  ldc.i4.s   100
    IL_003f:  stloc.1
    IL_0040:  br.s       IL_0048

    IL_0042:  nop
    IL_0043:  ldc.i4.s   101
    IL_0045:  stloc.1
    IL_0046:  br.s       IL_0048

    IL_0048:  ldloc.1
    IL_0049:  ret
  } // end of method MainApp::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MainApp::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     uint8 MainApp::s
    IL_0006:  ret
  } // end of method MainApp::.cctor

} // end of class MainApp


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file b170362_new.res
