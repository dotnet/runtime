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
// MVID: {8F0BCC1D-8425-48B7-A9BE-1043DA25AD87}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01B90000


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
    IL_0001:  call       string HelloWorld::StepBC_1()
    IL_0006:  stloc.0
    IL_0007:  call       string HelloWorld::StepBC_2()
    IL_000c:  stloc.1
    IL_000d:  call       string HelloWorld::StepCB_1()
    IL_0012:  stloc.2
    IL_0013:  call       string HelloWorld::StepCB_2()
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
    IL_0036:  ldstr      "B_1"
    IL_003b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0040:  brfalse.s  IL_006c

    IL_0042:  ldloc.1
    IL_0043:  ldstr      "C_2"
    IL_0048:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_004d:  brfalse.s  IL_006c

    IL_004f:  ldloc.2
    IL_0050:  ldstr      "C_11"
    IL_0055:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005a:  brfalse.s  IL_006c

    IL_005c:  ldloc.3
    IL_005d:  ldstr      "B_12"
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
          StepBC_1() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (class I`1<class A> V_0,
             class A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIBC_1::.ctor()
    IL_0006:  castclass  class I`1<class A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  callvirt   instance !0 class I`1<class A>::Print(int32)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  callvirt   instance string A::get_Value()
    IL_001a:  stloc.2
    IL_001b:  br.s       IL_001d

    IL_001d:  ldloc.2
    IL_001e:  ret
  } // end of method HelloWorld::StepBC_1

  .method public hidebysig static string 
          StepBC_2() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (class I`1<class A> V_0,
             class A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void XIBC_2::.ctor()
    IL_0006:  castclass  class I`1<class A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.2
    IL_000e:  callvirt   instance !0 class I`1<class A>::Print(int32)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  callvirt   instance string A::get_Value()
    IL_001a:  stloc.2
    IL_001b:  br.s       IL_001d

    IL_001d:  ldloc.2
    IL_001e:  ret
  } // end of method HelloWorld::StepBC_2

  .method public hidebysig static string 
          StepCB_1() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  2
    .locals init (class I`1<class A> V_0,
             class A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void XICB_1::.ctor()
    IL_0006:  castclass  class I`1<class A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   11
    IL_000f:  callvirt   instance !0 class I`1<class A>::Print(int32)
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  callvirt   instance string A::get_Value()
    IL_001b:  stloc.2
    IL_001c:  br.s       IL_001e

    IL_001e:  ldloc.2
    IL_001f:  ret
  } // end of method HelloWorld::StepCB_1

  .method public hidebysig static string 
          StepCB_2() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  2
    .locals init (class I`1<class A> V_0,
             class A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void XICB_2::.ctor()
    IL_0006:  castclass  class I`1<class A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.s   12
    IL_000f:  callvirt   instance !0 class I`1<class A>::Print(int32)
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  callvirt   instance string A::get_Value()
    IL_001b:  stloc.2
    IL_001c:  br.s       IL_001e

    IL_001e:  ldloc.2
    IL_001f:  ret
  } // end of method HelloWorld::StepCB_2

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

//CHANGE: add variance to I:
//.class interface private abstract auto ansi I`1<T>
.class interface private abstract auto ansi I`1<+T>
{
  .method public hidebysig newslot abstract virtual 
          instance !T  Print(int32 index) cil managed
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
    .locals init ([0] string CS$1$0000)
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

.class private auto ansi beforefieldinit XIBC_1
       extends [mscorlib]System.Object
       implements class I`1<class B>
{
  .method public hidebysig newslot virtual final 
          instance class B  Print(int32 index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init ([0] class B b,
             [1] class B CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void B::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    B XIBC_1.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_000e:  box        [mscorlib]System.Int32
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string A::get_Value()
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method XIBC_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XIBC_1::.ctor

} // end of class XIBC_1

.class private auto ansi beforefieldinit XIBC_2
       extends XIBC_1
       implements class I`1<class C>
{
  .method public hidebysig newslot virtual final 
          instance class C  Print(int32 index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init ([0] class C c,
             [1] class C CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void C::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    C XIBC_2.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_000e:  box        [mscorlib]System.Int32
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string A::get_Value()
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method XIBC_2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void XIBC_1::.ctor()
    IL_0006:  ret
  } // end of method XIBC_2::.ctor

} // end of class XIBC_2

.class private auto ansi beforefieldinit XICB_1
       extends [mscorlib]System.Object
       implements class I`1<class C>
{
  .method public hidebysig newslot virtual final 
          instance class C  Print(int32 index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init ([0] class C c,
             [1] class C CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void C::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    C XICB_1.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_000e:  box        [mscorlib]System.Int32
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string A::get_Value()
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method XICB_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XICB_1::.ctor

} // end of class XICB_1

.class private auto ansi beforefieldinit XICB_2
       extends XICB_1
       implements class I`1<class B>
{
  .method public hidebysig newslot virtual final 
          instance class B  Print(int32 index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init ([0] class B b,
             [1] class B CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void B::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    B XICB_2.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_000e:  box        [mscorlib]System.Int32
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string A::get_Value()
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method XICB_2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void XICB_1::.ctor()
    IL_0006:  ret
  } // end of method XICB_2::.ctor

} // end of class XICB_2


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld_out.res
