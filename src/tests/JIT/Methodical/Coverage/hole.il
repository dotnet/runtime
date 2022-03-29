// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





// Metadata version: v1.2.30210
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly hole {}
.assembly extern xunit.core {}
// MVID: {972ABF67-C385-44E6-B525-571BCCECC13C}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000006FBFEC20000

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit BASIC
       extends [mscorlib]System.Object
{
  .field public static int32 x
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       115 (0x73)
    .maxstack  10
    .locals init (valuetype VT[] V_0)
    IL_0000:  ldc.i4.4
    IL_0001:  newarr     VT
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  ldelema    VT
    IL_000e:  ldstr      "Boo ya!"
    IL_0013:  ldc.i4.3
    IL_0014:  newobj     instance void VT::.ctor(string,
                                                 int32)
    IL_0019:  stobj      VT
    IL_001e:  ldloc.0
    IL_001f:  ldc.i4.1
    IL_0020:  ldelema    VT
    IL_0025:  ldstr      "Boo ya!"
    IL_002a:  ldc.i4.3
    IL_002b:  newobj     instance void VT::.ctor(string,
                                                 int32)
    IL_0030:  stobj      VT
    IL_0035:  ldloc.0
    IL_0036:  ldc.i4.2
    IL_0037:  ldelema    VT
    IL_003c:  ldstr      "Boo ya!"
    IL_0041:  ldc.i4.3
    IL_0042:  newobj     instance void VT::.ctor(string,
                                                 int32)
    IL_0047:  stobj      VT
    IL_004c:  ldloc.0
    IL_004d:  ldc.i4.3
    IL_004e:  ldelema    VT
    IL_0053:  ldstr      "Boo ya!"
    IL_0058:  ldc.i4.3
    IL_0059:  newobj     instance void VT::.ctor(string,
                                                 int32)
    IL_005e:  stobj      VT
    IL_0063:  ldsfld     int32 BASIC::x
    IL_0068:  ldc.i4.0
    IL_0069:  ble.s      IL_0072

    tail.
    call       int32 BASIC::Foo()
    ret
    
    IL_0072:   ldc.i4 1
    		ret
  } // end of method BASIC::Main

  .method public hidebysig static int32  Foo() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  10
    IL_0000:  ldstr      "In Foo: "
    IL_0005:  call       void [System.Console]System.Console::Write(string)
    IL_000a:  ldsfld     int32 BASIC::x
    IL_000f:  call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4 100
    IL_0014:  ret
  } // end of method BASIC::Foo

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  stsfld     int32 BASIC::x
    IL_0006:  ret
  } // end of method BASIC::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BASIC::.ctor

} // end of class BASIC

.class public sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field private string s
  .field private int32 x
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string s,
                               int32 x) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      string VT::s
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  stfld      int32 VT::x
    IL_000e:  ret
  } // end of method VT::.ctor

} // end of class VT


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file hole.res
