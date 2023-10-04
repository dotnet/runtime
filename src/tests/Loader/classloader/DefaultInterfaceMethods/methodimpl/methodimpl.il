// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly methodimpl
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {F249A85F-3FD2-488F-B084-97B542AD68D6}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02FD0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual 
          instance int32  Foo1(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IIFoo.Foo1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   1
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret    
  } // end of method IFoo::Foo1

  .method public hidebysig newslot virtual 
          instance int32  Foo2(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   2
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret  
  } // end of method IFoo::Foo2

  .method public hidebysig newslot virtual 
          instance int32  Foo3(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   3
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo3

  .method public hidebysig newslot virtual 
          instance int32  Foo4(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   4
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo4

  .method public hidebysig newslot virtual 
          instance int32  Foo5(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo5"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   5
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo5

  .method public hidebysig newslot virtual 
          instance int32  Foo6(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo6"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   6
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo6

  .method public hidebysig newslot virtual 
          instance int32  Foo7(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo7"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   7
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo7

  .method public hidebysig newslot virtual 
          instance int32  Foo8(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo8"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   8
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo8

  .method public hidebysig newslot virtual 
          instance int32  Foo9(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo.Foo9"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   9
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFoo::Foo9
} // end of class IFoo

.class interface private abstract auto ansi IBar
       implements IFoo  
{
 .method public hidebysig newslot virtual 
          instance int32  Bar1(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IIBar.Bar1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   11
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret    
  } // end of method IBar::Bar1

  .method public hidebysig newslot virtual 
          instance int32  Bar2(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   22
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret  
  } // end of method IBar::Bar2

  .method public hidebysig newslot virtual 
          instance int32  Bar3(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   33
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar3

  .method public hidebysig newslot virtual 
          instance int32  Bar4(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   44
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar4

  .method public hidebysig newslot virtual 
          instance int32  Bar5(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar5"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   55
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar5

  .method public hidebysig newslot virtual 
          instance int32  Bar6(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar6"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   66
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar6

  .method public hidebysig newslot virtual 
          instance int32  Bar7(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar7"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   77
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar7

  .method public hidebysig newslot virtual 
          instance int32  Bar8(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar8"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   88
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar8

  .method public hidebysig newslot virtual 
          instance int32  Bar9(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar.Bar9"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   99
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBar::Bar9 

  //////////////////////////////////////////////////////////////////////////////
  // Overriding IFoo
  //////////////////////////////////////////////////////////////////////////////

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo1(int32 a) cil managed
  {
    .override IFoo::Foo1
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::IFoo.Foo1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   10
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::IFoo.Foo1  

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo2(int32 a) cil managed
  {
    .override IFoo::Foo2
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::IFoo.Foo2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   20
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::IFoo.Foo2  

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo3(int32 a) cil managed
  {
    .override IFoo::Foo3
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::IFoo.Foo3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   30
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::IFoo.Foo3 

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo4(int32 a) cil managed
  {
    .override IFoo::Foo4
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::IFoo.Foo4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   40
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::IFoo.Foo4  

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo5(int32 a) cil managed
  {
    .override IFoo::Foo5
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::IFoo.Foo5"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   50
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::IFoo.Foo5 
} // end of class IBar

.class interface private abstract auto ansi IBlah
       implements IBar,
                  IFoo  
{
 .method public hidebysig newslot virtual
          instance int32  Blah1(int32 c) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     111
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret     
  } // end of method IBlah::Blah1

  .method public hidebysig newslot virtual
          instance int32  Blah2(int32 c) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     222
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::Blah2

  .method public hidebysig newslot virtual
          instance int32  Blah3(int32 c) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah.Blah3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     333
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBlah::Blah3   

  //////////////////////////////////////////////////////////////////////////////
  // Overriding IFoo
  //////////////////////////////////////////////////////////////////////////////

  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo6(int32 a) cil managed
  {
    .override IFoo::Foo6
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IFoo.Foo6"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   60
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IFoo.Foo6   

 .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo7(int32 a) cil managed
  {
    .override IFoo::Foo7
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IFoo.Foo7"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   70
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IFoo.Foo6  

 .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo8(int32 a) cil managed
  {
    .override IFoo::Foo8
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IFoo.Foo8"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   80
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IFoo.Foo8  

 .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo9(int32 a) cil managed
  {
    .override IFoo::Foo9
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IFoo.Foo9"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   90
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IFoo.Foo9

  //////////////////////////////////////////////////////////////////////////////
  // Overriding IBar
  //////////////////////////////////////////////////////////////////////////////

  .method private hidebysig newslot virtual final 
          instance int32  IBar.Bar1(int32 a) cil managed
  {
    .override IBar::Bar1
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IBar.Bar1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   110
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IBar.Bar1   

  .method private hidebysig newslot virtual final 
          instance int32  IBar.Bar2(int32 a) cil managed
  {
    .override IBar::Bar2
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IBar.Bar2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     220
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IBar.Bar2

  .method private hidebysig newslot virtual final 
          instance int32  IBar.Bar3(int32 a) cil managed
  {
    .override IBar::Bar3
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IBar.Bar3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     330
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IBar.Bar3

  .method private hidebysig newslot virtual final 
          instance int32  IBar.Bar4(int32 a) cil managed
  {
    .override IBar::Bar4
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IBar.Bar4"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     440
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IBar.Bar4

  .method private hidebysig newslot virtual final 
          instance int32  IBar.Bar5(int32 a) cil managed
  {
    .override IBar::Bar5
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlah::IBar.Bar5"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     550
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlah::IBar.Bar5  
} // end of class IBlah

.class private auto ansi beforefieldinit IBarImpl
       extends [mscorlib]System.Object
       implements IBar,
                  IFoo  
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method IBarImpl::.ctor

} // end of class IBarImpl

.class private auto ansi beforefieldinit IBlahImpl
       extends IBarImpl
       implements IBlah,
                  IBar,
                  IFoo
{
  .method private hidebysig newslot virtual final 
          instance int32  IBlah.Blah2(int32 c) cil managed
  {
    .override IBlah::Blah2
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlahImpl::IBlah.Blah2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     2220
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IBlahImpl::IBlah.Blah2

  .method private hidebysig newslot virtual final 
          instance int32  IBlah.Blah3(int32 c) cil managed
  {
    .override IBlah::Blah3
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBlahImpl::IBlah.Blah2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     3330
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBlahImpl::IBlah.Blah3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void IBarImpl::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method IBlahImpl::.ctor

} // end of class IBlahImpl   
 
.class interface private abstract auto ansi IFooBarBlah
       implements IFoo,
                  IBar,
                  IBlah
{
  .method private hidebysig newslot virtual final 
          instance int32  IFooBarBlah.FooBarBlah1(int32 c) cil managed
  {
    .override IFoo::Foo1
    .override IBar::Bar1
    .override IBlah::Blah1
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooBarBlah.FooBarBlah1"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     11111
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFooBarBlah.FooBarBlah1  
  .method private hidebysig newslot virtual final 
          instance int32  IFooBarBlah.FooBarBlah2(int32 c) cil managed
  {
    .override IFoo::Foo2
    .override IBar::Bar2
    .override IBlah::Blah2
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooBarBlah.FooBarBlah2"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     22222
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFooBarBlah.FooBarBlah1

  .method private hidebysig newslot virtual final 
          instance int32  IFooBarBlah.FooBarBlah3(int32 c) cil managed
  {
    .override IFoo::Foo3
    .override IFoo::Foo4
    .override IFoo::Foo5
    .override IBar::Bar3
    .override IBar::Bar4
    .override IBar::Bar5
    .override IBlah::Blah3
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooBarBlah.FooBarBlah3"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     33333
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret   
  } // end of method IFooBarBlah.FooBarBlah3
} // end of class IFooBarBlah  

.class private auto ansi beforefieldinit FooBarBlahImpl
       implements IFooBarBlah,
                  IFoo,
                  IBar,
                  IBlah
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void IBlahImpl::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method FooBarBlahImpl::.ctor

} // end of class FooBarBlahImpl         

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       23 (0x17)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void Program::SingleOverride()
    IL_0006:  nop
    IL_0007:  call       void Program::MultiOverride()
    IL_000c:  nop
    IL_000d:  call       int32 Test::Ret()
    IL_0012:  stloc.0
    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.0
    IL_0016:  ret
  } // end of method Program::Main

  .method private hidebysig static void  SingleOverride() cil managed
  {
    // Code size       946 (0x3b2)
    .maxstack  2
    .locals init (class IBarImpl V_0,
             class IFoo V_1,
             class IBar V_2,
             class IBlahImpl V_3,
             class IBlah V_4)
    IL_0000:  nop
    IL_0001:  newobj     instance void IBarImpl::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldstr      "Calling IFoo.Foo methods on IBarImpl..."
    IL_000e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.1
    IL_0016:  callvirt   instance int32 IFoo::Foo1(int32)
    IL_001b:  ldc.i4.s   11
    IL_001d:  ceq
    IL_001f:  ldstr      "Calling IFoo.Foo1 on IBarImpl"
    IL_0024:  call       void Test::Assert(bool,
                                           string)
    IL_0029:  nop
    IL_002a:  ldloc.1
    IL_002b:  ldc.i4.2
    IL_002c:  callvirt   instance int32 IFoo::Foo2(int32)
    IL_0031:  ldc.i4.s   22
    IL_0033:  ceq
    IL_0035:  ldstr      "Calling IFoo.Foo2 on IBarImpl"
    IL_003a:  call       void Test::Assert(bool,
                                           string)
    IL_003f:  nop
    IL_0040:  ldloc.1
    IL_0041:  ldc.i4.3
    IL_0042:  callvirt   instance int32 IFoo::Foo3(int32)
    IL_0047:  ldc.i4.s   33
    IL_0049:  ceq
    IL_004b:  ldstr      "Calling IFoo.Foo3 on IBarImpl"
    IL_0050:  call       void Test::Assert(bool,
                                           string)
    IL_0055:  nop
    IL_0056:  ldloc.1
    IL_0057:  ldc.i4.4
    IL_0058:  callvirt   instance int32 IFoo::Foo4(int32)
    IL_005d:  ldc.i4.s   44
    IL_005f:  ceq
    IL_0061:  ldstr      "Calling IFoo.Foo4 on IBarImpl"
    IL_0066:  call       void Test::Assert(bool,
                                           string)
    IL_006b:  nop
    IL_006c:  ldloc.1
    IL_006d:  ldc.i4.5
    IL_006e:  callvirt   instance int32 IFoo::Foo5(int32)
    IL_0073:  ldc.i4.s   55
    IL_0075:  ceq
    IL_0077:  ldstr      "Calling IFoo.Foo5 on IBarImpl"
    IL_007c:  call       void Test::Assert(bool,
                                           string)
    IL_0081:  nop
    IL_0082:  ldloc.1
    IL_0083:  ldc.i4.0
    IL_0084:  callvirt   instance int32 IFoo::Foo6(int32)
    IL_0089:  ldc.i4.6
    IL_008a:  ceq
    IL_008c:  ldstr      "Calling IFoo.Foo6 on IBarImpl"
    IL_0091:  call       void Test::Assert(bool,
                                           string)
    IL_0096:  nop
    IL_0097:  ldloc.1
    IL_0098:  ldc.i4.0
    IL_0099:  callvirt   instance int32 IFoo::Foo7(int32)
    IL_009e:  ldc.i4.7
    IL_009f:  ceq
    IL_00a1:  ldstr      "Calling IFoo.Foo7 on IBarImpl"
    IL_00a6:  call       void Test::Assert(bool,
                                           string)
    IL_00ab:  nop
    IL_00ac:  ldloc.1
    IL_00ad:  ldc.i4.0
    IL_00ae:  callvirt   instance int32 IFoo::Foo8(int32)
    IL_00b3:  ldc.i4.8
    IL_00b4:  ceq
    IL_00b6:  ldstr      "Calling IFoo.Foo8 on IBarImpl"
    IL_00bb:  call       void Test::Assert(bool,
                                           string)
    IL_00c0:  nop
    IL_00c1:  ldloc.1
    IL_00c2:  ldc.i4.0
    IL_00c3:  callvirt   instance int32 IFoo::Foo9(int32)
    IL_00c8:  ldc.i4.s   9
    IL_00ca:  ceq
    IL_00cc:  ldstr      "Calling IFoo.Foo9 on IBarImpl"
    IL_00d1:  call       void Test::Assert(bool,
                                           string)
    IL_00d6:  nop
    IL_00d7:  ldloc.0
    IL_00d8:  stloc.2
    IL_00d9:  ldstr      "Calling IBar.Bar methods on IBarImpl..."
    IL_00de:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_00e3:  nop
    IL_00e4:  ldloc.2
    IL_00e5:  ldc.i4.0
    IL_00e6:  callvirt   instance int32 IBar::Bar1(int32)
    IL_00eb:  ldc.i4.s   11
    IL_00ed:  ceq
    IL_00ef:  ldstr      "Calling IBar.Bar1 on IBarImpl"
    IL_00f4:  call       void Test::Assert(bool,
                                           string)
    IL_00f9:  nop
    IL_00fa:  ldloc.2
    IL_00fb:  ldc.i4.0
    IL_00fc:  callvirt   instance int32 IBar::Bar2(int32)
    IL_0101:  ldc.i4.s   22
    IL_0103:  ceq
    IL_0105:  ldstr      "Calling IBar.Bar2 on IBarImpl"
    IL_010a:  call       void Test::Assert(bool,
                                           string)
    IL_010f:  nop
    IL_0110:  ldloc.2
    IL_0111:  ldc.i4.0
    IL_0112:  callvirt   instance int32 IBar::Bar3(int32)
    IL_0117:  ldc.i4.s   33
    IL_0119:  ceq
    IL_011b:  ldstr      "Calling IBar.Bar3 on IBarImpl"
    IL_0120:  call       void Test::Assert(bool,
                                           string)
    IL_0125:  nop
    IL_0126:  ldloc.2
    IL_0127:  ldc.i4.0
    IL_0128:  callvirt   instance int32 IBar::Bar4(int32)
    IL_012d:  ldc.i4.s   44
    IL_012f:  ceq
    IL_0131:  ldstr      "Calling IBar.Bar4 on IBarImpl"
    IL_0136:  call       void Test::Assert(bool,
                                           string)
    IL_013b:  nop
    IL_013c:  ldloc.2
    IL_013d:  ldc.i4.0
    IL_013e:  callvirt   instance int32 IBar::Bar5(int32)
    IL_0143:  ldc.i4.s   55
    IL_0145:  ceq
    IL_0147:  ldstr      "Calling IBar.Bar5 on IBarImpl"
    IL_014c:  call       void Test::Assert(bool,
                                           string)
    IL_0151:  nop
    IL_0152:  ldloc.2
    IL_0153:  ldc.i4.0
    IL_0154:  callvirt   instance int32 IBar::Bar6(int32)
    IL_0159:  ldc.i4.s   66
    IL_015b:  ceq
    IL_015d:  ldstr      "Calling IBar.Bar6 on IBarImpl"
    IL_0162:  call       void Test::Assert(bool,
                                           string)
    IL_0167:  nop
    IL_0168:  ldloc.2
    IL_0169:  ldc.i4.0
    IL_016a:  callvirt   instance int32 IBar::Bar7(int32)
    IL_016f:  ldc.i4.s   77
    IL_0171:  ceq
    IL_0173:  ldstr      "Calling IBar.Bar7 on IBarImpl"
    IL_0178:  call       void Test::Assert(bool,
                                           string)
    IL_017d:  nop
    IL_017e:  ldloc.2
    IL_017f:  ldc.i4.0
    IL_0180:  callvirt   instance int32 IBar::Bar8(int32)
    IL_0185:  ldc.i4.s   88
    IL_0187:  ceq
    IL_0189:  ldstr      "Calling IBar.Bar8 on IBarImpl"
    IL_018e:  call       void Test::Assert(bool,
                                           string)
    IL_0193:  nop
    IL_0194:  ldloc.2
    IL_0195:  ldc.i4.0
    IL_0196:  callvirt   instance int32 IBar::Bar9(int32)
    IL_019b:  ldc.i4.s   99
    IL_019d:  ceq
    IL_019f:  ldstr      "Calling IBar.Bar9 on IBarImpl"
    IL_01a4:  call       void Test::Assert(bool,
                                           string)
    IL_01a9:  nop
    IL_01aa:  newobj     instance void IBlahImpl::.ctor()
    IL_01af:  stloc.3
    IL_01b0:  ldloc.3
    IL_01b1:  stloc.1
    IL_01b2:  ldloc.1
    IL_01b3:  ldc.i4.1
    IL_01b4:  callvirt   instance int32 IFoo::Foo1(int32)
    IL_01b9:  ldc.i4.s   11
    IL_01bb:  ceq
    IL_01bd:  ldstr      "Calling IFoo.Foo1 on IBlahImpl"
    IL_01c2:  call       void Test::Assert(bool,
                                           string)
    IL_01c7:  nop
    IL_01c8:  ldloc.1
    IL_01c9:  ldc.i4.2
    IL_01ca:  callvirt   instance int32 IFoo::Foo2(int32)
    IL_01cf:  ldc.i4.s   22
    IL_01d1:  ceq
    IL_01d3:  ldstr      "Calling IFoo.Foo2 on IBlahImpl"
    IL_01d8:  call       void Test::Assert(bool,
                                           string)
    IL_01dd:  nop
    IL_01de:  ldloc.1
    IL_01df:  ldc.i4.3
    IL_01e0:  callvirt   instance int32 IFoo::Foo3(int32)
    IL_01e5:  ldc.i4.s   33
    IL_01e7:  ceq
    IL_01e9:  ldstr      "Calling IFoo.Foo3 on IBlahImpl"
    IL_01ee:  call       void Test::Assert(bool,
                                           string)
    IL_01f3:  nop
    IL_01f4:  ldloc.1
    IL_01f5:  ldc.i4.4
    IL_01f6:  callvirt   instance int32 IFoo::Foo4(int32)
    IL_01fb:  ldc.i4.s   44
    IL_01fd:  ceq
    IL_01ff:  ldstr      "Calling IFoo.Foo4 on IBlahImpl"
    IL_0204:  call       void Test::Assert(bool,
                                           string)
    IL_0209:  nop
    IL_020a:  ldloc.1
    IL_020b:  ldc.i4.5
    IL_020c:  callvirt   instance int32 IFoo::Foo5(int32)
    IL_0211:  ldc.i4.s   55
    IL_0213:  ceq
    IL_0215:  ldstr      "Calling IFoo.Foo5 on IBlahImpl"
    IL_021a:  call       void Test::Assert(bool,
                                           string)
    IL_021f:  nop
    IL_0220:  ldloc.1
    IL_0221:  ldc.i4.6
    IL_0222:  callvirt   instance int32 IFoo::Foo6(int32)
    IL_0227:  ldc.i4.s   66
    IL_0229:  ceq
    IL_022b:  ldstr      "Calling IFoo.Foo6 on IBlahImpl"
    IL_0230:  call       void Test::Assert(bool,
                                           string)
    IL_0235:  nop
    IL_0236:  ldloc.1
    IL_0237:  ldc.i4.7
    IL_0238:  callvirt   instance int32 IFoo::Foo7(int32)
    IL_023d:  ldc.i4.s   77
    IL_023f:  ceq
    IL_0241:  ldstr      "Calling IFoo.Foo7 on IBlahImpl"
    IL_0246:  call       void Test::Assert(bool,
                                           string)
    IL_024b:  nop
    IL_024c:  ldloc.1
    IL_024d:  ldc.i4.8
    IL_024e:  callvirt   instance int32 IFoo::Foo8(int32)
    IL_0253:  ldc.i4.s   88
    IL_0255:  ceq
    IL_0257:  ldstr      "Calling IFoo.Foo8 on IBlahImpl"
    IL_025c:  call       void Test::Assert(bool,
                                           string)
    IL_0261:  nop
    IL_0262:  ldloc.1
    IL_0263:  ldc.i4.s   9
    IL_0265:  callvirt   instance int32 IFoo::Foo9(int32)
    IL_026a:  ldc.i4.s   99
    IL_026c:  ceq
    IL_026e:  ldstr      "Calling IFoo.Foo9 on IBlahImpl"
    IL_0273:  call       void Test::Assert(bool,
                                           string)
    IL_0278:  nop
    IL_0279:  ldloc.3
    IL_027a:  stloc.2
    IL_027b:  ldstr      "Calling IBar.Bar methods on IBlahImpl..."
    IL_0280:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0285:  nop
    IL_0286:  ldloc.2
    IL_0287:  ldc.i4.1
    IL_0288:  callvirt   instance int32 IBar::Bar1(int32)
    IL_028d:  ldc.i4.s   111
    IL_028f:  ceq
    IL_0291:  ldstr      "Calling IBar.Bar1 on IBlahImpl"
    IL_0296:  call       void Test::Assert(bool,
                                           string)
    IL_029b:  nop
    IL_029c:  ldloc.2
    IL_029d:  ldc.i4.2
    IL_029e:  callvirt   instance int32 IBar::Bar2(int32)
    IL_02a3:  ldc.i4     0xde
    IL_02a8:  ceq
    IL_02aa:  ldstr      "Calling IBar.Bar2 on IBlahImpl"
    IL_02af:  call       void Test::Assert(bool,
                                           string)
    IL_02b4:  nop
    IL_02b5:  ldloc.2
    IL_02b6:  ldc.i4.3
    IL_02b7:  callvirt   instance int32 IBar::Bar3(int32)
    IL_02bc:  ldc.i4     0x14d
    IL_02c1:  ceq
    IL_02c3:  ldstr      "Calling IBar.Bar3 on IBlahImpl"
    IL_02c8:  call       void Test::Assert(bool,
                                           string)
    IL_02cd:  nop
    IL_02ce:  ldloc.2
    IL_02cf:  ldc.i4.4
    IL_02d0:  callvirt   instance int32 IBar::Bar4(int32)
    IL_02d5:  ldc.i4     0x1bc
    IL_02da:  ceq
    IL_02dc:  ldstr      "Calling IBar.Bar4 on IBlahImpl"
    IL_02e1:  call       void Test::Assert(bool,
                                           string)
    IL_02e6:  nop
    IL_02e7:  ldloc.2
    IL_02e8:  ldc.i4.5
    IL_02e9:  callvirt   instance int32 IBar::Bar5(int32)
    IL_02ee:  ldc.i4     0x22b
    IL_02f3:  ceq
    IL_02f5:  ldstr      "Calling IBar.Bar5 on IBlahImpl"
    IL_02fa:  call       void Test::Assert(bool,
                                           string)
    IL_02ff:  nop
    IL_0300:  ldloc.2
    IL_0301:  ldc.i4.0
    IL_0302:  callvirt   instance int32 IBar::Bar6(int32)
    IL_0307:  ldc.i4.s   66
    IL_0309:  ceq
    IL_030b:  ldstr      "Calling IBar.Bar6 on IBlahImpl"
    IL_0310:  call       void Test::Assert(bool,
                                           string)
    IL_0315:  nop
    IL_0316:  ldloc.2
    IL_0317:  ldc.i4.0
    IL_0318:  callvirt   instance int32 IBar::Bar7(int32)
    IL_031d:  ldc.i4.s   77
    IL_031f:  ceq
    IL_0321:  ldstr      "Calling IBar.Bar7 on IBlahImpl"
    IL_0326:  call       void Test::Assert(bool,
                                           string)
    IL_032b:  nop
    IL_032c:  ldloc.2
    IL_032d:  ldc.i4.0
    IL_032e:  callvirt   instance int32 IBar::Bar8(int32)
    IL_0333:  ldc.i4.s   88
    IL_0335:  ceq
    IL_0337:  ldstr      "Calling IBar.Bar8 on IBlahImpl"
    IL_033c:  call       void Test::Assert(bool,
                                           string)
    IL_0341:  nop
    IL_0342:  ldloc.2
    IL_0343:  ldc.i4.0
    IL_0344:  callvirt   instance int32 IBar::Bar9(int32)
    IL_0349:  ldc.i4.s   99
    IL_034b:  ceq
    IL_034d:  ldstr      "Calling IBar.Bar9 on IBlahImpl"
    IL_0352:  call       void Test::Assert(bool,
                                           string)
    IL_0357:  nop
    IL_0358:  ldloc.3
    IL_0359:  stloc.s    V_4
    IL_035b:  ldstr      "Calling IBlah.Blah methods on IBlahImpl..."
    IL_0360:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0365:  nop
    IL_0366:  ldloc.s    V_4
    IL_0368:  ldc.i4.0
    IL_0369:  callvirt   instance int32 IBlah::Blah1(int32)
    IL_036e:  ldc.i4.s   111
    IL_0370:  ceq
    IL_0372:  ldstr      "Calling IBlah.Blah1 on IBlahImpl"
    IL_0377:  call       void Test::Assert(bool,
                                           string)
    IL_037c:  nop
    IL_037d:  ldloc.s    V_4
    IL_037f:  ldc.i4.2
    IL_0380:  callvirt   instance int32 IBlah::Blah2(int32)
    IL_0385:  ldc.i4     0x8ae
    IL_038a:  ceq
    IL_038c:  ldstr      "Calling IBlah.Blah1 on IBlahImpl"
    IL_0391:  call       void Test::Assert(bool,
                                           string)
    IL_0396:  nop
    IL_0397:  ldloc.s    V_4
    IL_0399:  ldc.i4.3
    IL_039a:  callvirt   instance int32 IBlah::Blah3(int32)
    IL_039f:  ldc.i4     0xd05
    IL_03a4:  ceq
    IL_03a6:  ldstr      "Calling IBlah.Blah1 on IBlahImpl"
    IL_03ab:  call       void Test::Assert(bool,
                                           string)
    IL_03b0:  nop
    IL_03b1:  ret
  } // end of method Program::SingleOverride

  .method private hidebysig static void  MultiOverride() cil managed
  {
    // Code size       549 (0x225)
    .maxstack  2
    .locals init (class FooBarBlahImpl V_0,
             class IFoo V_1,
             class IBar V_2,
             class IBlah V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void FooBarBlahImpl::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldstr      "Calling IFoo.Foo methods on FooBarBlahImpl..."
    IL_000e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  ldc.i4.0
    IL_0016:  callvirt   instance int32 IFoo::Foo1(int32)
    IL_001b:  ldc.i4     0x2b67
    IL_0020:  ceq
    IL_0022:  ldstr      "Calling IFoo.Foo1 on FooBarBlahImpl"
    IL_0027:  call       void Test::Assert(bool,
                                           string)
    IL_002c:  nop
    IL_002d:  ldloc.1
    IL_002e:  ldc.i4.0
    IL_002f:  callvirt   instance int32 IFoo::Foo2(int32)
    IL_0034:  ldc.i4     0x56ce
    IL_0039:  ceq
    IL_003b:  ldstr      "Calling IFoo.Foo2 on FooBarBlahImpl"
    IL_0040:  call       void Test::Assert(bool,
                                           string)
    IL_0045:  nop
    IL_0046:  ldloc.1
    IL_0047:  ldc.i4.0
    IL_0048:  callvirt   instance int32 IFoo::Foo3(int32)
    IL_004d:  ldc.i4     0x8235
    IL_0052:  ceq
    IL_0054:  ldstr      "Calling IFoo.Foo3 on FooBarBlahImpl"
    IL_0059:  call       void Test::Assert(bool,
                                           string)
    IL_005e:  nop
    IL_005f:  ldloc.1
    IL_0060:  ldc.i4.0
    IL_0061:  callvirt   instance int32 IFoo::Foo4(int32)
    IL_0066:  ldc.i4     0x8235
    IL_006b:  ceq
    IL_006d:  ldstr      "Calling IFoo.Foo4 on FooBarBlahImpl"
    IL_0072:  call       void Test::Assert(bool,
                                           string)
    IL_0077:  nop
    IL_0078:  ldloc.1
    IL_0079:  ldc.i4.0
    IL_007a:  callvirt   instance int32 IFoo::Foo5(int32)
    IL_007f:  ldc.i4     0x8235
    IL_0084:  ceq
    IL_0086:  ldstr      "Calling IFoo.Foo5 on FooBarBlahImpl"
    IL_008b:  call       void Test::Assert(bool,
                                           string)
    IL_0090:  nop
    IL_0091:  ldloc.1
    IL_0092:  ldc.i4.6
    IL_0093:  callvirt   instance int32 IFoo::Foo6(int32)
    IL_0098:  ldc.i4.s   66
    IL_009a:  ceq
    IL_009c:  ldstr      "Calling IFoo.Foo6 on FooBarBlahImpl"
    IL_00a1:  call       void Test::Assert(bool,
                                           string)
    IL_00a6:  nop
    IL_00a7:  ldloc.1
    IL_00a8:  ldc.i4.7
    IL_00a9:  callvirt   instance int32 IFoo::Foo7(int32)
    IL_00ae:  ldc.i4.s   77
    IL_00b0:  ceq
    IL_00b2:  ldstr      "Calling IFoo.Foo7 on FooBarBlahImpl"
    IL_00b7:  call       void Test::Assert(bool,
                                           string)
    IL_00bc:  nop
    IL_00bd:  ldloc.1
    IL_00be:  ldc.i4.8
    IL_00bf:  callvirt   instance int32 IFoo::Foo8(int32)
    IL_00c4:  ldc.i4.s   88
    IL_00c6:  ceq
    IL_00c8:  ldstr      "Calling IFoo.Foo8 on FooBarBlahImpl"
    IL_00cd:  call       void Test::Assert(bool,
                                           string)
    IL_00d2:  nop
    IL_00d3:  ldloc.1
    IL_00d4:  ldc.i4.s   9
    IL_00d6:  callvirt   instance int32 IFoo::Foo9(int32)
    IL_00db:  ldc.i4.s   99
    IL_00dd:  ceq
    IL_00df:  ldstr      "Calling IFoo.Foo9 on FooBarBlahImpl"
    IL_00e4:  call       void Test::Assert(bool,
                                           string)
    IL_00e9:  nop
    IL_00ea:  ldloc.0
    IL_00eb:  stloc.2
    IL_00ec:  ldstr      "Calling IBar.Bar methods on FooBarBlahImpl..."
    IL_00f1:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_00f6:  nop
    IL_00f7:  ldloc.2
    IL_00f8:  ldc.i4.0
    IL_00f9:  callvirt   instance int32 IBar::Bar1(int32)
    IL_00fe:  ldc.i4     0x2b67
    IL_0103:  ceq
    IL_0105:  ldstr      "Calling IBar.Bar1 on FooBarBlahImpl"
    IL_010a:  call       void Test::Assert(bool,
                                           string)
    IL_010f:  nop
    IL_0110:  ldloc.2
    IL_0111:  ldc.i4.0
    IL_0112:  callvirt   instance int32 IBar::Bar2(int32)
    IL_0117:  ldc.i4     0x56ce
    IL_011c:  ceq
    IL_011e:  ldstr      "Calling IBar.Bar2 on FooBarBlahImpl"
    IL_0123:  call       void Test::Assert(bool,
                                           string)
    IL_0128:  nop
    IL_0129:  ldloc.2
    IL_012a:  ldc.i4.0
    IL_012b:  callvirt   instance int32 IBar::Bar3(int32)
    IL_0130:  ldc.i4     0x8235
    IL_0135:  ceq
    IL_0137:  ldstr      "Calling IBar.Bar3 on FooBarBlahImpl"
    IL_013c:  call       void Test::Assert(bool,
                                           string)
    IL_0141:  nop
    IL_0142:  ldloc.2
    IL_0143:  ldc.i4.0
    IL_0144:  callvirt   instance int32 IBar::Bar4(int32)
    IL_0149:  ldc.i4     0x8235
    IL_014e:  ceq
    IL_0150:  ldstr      "Calling IBar.Bar4 on FooBarBlahImpl"
    IL_0155:  call       void Test::Assert(bool,
                                           string)
    IL_015a:  nop
    IL_015b:  ldloc.2
    IL_015c:  ldc.i4.0
    IL_015d:  callvirt   instance int32 IBar::Bar5(int32)
    IL_0162:  ldc.i4     0x8235
    IL_0167:  ceq
    IL_0169:  ldstr      "Calling IBar.Bar5 on FooBarBlahImpl"
    IL_016e:  call       void Test::Assert(bool,
                                           string)
    IL_0173:  nop
    IL_0174:  ldloc.2
    IL_0175:  ldc.i4.0
    IL_0176:  callvirt   instance int32 IBar::Bar6(int32)
    IL_017b:  ldc.i4.s   66
    IL_017d:  ceq
    IL_017f:  ldstr      "Calling IBar.Bar6 on FooBarBlahImpl"
    IL_0184:  call       void Test::Assert(bool,
                                           string)
    IL_0189:  nop
    IL_018a:  ldloc.2
    IL_018b:  ldc.i4.0
    IL_018c:  callvirt   instance int32 IBar::Bar7(int32)
    IL_0191:  ldc.i4.s   77
    IL_0193:  ceq
    IL_0195:  ldstr      "Calling IBar.Bar7 on FooBarBlahImpl"
    IL_019a:  call       void Test::Assert(bool,
                                           string)
    IL_019f:  nop
    IL_01a0:  ldloc.2
    IL_01a1:  ldc.i4.0
    IL_01a2:  callvirt   instance int32 IBar::Bar8(int32)
    IL_01a7:  ldc.i4.s   88
    IL_01a9:  ceq
    IL_01ab:  ldstr      "Calling IBar.Bar8 on FooBarBlahImpl"
    IL_01b0:  call       void Test::Assert(bool,
                                           string)
    IL_01b5:  nop
    IL_01b6:  ldloc.2
    IL_01b7:  ldc.i4.0
    IL_01b8:  callvirt   instance int32 IBar::Bar9(int32)
    IL_01bd:  ldc.i4.s   99
    IL_01bf:  ceq
    IL_01c1:  ldstr      "Calling IBar.Bar9 on FooBarBlahImpl"
    IL_01c6:  call       void Test::Assert(bool,
                                           string)
    IL_01cb:  nop
    IL_01cc:  ldloc.0
    IL_01cd:  stloc.3
    IL_01ce:  ldstr      "Calling IBlah.Blah methods on FooBarBlahImpl..."
    IL_01d3:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_01d8:  nop
    IL_01d9:  ldloc.3
    IL_01da:  ldc.i4.0
    IL_01db:  callvirt   instance int32 IBlah::Blah1(int32)
    IL_01e0:  ldc.i4     0x2b67
    IL_01e5:  ceq
    IL_01e7:  ldstr      "Calling IBlah.Blah1 on FooBarBlahImpl"
    IL_01ec:  call       void Test::Assert(bool,
                                           string)
    IL_01f1:  nop
    IL_01f2:  ldloc.3
    IL_01f3:  ldc.i4.0
    IL_01f4:  callvirt   instance int32 IBlah::Blah2(int32)
    IL_01f9:  ldc.i4     0x56ce
    IL_01fe:  ceq
    IL_0200:  ldstr      "Calling IBlah.Blah1 on FooBarBlahImpl"
    IL_0205:  call       void Test::Assert(bool,
                                           string)
    IL_020a:  nop
    IL_020b:  ldloc.3
    IL_020c:  ldc.i4.0
    IL_020d:  callvirt   instance int32 IBlah::Blah3(int32)
    IL_0212:  ldc.i4     0x8235
    IL_0217:  ceq
    IL_0219:  ldstr      "Calling IBlah.Blah1 on FooBarBlahImpl"
    IL_021e:  call       void Test::Assert(bool,
                                           string)
    IL_0223:  nop
    IL_0224:  ret
  } // end of method Program::MultiOverride

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program  

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field private static bool Pass
  .method public hidebysig static int32  Ret() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     bool Test::Pass
    IL_0006:  brtrue.s   IL_000c

    IL_0008:  ldc.i4.s   101
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   100
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Test::Ret

  .method public hidebysig static void  Assert(bool cond,
                                               string msg) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0015

    IL_0006:  nop
    IL_0007:  ldstr      "PASS"
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  br.s       IL_002e

    IL_0015:  nop
    IL_0016:  ldstr      "FAIL: "
    IL_001b:  ldarg.1
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.0
    IL_0028:  stsfld     bool Test::Pass
    IL_002d:  nop
    IL_002e:  ret
  } // end of method Test::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test::Pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file methodimpl.res
