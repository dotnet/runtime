// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ASSEMBLY_NAME
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


.namespace Test_delegate {
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
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base::.ctor

} // end of class Test.Base

.class public auto ansi beforefieldinit Child
       extends Test_delegate.Base
{
  .method public hidebysig virtual final 
          instance string  AbstractFinal() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractFinal

  .method public hidebysig virtual instance string 
          AbstractOverrideFinal() cil managed
  {
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
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.AbstractOverrideNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::AbstractOverrideNil

  .method public hidebysig virtual final 
          instance string  VirtualFinal() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualFinal

  .method public hidebysig virtual instance string 
          VirtualOverrideOverride() cil managed
  {
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
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Child.VirtualOverrideNil"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method Child::VirtualOverrideNil

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_delegate.Base::.ctor()
    IL_0006:  ret
  } // end of method Child::.ctor

} // end of class Test.Child

.class public auto ansi beforefieldinit GrandChild
       extends Test_delegate.Child
{
  .method public hidebysig virtual final 
          instance string  AbstractOverrideFinal() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::AbstractOverrideFinal

  .method public hidebysig virtual instance string 
          AbstractOverrideOverride() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::AbstractOverrideOverride

  .method public hidebysig virtual final 
          instance string  VirtualNilFinal() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualNilFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualNilFinal

  .method public hidebysig virtual final 
          instance string  VirtualOverrideFinal() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualOverrideFinal

  .method public hidebysig virtual instance string 
          VirtualOverrideOverride() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualOverrideOverride

  .method public hidebysig virtual instance string 
          VirtualNilOverride() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GrandChild.VirtualNilOverride"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method GrandChild::VirtualNilOverride

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_delegate.Child::.ctor()
    IL_0006:  ret
  } // end of method GrandChild::.ctor

} // end of class Test.GrandChild

.class public auto ansi sealed beforefieldinit SealedGrandChild
       extends Test_delegate.GrandChild
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_delegate.GrandChild::.ctor()
    IL_0006:  ret
  } // end of method SealedGrandChild::.ctor

} // end of class Test.SealedGrandChild

.class public auto ansi sealed TestMethod
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method TestMethod::.ctor

  .method public hidebysig newslot virtual 
          instance string  Invoke() runtime managed
  {
  } // end of method TestMethod::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method TestMethod::BeginInvoke

  .method public hidebysig newslot virtual 
          instance string  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method TestMethod::EndInvoke

} // end of class Test.TestMethod

