// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.50509
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
// MVID: {5D0110E9-A5F8-484A-97F5-D5C79C5A9A77}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000000001AB30000


.namespace Test_generics2 {
// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi beforefieldinit Base`1<K>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance string  AbstractFinal() cil managed
  {
  } // end of method Base`1::AbstractFinal

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideFinal() cil managed
  {
  } // end of method Base`1::AbstractOverrideFinal

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideOverride() cil managed
  {
  } // end of method Base`1::AbstractOverrideOverride

  .method public hidebysig newslot abstract virtual 
          instance string  AbstractOverrideNil() cil managed
  {
  } // end of method Base`1::AbstractOverrideNil

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
  } // end of method Base`1::VirtualFinal

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
  } // end of method Base`1::VirtualNilFinal

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
  } // end of method Base`1::VirtualOverrideFinal

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
  } // end of method Base`1::VirtualNilOverride

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
  } // end of method Base`1::VirtualNilNil

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
  } // end of method Base`1::VirtualOverrideOverride

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
  } // end of method Base`1::VirtualOverrideNil

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Base`1::.ctor

} // end of class Test.Base`1

.class public auto ansi beforefieldinit Child`1<K>
       extends class Test_generics2.Base`1<!K>
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
  } // end of method Child`1::AbstractFinal

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
  } // end of method Child`1::AbstractOverrideFinal

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
  } // end of method Child`1::AbstractOverrideOverride

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
  } // end of method Child`1::AbstractOverrideNil

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
  } // end of method Child`1::VirtualFinal

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
  } // end of method Child`1::VirtualOverrideFinal

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
  } // end of method Child`1::VirtualOverrideOverride

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
  } // end of method Child`1::VirtualOverrideNil

  .method public hidebysig instance void 
          TestChild() cil managed
  {
    // Code size       217 (0xd9)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldstr      "Call from inside Child"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldstr      "Child.AbstractFinal"
    IL_0011:  ldarg.0
    IL_0012:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractFinal()
    IL_0017:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_001c:  nop
    IL_001d:  ldstr      "Child.AbstractOverrideFinal"
    IL_0022:  ldarg.0
    IL_0023:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractOverrideFinal()
    IL_0028:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_002d:  nop
    IL_002e:  ldstr      "Child.AbstractOverrideOverride"
    IL_0033:  ldarg.0
    IL_0034:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractOverrideOverride()
    IL_0039:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_003e:  nop
    IL_003f:  ldstr      "Child.AbstractOverrideNil"
    IL_0044:  ldarg.0
    IL_0045:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractOverrideNil()
    IL_004a:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_004f:  nop
    IL_0050:  ldstr      "Base.VirtualFinal"
    IL_0055:  ldarg.0
    IL_0056:  call       instance string class Test_generics2.Base`1<!K>::VirtualFinal()
    IL_005b:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0060:  nop
    IL_0061:  ldstr      "Child.VirtualFinal"
    IL_0066:  ldarg.0
    IL_0067:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualFinal()
    IL_006c:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0071:  nop
    IL_0072:  ldstr      "Base.VirtualOverrideFinal"
    IL_0077:  ldarg.0
    IL_0078:  call       instance string class Test_generics2.Base`1<!K>::VirtualOverrideFinal()
    IL_007d:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0082:  nop
    IL_0083:  ldstr      "Child.VirtualOverrideFinal"
    IL_0088:  ldarg.0
    IL_0089:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualOverrideFinal()
    IL_008e:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0093:  nop
    IL_0094:  ldstr      "Base.VirtualOverrideOverride"
    IL_0099:  ldarg.0
    IL_009a:  call       instance string class Test_generics2.Base`1<!K>::VirtualOverrideOverride()
    IL_009f:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00a4:  nop
    IL_00a5:  ldstr      "Child.VirtualOverrideOverride"
    IL_00aa:  ldarg.0
    IL_00ab:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualOverrideOverride()
    IL_00b0:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00b5:  nop
    IL_00b6:  ldstr      "Base.VirtualOverrideNil"
    IL_00bb:  ldarg.0
    IL_00bc:  call       instance string class Test_generics2.Base`1<!K>::VirtualOverrideNil()
    IL_00c1:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00c6:  nop
    IL_00c7:  ldstr      "Child.VirtualOverrideNil"
    IL_00cc:  ldarg.0
    IL_00cd:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualOverrideNil()
    IL_00d2:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00d7:  nop
    IL_00d8:  ret
  } // end of method Child`1::TestChild

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class Test_generics2.Base`1<!K>::.ctor()
    IL_0006:  ret
  } // end of method Child`1::.ctor

} // end of class Test.Child`1

