// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly Ambiguous
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {7CFDE9D8-A5B3-4D83-86E7-E48FE93FEB09}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01B60000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Foo() cil managed
  {
  } // end of method I`1::Foo

} // end of class I`1

.class private auto ansi beforefieldinit A`1<U>
       extends [mscorlib]System.Object
       implements class I`1<!U>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<U>.Foo'() cil managed
  {
    .override  method instance string class I`1<!U>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A::Foo"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A`1::'I<U>.Foo'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`1::.ctor

} // end of class A`1

.class private auto ansi beforefieldinit B`2<V,W>
       extends class A`1<!V>
       implements class I`1<!W>
{
  .method private hidebysig newslot virtual final instance string  'I<W>.Foo'() cil managed
  {
    .override  method instance string class I`1<!W>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B::Foo1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::'I<W>.Foo'

  .method private hidebysig newslot virtual final instance string  'I<V>.Foo'() cil managed
  {
    .override  method instance string class I`1<!V>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B::Foo2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::'I<W>.Foo'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`1<!V>::.ctor()
    IL_0006:  ret
  } // end of method B`2::.ctor

} // end of class B`2

.class public auto ansi beforefieldinit Test_Ambiguous
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       96 (0x60)
    .maxstack  2
    .locals init (class B`2<int32,int32> V_0,
             class B`2<string,string> V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void class B`2<int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance string class I`1<int32>::Foo()
    IL_000d:  ldstr      "B::Foo2"
    IL_0012:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0017:  ldc.i4.0
    IL_0018:  ceq
    IL_001a:  stloc.3
    IL_001b:  ldloc.3
    IL_001c:  brtrue.s   IL_0020

    IL_001e:  br.s       IL_004f

    IL_0020:  newobj     instance void class B`2<string,string>::.ctor()
    IL_0025:  stloc.1
    IL_0026:  ldloc.1
    IL_0027:  callvirt   instance string class I`1<string>::Foo()
    IL_002c:  ldstr      "B::Foo2"
    IL_0031:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0036:  ldc.i4.0
    IL_0037:  ceq
    IL_0039:  stloc.3
    IL_003a:  ldloc.3
    IL_003b:  brtrue.s   IL_003f

    IL_003d:  br.s       IL_004f

    IL_003f:  ldstr      "Pass"
    IL_0044:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0049:  nop
    IL_004a:  ldc.i4.s   100
    IL_004c:  stloc.2
    IL_004d:  br.s       IL_005e

    IL_004f:  ldstr      "Failed!"
    IL_0054:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0059:  nop
    IL_005a:  ldc.i4.m1
    IL_005b:  stloc.2
    IL_005c:  br.s       IL_005e

    IL_005e:  ldloc.2
    IL_005f:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Ambiguous.res