.class public abstract auto ansi sealed beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  CallDelegateFromSealedGrandChild() cil managed
  {
    .maxstack  4
    .locals init (class Test_delegate.SealedGrandChild V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void Test_delegate.SealedGrandChild::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldstr      "Child.AbstractFinal"
    IL_000c:  ldloc.0
    IL_000e:  ldftn  instance string Test_delegate.Child::AbstractFinal()
    IL_0014:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0019:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_001e:  nop
    IL_001f:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0024:  ldloc.0
    IL_0026:  ldftn  instance string Test_delegate.GrandChild::AbstractOverrideFinal()
    IL_002c:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0031:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_0036:  nop
    IL_0037:  ldstr      "Child.AbstractOverrideNil"
    IL_003c:  ldloc.0
    IL_003e:  ldftn  instance string Test_delegate.Child::AbstractOverrideNil()
    IL_0044:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0049:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_004e:  nop
    IL_004f:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0054:  ldloc.0
    IL_0056:  ldftn  instance string Test_delegate.GrandChild::AbstractOverrideOverride()
    IL_005c:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0061:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_0066:  nop
    IL_0067:  ldstr      "Child.VirtualFinal"
    IL_006c:  ldloc.0
    IL_006e:  ldftn  instance string Test_delegate.Child::VirtualFinal()
    IL_0074:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0079:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_007e:  nop
    IL_007f:  ldstr      "GrandChild.VirtualNilFinal"
    IL_0084:  ldloc.0
    IL_0086:  ldftn  instance string Test_delegate.GrandChild::VirtualNilFinal()
    IL_008c:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0091:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_0096:  nop
    IL_0097:  ldstr      "Base.VirtualNilNil"
    IL_009c:  ldloc.0
    IL_009e:  ldftn  instance string Test_delegate.Base::VirtualNilNil()
    IL_00a4:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_00a9:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_00ae:  nop
    IL_00af:  ldstr      "GrandChild.VirtualNilOverride"
    IL_00b4:  ldloc.0
    IL_00b6:  ldftn  instance string Test_delegate.GrandChild::VirtualNilOverride()
    IL_00bc:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_00c1:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_00c6:  nop
    IL_00c7:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_00cc:  ldloc.0
    IL_00ce:  ldftn  instance string Test_delegate.GrandChild::VirtualOverrideFinal()
    IL_00d4:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_00d9:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_00de:  nop
    IL_00df:  ldstr      "Child.VirtualOverrideNil"
    IL_00e4:  ldloc.0
    IL_00e6:  ldftn  instance string Test_delegate.Child::VirtualOverrideNil()
    IL_00ec:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_00f1:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_00f6:  nop
    IL_00f7:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_00fc:  ldloc.0
    IL_00fe:  ldftn  instance string Test_delegate.GrandChild::VirtualOverrideOverride()
    IL_0104:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0109:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_010e:  nop
    IL_010f:  ret
  } // end of method Program::CallDelegateFromSealedGrandChild

  .method public hidebysig static void  CallDelegateFromGrandChild() cil managed
  {
    .maxstack  4
    .locals init (class Test_delegate.GrandChild V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void Test_delegate.GrandChild::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldstr      "Child.AbstractFinal"
    IL_000c:  ldloc.0
    IL_000e:  ldftn  instance string Test_delegate.Child::AbstractFinal()
    IL_0014:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0019:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_001e:  nop
    IL_001f:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0024:  ldloc.0
    IL_0026:  ldftn  instance string Test_delegate.GrandChild::AbstractOverrideFinal()
    IL_002c:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0031:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_0036:  nop
    IL_0037:  ldstr      "Child.VirtualFinal"
    IL_003c:  ldloc.0
    IL_003e:  ldftn  instance string Test_delegate.Child::VirtualFinal()
    IL_0044:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0049:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_004e:  nop
    IL_004f:  ldstr      "GrandChild.VirtualNilFinal"
    IL_0054:  ldloc.0
    IL_0056:  ldftn  instance string Test_delegate.GrandChild::VirtualNilFinal()
    IL_005c:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0061:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_0066:  nop
    IL_0067:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_006c:  ldloc.0
    IL_006e:  ldftn  instance string Test_delegate.GrandChild::VirtualOverrideFinal()
    IL_0074:  newobj     instance void Test_delegate.TestMethod::.ctor(object,
                                                              native int)
    IL_0079:  call       void Test_delegate.Assert::AreEqual(string,
                                                    class Test_delegate.TestMethod)
    IL_007e:  nop
    IL_007f:  ret
  } // end of method Program::CallDelegateFromGrandChild

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  call       void Test_delegate.Program::CallDelegateFromGrandChild()
      IL_0007:  nop
      IL_0008:  call       void Test_delegate.Program::CallDelegateFromSealedGrandChild()
      IL_000d:  nop
      IL_000e:  ldstr      "Test SUCCESS"
      IL_0013:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0018:  nop
      IL_0019:  ldc.i4.s   100
      IL_001b:  stloc.1
      IL_001c:  leave.s    IL_0037

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001e:  stloc.0
      IL_001f:  nop
      IL_0020:  ldloc.0
      IL_0021:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0026:  nop
      IL_0027:  ldstr      "Test FAILED"
      IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0031:  nop
      IL_0032:  ldc.i4.s   101
      IL_0034:  stloc.1
      IL_0035:  leave.s    IL_0037

    }  // end handler
    IL_0037:  nop
    IL_0038:  ldloc.1
    IL_0039:  ret
  } // end of method Program::Main

} // end of class Test.Program

.class public abstract auto ansi sealed beforefieldinit Assert
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  AreEqual(string left,
                                                 class Test_delegate.TestMethod right) cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string Test_delegate.TestMethod::Invoke()
    IL_0008:  call       void Test_delegate.Assert::AreEqual(string,
                                                    string)
    IL_000d:  nop
    IL_000e:  ret
  } // end of method Assert::AreEqual

  .method public hidebysig static void  AreEqual(string left,
                                                 string right) cil managed
  {
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
    IL_004e:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string)
    IL_0053:  throw

    IL_0054:  ret
  } // end of method Assert::AreEqual

} // end of class Test.Assert
}