.class public auto ansi beforefieldinit GrandChild`1<K>
       extends class Test_generics2.Child`1<!K>
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
  } // end of method GrandChild`1::AbstractOverrideFinal

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
  } // end of method GrandChild`1::AbstractOverrideOverride

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
  } // end of method GrandChild`1::VirtualNilFinal

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
  } // end of method GrandChild`1::VirtualOverrideFinal

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
  } // end of method GrandChild`1::VirtualOverrideOverride

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
  } // end of method GrandChild`1::VirtualNilOverride

  .method public hidebysig instance void 
          TestGrandChild() cil managed
  {
    // Code size       268 (0x10c)
    .maxstack  2
    IL_0000:  nop
    IL_0001:  ldstr      "Call from inside GrandChild"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldstr      "Child.AbstractFinal"
    IL_0011:  ldarg.0
    IL_0012:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractFinal()
    IL_0017:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_001c:  nop
    IL_001d:  ldstr      "Child.AbstractOverrideFinal"
    IL_0022:  ldarg.0
    IL_0023:  call       instance string class Test_generics2.Child`1<!K>::AbstractOverrideFinal()
    IL_0028:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_002d:  nop
    IL_002e:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0033:  ldarg.0
    IL_0034:  callvirt   instance string class Test_generics2.GrandChild`1<!K>::AbstractOverrideFinal()
    IL_0039:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_003e:  nop
    IL_003f:  ldstr      "Child.AbstractOverrideOverride"
    IL_0044:  ldarg.0
    IL_0045:  call       instance string class Test_generics2.Child`1<!K>::AbstractOverrideOverride()
    IL_004a:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_004f:  nop
    IL_0050:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0055:  ldarg.0
    IL_0056:  callvirt   instance string class Test_generics2.GrandChild`1<!K>::AbstractOverrideOverride()
    IL_005b:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0060:  nop
    IL_0061:  ldstr      "Child.AbstractOverrideNil"
    IL_0066:  ldarg.0
    IL_0067:  call       instance string class Test_generics2.Child`1<!K>::AbstractOverrideNil()
    IL_006c:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0071:  nop
    IL_0072:  ldstr      "Child.AbstractOverrideNil"
    IL_0077:  ldarg.0
    IL_0078:  callvirt   instance string class Test_generics2.Child`1<!K>::AbstractOverrideNil()
    IL_007d:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0082:  nop
    IL_0083:  ldstr      "Child.VirtualFinal"
    IL_0088:  ldarg.0
    IL_0089:  call       instance string class Test_generics2.Child`1<!K>::VirtualFinal()
    IL_008e:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0093:  nop
    IL_0094:  ldstr      "Child.VirtualFinal"
    IL_0099:  ldarg.0
    IL_009a:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualFinal()
    IL_009f:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00a4:  nop
    IL_00a5:  ldstr      "Child.VirtualOverrideFinal"
    IL_00aa:  ldarg.0
    IL_00ab:  call       instance string class Test_generics2.Child`1<!K>::VirtualOverrideFinal()
    IL_00b0:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00b5:  nop
    IL_00b6:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_00bb:  ldarg.0
    IL_00bc:  callvirt   instance string class Test_generics2.GrandChild`1<!K>::VirtualOverrideFinal()
    IL_00c1:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00c6:  nop
    IL_00c7:  ldstr      "Child.VirtualOverrideOverride"
    IL_00cc:  ldarg.0
    IL_00cd:  call       instance string class Test_generics2.Child`1<!K>::VirtualOverrideOverride()
    IL_00d2:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00d7:  nop
    IL_00d8:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_00dd:  ldarg.0
    IL_00de:  callvirt   instance string class Test_generics2.GrandChild`1<!K>::VirtualOverrideOverride()
    IL_00e3:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00e8:  nop
    IL_00e9:  ldstr      "Child.VirtualOverrideNil"
    IL_00ee:  ldarg.0
    IL_00ef:  call       instance string class Test_generics2.Child`1<!K>::VirtualOverrideNil()
    IL_00f4:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00f9:  nop
    IL_00fa:  ldstr      "Child.VirtualOverrideNil"
    IL_00ff:  ldarg.0
    IL_0100:  callvirt   instance string class Test_generics2.Child`1<!K>::VirtualOverrideNil()
    IL_0105:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_010a:  nop
    IL_010b:  ret
  } // end of method GrandChild`1::TestGrandChild

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class Test_generics2.Child`1<!K>::.ctor()
    IL_0006:  ret
  } // end of method GrandChild`1::.ctor

} // end of class Test.GrandChild`1

.class public auto ansi sealed beforefieldinit SealedGrandChild`1<K>
       extends class Test_generics2.GrandChild`1<!K>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class Test_generics2.GrandChild`1<!K>::.ctor()
    IL_0006:  ret
  } // end of method SealedGrandChild`1::.ctor

} // end of class Test.SealedGrandChild`1

