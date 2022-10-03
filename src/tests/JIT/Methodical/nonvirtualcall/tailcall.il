// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
// Metadata version: v2.0.50509
.assembly extern mscorlib { auto }

.assembly tailcall_nonvirtualcall
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {EC3860F1-6469-489C-86D2-A81D88AE196E}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000000001AB30000


.namespace Test_tailcall {
// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance string  AbstractFinal() cil managed
  {
  } // end of method Base::AbstractFinal

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideFinal() cil managed
  {
  } // end of method Base::AbstractOverrideFinal

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideOverride() cil managed
  {
  } // end of method Base::AbstractOverrideOverride

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideNil() cil managed
  {
  } // end of method Base::AbstractOverrideNil

  .method public hidebysig newslot virtual 
          instance string  VirtualFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualFinal

  .method public hidebysig newslot virtual 
          instance string  VirtualNilFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualNilFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualNilFinal

  .method public hidebysig newslot virtual 
          instance string  VirtualOverrideFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualOverrideFinal

  .method public hidebysig newslot virtual 
          instance string  VirtualNilOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualNilOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualNilOverride

  .method public hidebysig newslot virtual 
          instance string  VirtualNilNil() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualNilNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualNilNil

  .method public hidebysig newslot virtual 
          instance string  VirtualOverrideOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualOverrideOverride

  .method public hidebysig newslot virtual 
          instance string  VirtualOverrideNil() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Base.VirtualOverrideNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Base::VirtualOverrideNil

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Test.Base

.class public auto ansi beforefieldinit Child
       extends Test_tailcall.Base
{
  .method public hidebysig virtual final 
          instance string  AbstractFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractFinal

  .method public hidebysig instance string 
          CallAbstractFinal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.Child::AbstractFinal()
    IL_000b:  ret
  } // end of method Child::CallAbstractFinal

  .method public hidebysig virtual instance string 
          AbstractOverrideFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractOverrideFinal

  .method public hidebysig virtual instance string 
          AbstractOverrideOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractOverrideOverride

  .method public hidebysig virtual instance string 
          AbstractOverrideNil() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractOverrideNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractOverrideNil

  .method public hidebysig instance string 
          CallAbstractOverrideNil() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.Child::AbstractOverrideNil()
    IL_000b:  ret
  } // end of method Child::CallAbstractOverrideNil

  .method public hidebysig virtual final 
          instance string  VirtualFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualFinal

  .method public hidebysig instance string 
          CallVirtualFinal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.Child::VirtualFinal()
   IL_000b:  ret
  } // end of method Child::tail. callualFinal

  .method public hidebysig virtual instance string 
          VirtualOverrideFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualOverrideFinal

  .method public hidebysig virtual instance string 
          VirtualOverrideOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualOverrideOverride

  .method public hidebysig virtual instance string 
          VirtualOverrideNil() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualOverrideNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualOverrideNil

  .method public hidebysig instance string 
          CallVirtualOverrideNil() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.Child::VirtualOverrideNil()
    IL_000b:  ret
  } // end of method Child::tail. callualOverrideNil

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_tailcall.Base::.ctor()
    IL_0006:  ret
  } // end of method Child::.ctor

} // end of class Test.Child

