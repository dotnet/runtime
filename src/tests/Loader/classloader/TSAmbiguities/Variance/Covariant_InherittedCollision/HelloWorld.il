// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly HelloWorld
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {E623A36D-5AFB-422F-A603-902960F86566}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x016A0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       129 (0x81)
    .maxstack  2
    .locals init (string V_0,
             string V_1,
             string V_2,
             string V_3,
             int32 V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  call       string HelloWorld::StepAB_1()
    IL_0006:  stloc.0
    IL_0007:  call       string HelloWorld::StepAB_2()
    IL_000c:  stloc.1
    IL_000d:  call       string HelloWorld::StepBA_1()
    IL_0012:  stloc.2
    IL_0013:  call       string HelloWorld::StepBA_2()
    IL_0018:  stloc.3
    IL_0019:  ldloc.0
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001f:  nop
    IL_0020:  ldloc.1
    IL_0021:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldloc.2
    IL_0028:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002d:  nop
    IL_002e:  ldloc.3
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  nop
    IL_0035:  ldloc.0
    IL_0036:  ldstr      "XIAB_1.Print(A='C_1')"
    IL_003b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0040:  brfalse.s  IL_006c

    IL_0042:  ldloc.1
    IL_0043:  ldstr      "XIAB_2.Print(B='C_2')"
    IL_0048:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_004d:  brfalse.s  IL_006c

    IL_004f:  ldloc.2
    IL_0050:  ldstr      "XIBA_1.Print(B='C_11')"
    IL_0055:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005a:  brfalse.s  IL_006c

    IL_005c:  ldloc.3
    IL_005d:  ldstr      "XIBA_2.Print(A='C_12')"
    IL_0062:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0067:  ldc.i4.0
    IL_0068:  ceq
    IL_006a:  br.s       IL_006d

    IL_006c:  ldc.i4.1
    IL_006d:  stloc.s    V_5
    IL_006f:  ldloc.s    V_5
    IL_0071:  brtrue.s   IL_0079

    IL_0073:  ldc.i4.s   100
    IL_0075:  stloc.s    V_4
    IL_0077:  br.s       IL_007e

    IL_0079:  ldc.i4.m1
    IL_007a:  stloc.s    V_4
    IL_007c:  br.s       IL_007e

    IL_007e:  ldloc.s    V_4
    IL_0080:  ret
  } // end of method HelloWorld::Main

  .method public hidebysig static string 
          StepAB_1() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIAB_1::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  newobj     instance void C::.ctor(int32)
    IL_0013:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method HelloWorld::StepAB_1

  .method public hidebysig static string 
          StepAB_2() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIAB_2::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.2
    IL_000e:  newobj     instance void C::.ctor(int32)
    IL_0013:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method HelloWorld::StepAB_2

  .method public hidebysig static string 
          StepBA_1() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIBA_1::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   11
    IL_000f:  newobj     instance void C::.ctor(int32)
    IL_0014:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0019:  stloc.1
    IL_001a:  br.s       IL_001c

    IL_001c:  ldloc.1
    IL_001d:  ret
  } // end of method HelloWorld::StepBA_1

  .method public hidebysig static string 
          StepBA_2() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIBA_2::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   12
    IL_000f:  newobj     instance void C::.ctor(int32)
    IL_0014:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0019:  stloc.1
    IL_001a:  br.s       IL_001c

    IL_001c:  ldloc.1
    IL_001d:  ret
  } // end of method HelloWorld::StepBA_2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method HelloWorld::.ctor

} // end of class HelloWorld

.class interface private abstract auto ansi I`1<-T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method I`1::Print

} // end of class I`1

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .field family string 'value'
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldstr      "A_"
    IL_000e:  ldarg.1
    IL_000f:  box        [mscorlib]System.Int32
    IL_0014:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0019:  stfld      string A::'value'
    IL_001e:  nop
    IL_001f:  ret
  } // end of method A::.ctor

  .method public hidebysig specialname instance string 
          get_Value() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      string A::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method A::get_Value

  .property instance string Value()
  {
    .get instance string A::get_Value()
  } // end of property A::Value
} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void A::.ctor(int32)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "B_"
    IL_000f:  ldarg.1
    IL_0010:  box        [mscorlib]System.Int32
    IL_0015:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_001a:  stfld      string A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method B::.ctor

} // end of class B

.class private auto ansi beforefieldinit C
       extends B
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void B::.ctor(int32)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "C_"
    IL_000f:  ldarg.1
    IL_0010:  box        [mscorlib]System.Int32
    IL_0015:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_001a:  stfld      string A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method C::.ctor

} // end of class C

.class private auto ansi beforefieldinit XIAB_1
       extends [mscorlib]System.Object
       implements class I`1<class A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XIAB_1.Print(A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XIAB_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XIAB_1::.ctor

} // end of class XIAB_1

.class private auto ansi beforefieldinit XIAB_2
       extends XIAB_1
       implements class I`1<class B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XIAB_2.Print(B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XIAB_2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void XIAB_1::.ctor()
    IL_0006:  ret
  } // end of method XIAB_2::.ctor

} // end of class XIAB_2

.class private auto ansi beforefieldinit XIBA_1
       extends [mscorlib]System.Object
       implements class I`1<class B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XIBA_1.Print(B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XIBA_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XIBA_1::.ctor

} // end of class XIBA_1

.class private auto ansi beforefieldinit XIBA_2
       extends XIBA_1
       implements class I`1<class A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XIBA_2.Print(A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XIBA_2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void XIBA_1::.ctor()
    IL_0006:  ret
  } // end of method XIBA_2::.ctor

} // end of class XIBA_2


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