.class public abstract auto ansi sealed beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  CallSealedGrandChild() cil managed
  {
    // Code size       206 (0xce)
    .maxstack  2
    .locals init (class Test_generics2.SealedGrandChild`1<object> V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Call SealedGrandChild from outside"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void class Test_generics2.SealedGrandChild`1<object>::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldstr      "Child.AbstractFinal"
    IL_0017:  ldloc.0
    IL_0018:  callvirt   instance string class Test_generics2.GrandChild`1<object>::AbstractFinal()
    IL_001d:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0022:  nop
    IL_0023:  ldstr      "GrandChild.AbstractOverrideFinal"
    IL_0028:  ldloc.0
    IL_0029:  callvirt   instance string class Test_generics2.GrandChild`1<object>::AbstractOverrideFinal()
    IL_002e:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0033:  nop
    IL_0034:  ldstr      "GrandChild.AbstractOverrideOverride"
    IL_0039:  ldloc.0
    IL_003a:  callvirt   instance string class Test_generics2.GrandChild`1<object>::AbstractOverrideOverride()
    IL_003f:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0044:  nop
    IL_0045:  ldstr      "Child.AbstractOverrideNil"
    IL_004a:  ldloc.0
    IL_004b:  callvirt   instance string class Test_generics2.Child`1<object>::AbstractOverrideNil()
    IL_0050:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0055:  nop
    IL_0056:  ldstr      "Child.VirtualFinal"
    IL_005b:  ldloc.0
    IL_005c:  callvirt   instance string class Test_generics2.Child`1<object>::VirtualFinal()
    IL_0061:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0066:  nop
    IL_0067:  ldstr      "GrandChild.VirtualNilFinal"
    IL_006c:  ldloc.0
    IL_006d:  callvirt   instance string class Test_generics2.GrandChild`1<object>::VirtualNilFinal()
    IL_0072:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0077:  nop
    IL_0078:  ldstr      "GrandChild.VirtualOverrideFinal"
    IL_007d:  ldloc.0
    IL_007e:  callvirt   instance string class Test_generics2.GrandChild`1<object>::VirtualOverrideFinal()
    IL_0083:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0088:  nop
    IL_0089:  ldstr      "GrandChild.VirtualNilOverride"
    IL_008e:  ldloc.0
    IL_008f:  callvirt   instance string class Test_generics2.GrandChild`1<object>::VirtualNilOverride()
    IL_0094:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_0099:  nop
    IL_009a:  ldstr      "Base.VirtualNilNil"
    IL_009f:  ldloc.0
    IL_00a0:  callvirt   instance string class Test_generics2.Base`1<object>::VirtualNilNil()
    IL_00a5:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00aa:  nop
    IL_00ab:  ldstr      "GrandChild.VirtualOverrideOverride"
    IL_00b0:  ldloc.0
    IL_00b1:  callvirt   instance string class Test_generics2.GrandChild`1<object>::VirtualOverrideOverride()
    IL_00b6:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00bb:  nop
    IL_00bc:  ldstr      "Child.VirtualOverrideNil"
    IL_00c1:  ldloc.0
    IL_00c2:  callvirt   instance string class Test_generics2.Child`1<object>::VirtualOverrideNil()
    IL_00c7:  call       void Test_generics2.Assert::AreEqual(string,
                                                    string)
    IL_00cc:  nop
    IL_00cd:  ret
  } // end of method Program::CallSealedGrandChild

  .method public hidebysig static void  CallFromInsideChild() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  1
    .locals init (class Test_generics2.Child`1<object> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class Test_generics2.Child`1<object>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void class Test_generics2.Child`1<object>::TestChild()
    IL_000d:  nop
    IL_000e:  ret
  } // end of method Program::CallFromInsideChild

  .method public hidebysig static void  CallFromInsideGrandChild() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  1
    .locals init (class Test_generics2.GrandChild`1<object> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class Test_generics2.GrandChild`1<object>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void class Test_generics2.GrandChild`1<object>::TestGrandChild()
    IL_000d:  nop
    IL_000e:  ret
  } // end of method Program::CallFromInsideGrandChild

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       64 (0x40)
    .maxstack  1
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  call       void Test_generics2.Program::CallSealedGrandChild()
      IL_0007:  nop
      IL_0008:  call       void Test_generics2.Program::CallFromInsideChild()
      IL_000d:  nop
      IL_000e:  call       void Test_generics2.Program::CallFromInsideGrandChild()
      IL_0013:  nop
      IL_0014:  ldstr      "Test SUCCESS"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001e:  nop
      IL_001f:  ldc.i4.s   100
      IL_0021:  stloc.1
      IL_0022:  leave.s    IL_003d

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0024:  stloc.0
      IL_0025:  nop
      IL_0026:  ldloc.0
      IL_0027:  call       void [System.Console]System.Console::WriteLine(object)
      IL_002c:  nop
      IL_002d:  ldstr      "Test FAILED"
      IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0037:  nop
      IL_0038:  ldc.i4.s   101
      IL_003a:  stloc.1
      IL_003b:  leave.s    IL_003d

    }  // end handler
    IL_003d:  nop
    IL_003e:  ldloc.1
    IL_003f:  ret
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