.class public auto ansi beforefieldinit GrandChild
       extends Test_tailcall.Child
{
  .method public hidebysig virtual final 
          instance string  AbstractOverrideFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::AbstractOverrideFinal

  .method public hidebysig instance string 
          CallAbstractOverrideFinal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::AbstractOverrideFinal()
    IL_000b:  ret
  } // end of method GrandChild::CallAbstractOverrideFinal

  .method public hidebysig virtual instance string 
          AbstractOverrideOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::AbstractOverrideOverride

  .method public hidebysig instance string 
          CallAbstractOverrideOverride() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::AbstractOverrideOverride()
   IL_000b:  ret
  } // end of method GrandChild::CallAbstractOverrideOverride

  .method public hidebysig virtual final 
          instance string  VirtualNilFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualNilFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualNilFinal

  .method public hidebysig instance string 
          CallVirtualNilFinal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::VirtualNilFinal()
   IL_000b:  ret
  } // end of method GrandChild::tail. callualNilFinal

  .method public hidebysig virtual final 
          instance string  VirtualOverrideFinal() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualOverrideFinal

  .method public hidebysig instance string 
          CallVirtualOverrideFinal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::VirtualOverrideFinal()
    IL_000b:  ret
  } // end of method GrandChild::tail. callualOverrideFinal

  .method public hidebysig virtual instance string 
          VirtualOverrideOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualOverrideOverride

  .method public hidebysig instance string 
          CallVirtualOverrideOverride() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::VirtualOverrideOverride()
   IL_000b:  ret
  } // end of method GrandChild::tail. callualOverrideOverride

  .method public hidebysig virtual instance string 
          VirtualNilOverride() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualNilOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualNilOverride

  .method public hidebysig instance string 
          CallVirtualNilOverride() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  tail. call   instance string Test_tailcall.GrandChild::VirtualNilOverride()
    IL_000b:  ret
  } // end of method GrandChild::tail. callualNilOverride

  .method public hidebysig instance void 
          TestGrandChild() cil managed
  {
    // Code size       149 (0x95)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldstr      "Call from inside GrandChild"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldstr      "Child.AbstractFinal"
    IL_0011:  ldarg.0
    IL_0012:  call       instance string Test_tailcall.Child::CallAbstractFinal()
    IL_0017:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_001c:  nop
    IL_001d:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0022:  ldarg.0
    IL_0023:  call       instance string Test_tailcall.GrandChild::CallAbstractOverrideFinal()
    IL_0028:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_002d:  nop
    IL_002e:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0033:  ldarg.0
    IL_0034:  call       instance string Test_tailcall.GrandChild::CallAbstractOverrideOverride()
    IL_0039:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_003e:  nop
    IL_003f:  ldstr      "Child.AbstractOverrideNil"
    IL_0044:  ldarg.0
    IL_0045:  call       instance string Test_tailcall.Child::CallAbstractOverrideNil()
    IL_004a:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_004f:  nop
    IL_0050:  ldstr      "Child.VirtualFinal"
    IL_0055:  ldarg.0
    IL_0056:  call       instance string Test_tailcall.Child::CallVirtualFinal()
    IL_005b:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_0060:  nop
    IL_0061:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_0066:  ldarg.0
    IL_0067:  call       instance string Test_tailcall.GrandChild::CallVirtualOverrideFinal()
    IL_006c:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_0071:  nop
    IL_0072:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_0077:  ldarg.0
    IL_0078:  call       instance string Test_tailcall.GrandChild::CallVirtualOverrideOverride()
    IL_007d:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_0082:  nop
    IL_0083:  ldstr      "Child.VirtualOverrideNil"
    IL_0088:  ldarg.0
    IL_0089:  call       instance string Test_tailcall.Child::CallVirtualOverrideNil()
    IL_008e:  call       void Test_tailcall.Assert::AreEqual(string,
                                                    string)
    IL_0093:  nop
    IL_0094:  ret
  } // end of method GrandChild::TestGrandChild

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_tailcall.Child::.ctor()
    IL_0006:  ret
  } // end of method GrandChild::.ctor

} // end of class Test.GrandChild

.class public abstract auto ansi sealed beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  CallFromInsideGrandChild() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  1
    .locals init (class Test_tailcall.GrandChild V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void Test_tailcall.GrandChild::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  tail. call   instance void Test_tailcall.GrandChild::TestGrandChild()
    IL_000e:  ret
  } // end of method Program::CallFromInsideGrandChild

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       52 (0x34)
    .maxstack  1
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  call       void Test_tailcall.Program::CallFromInsideGrandChild()
      IL_0007:  nop
      IL_0008:  ldstr      "Test SUCCESS"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  nop
      IL_0013:  ldc.i4.s   100
      IL_0015:  stloc.1
      IL_0016:  leave.s    IL_0031

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  nop
      IL_001a:  ldloc.0
      IL_001b:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0020:  nop
      IL_0021:  ldstr      "Test FAILED"
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002b:  nop
      IL_002c:  ldc.i4.s   101
      IL_002e:  stloc.1
      IL_002f:  leave.s    IL_0031

    }  // end handler
    IL_0031:  nop
    IL_0032:  ldloc.1
    IL_0033:  ret
  } // end of method Program::Main

} // end of class Test.Program

.class public abstract auto ansi sealed beforefieldinit Assert
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  AreEqual(string left,
                                                 string right) cil managed
  {
    // Code size       85 (0x55)
    .maxstack  3
    .locals init (string V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0007:  ldc.i4.0
    IL_0008:  ceq
    IL_000a:  stloc.1
    IL_000b:  ldloc.1
    IL_000c:  brtrue.s   IL_0019

    IL_000e:  ldstr      "left"
    IL_0013:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0018:  throw

    IL_0019:  ldarg.1
    IL_001a:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_001f:  ldc.i4.0
    IL_0020:  ceq
    IL_0022:  stloc.1
    IL_0023:  ldloc.1
    IL_0024:  brtrue.s   IL_0031

    IL_0026:  ldstr      "right"
    IL_002b:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0030:  throw

    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0038:  ldc.i4.0
    IL_0039:  ceq
    IL_003b:  stloc.1
    IL_003c:  ldloc.1
    IL_003d:  brtrue.s   IL_0054

    IL_003f:  nop
    IL_0040:  ldstr      "[[{0}]] != [[{1}]]"
    IL_0045:  ldarg.0
    IL_0046:  ldarg.1
    IL_0047:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_004c:  stloc.0
    IL_004d:  ldloc.0
    IL_004e:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0053:  throw

    IL_0054:  ret
  } // end of method Assert::AreEqual

} // end of class Test.Assert
}
