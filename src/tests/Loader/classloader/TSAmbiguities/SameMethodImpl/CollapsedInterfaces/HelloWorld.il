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
// MVID: {EE84F77E-91BD-4DE7-9EF8-6E248B067B89}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00310000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       382 (0x17e)
    .maxstack  2
    .locals init (bool V_0,
             class I`1<int32> V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  call       class I`1<int32> HelloWorld::Test_B()
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldc.i4.1
    IL_000b:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0015:  nop
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.1
    IL_0018:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_001d:  ldstr      "B.Print_T(T)"
    IL_0022:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0027:  ldc.i4.0
    IL_0028:  ceq
    IL_002a:  stloc.3
    IL_002b:  ldloc.3
    IL_002c:  brtrue.s   IL_0030

    IL_002e:  ldc.i4.0
    IL_002f:  stloc.0
    IL_0030:  call       class I`1<int32> HelloWorld::Test_C()
    IL_0035:  stloc.1
    IL_0036:  ldloc.1
    IL_0037:  ldc.i4.1
    IL_0038:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_003d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0042:  nop
    IL_0043:  ldloc.1
    IL_0044:  ldc.i4.1
    IL_0045:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_004a:  ldstr      "C.Print_T(T)"
    IL_004f:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0054:  ldc.i4.0
    IL_0055:  ceq
    IL_0057:  stloc.3
    IL_0058:  ldloc.3
    IL_0059:  brtrue.s   IL_005d

    IL_005b:  ldc.i4.0
    IL_005c:  stloc.0
    IL_005d:  call       class I`1<int32> HelloWorld::Test_D_TUV()
    IL_0062:  stloc.1
    IL_0063:  ldloc.1
    IL_0064:  ldc.i4.1
    IL_0065:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006f:  nop
    IL_0070:  ldloc.1
    IL_0071:  ldc.i4.1
    IL_0072:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_0077:  ldstr      "D.Print_T(T)"
    IL_007c:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0081:  ldc.i4.0
    IL_0082:  ceq
    IL_0084:  stloc.3
    IL_0085:  ldloc.3
    IL_0086:  brtrue.s   IL_008a

    IL_0088:  ldc.i4.0
    IL_0089:  stloc.0
    IL_008a:  call       class I`1<int32> HelloWorld::Test_D_TVU()
    IL_008f:  stloc.1
    IL_0090:  ldloc.1
    IL_0091:  ldc.i4.1
    IL_0092:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_0097:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009c:  nop
    IL_009d:  ldloc.1
    IL_009e:  ldc.i4.1
    IL_009f:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_00a4:  ldstr      "D.Print_T(T)"
    IL_00a9:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00ae:  ldc.i4.0
    IL_00af:  ceq
    IL_00b1:  stloc.3
    IL_00b2:  ldloc.3
    IL_00b3:  brtrue.s   IL_00b7

    IL_00b5:  ldc.i4.0
    IL_00b6:  stloc.0
    IL_00b7:  call       class I`1<int32> HelloWorld::Test_D_UTV()
    IL_00bc:  stloc.1
    IL_00bd:  ldloc.1
    IL_00be:  ldc.i4.1
    IL_00bf:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_00c4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c9:  nop
    IL_00ca:  ldloc.1
    IL_00cb:  ldc.i4.1
    IL_00cc:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_00d1:  ldstr      "D.Print_T(T)"
    IL_00d6:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00db:  ldc.i4.0
    IL_00dc:  ceq
    IL_00de:  stloc.3
    IL_00df:  ldloc.3
    IL_00e0:  brtrue.s   IL_00e4

    IL_00e2:  ldc.i4.0
    IL_00e3:  stloc.0
    IL_00e4:  call       class I`1<int32> HelloWorld::Test_D_UVT()
    IL_00e9:  stloc.1
    IL_00ea:  ldloc.1
    IL_00eb:  ldc.i4.1
    IL_00ec:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_00f1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00f6:  nop
    IL_00f7:  ldloc.1
    IL_00f8:  ldc.i4.1
    IL_00f9:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_00fe:  ldstr      "D.Print_T(T)"
    IL_0103:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0108:  ldc.i4.0
    IL_0109:  ceq
    IL_010b:  stloc.3
    IL_010c:  ldloc.3
    IL_010d:  brtrue.s   IL_0111

    IL_010f:  ldc.i4.0
    IL_0110:  stloc.0
    IL_0111:  call       class I`1<int32> HelloWorld::Test_D_VTU()
    IL_0116:  stloc.1
    IL_0117:  ldloc.1
    IL_0118:  ldc.i4.1
    IL_0119:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_011e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0123:  nop
    IL_0124:  ldloc.1
    IL_0125:  ldc.i4.1
    IL_0126:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_012b:  ldstr      "D.Print_T(T)"
    IL_0130:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0135:  ldc.i4.0
    IL_0136:  ceq
    IL_0138:  stloc.3
    IL_0139:  ldloc.3
    IL_013a:  brtrue.s   IL_013e

    IL_013c:  ldc.i4.0
    IL_013d:  stloc.0
    IL_013e:  call       class I`1<int32> HelloWorld::Test_D_VUT()
    IL_0143:  stloc.1
    IL_0144:  ldloc.1
    IL_0145:  ldc.i4.1
    IL_0146:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_014b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0150:  nop
    IL_0151:  ldloc.1
    IL_0152:  ldc.i4.1
    IL_0153:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_0158:  ldstr      "D.Print_T(T)"
    IL_015d:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0162:  ldc.i4.0
    IL_0163:  ceq
    IL_0165:  stloc.3
    IL_0166:  ldloc.3
    IL_0167:  brtrue.s   IL_016b

    IL_0169:  ldc.i4.0
    IL_016a:  stloc.0
    IL_016b:  ldloc.0
    IL_016c:  ldc.i4.0
    IL_016d:  ceq
    IL_016f:  stloc.3
    IL_0170:  ldloc.3
    IL_0171:  brtrue.s   IL_0178

    IL_0173:  ldc.i4.s   100
    IL_0175:  stloc.2
    IL_0176:  br.s       IL_017c

    IL_0178:  ldc.i4.m1
    IL_0179:  stloc.2
    IL_017a:  br.s       IL_017c

    IL_017c:  ldloc.2
    IL_017d:  ret
  } // end of method HelloWorld::Main

  .method public hidebysig static class I`1<int32> 
          Test_B() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class B`2<int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_B

  .method public hidebysig static class I`1<int32> 
          Test_C() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class C`2<int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_C

  .method public hidebysig static class I`1<int32> 
          Test_D_TUV() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_TUV`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_TUV

  .method public hidebysig static class I`1<int32> 
          Test_D_TVU() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_TVU`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_TVU

  .method public hidebysig static class I`1<int32> 
          Test_D_UTV() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_UTV`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_UTV

  .method public hidebysig static class I`1<int32> 
          Test_D_UVT() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_UVT`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_UVT

  .method public hidebysig static class I`1<int32> 
          Test_D_VTU() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_VTU`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_VTU

  .method public hidebysig static class I`1<int32> 
          Test_D_VUT() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class I`1<int32> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class D_VUT`3<int32,int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method HelloWorld::Test_D_VUT

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

.class interface private abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method I`1::Print

} // end of class I`1

.class private auto ansi beforefieldinit A`1<T>
       extends [mscorlib]System.Object
       implements class I`1<!T>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<T>.Print'(!T t) cil managed
  {
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "A.I<T>.Print(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A`1::'I<T>.Print'

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

.class private auto ansi beforefieldinit B`2<T,U>
       extends class A`1<!T>
       implements class I`1<!U>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<U>.Print'(!U u) cil managed
  {
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "B.I<I>.Print(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::'I<U>.Print'

  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::Print_T

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`1<!T>::.ctor()
    IL_0006:  ret
  } // end of method B`2::.ctor

} // end of class B`2

.class private auto ansi beforefieldinit C`2<T,U>
       extends class A`1<!T>
       implements class I`1<!U>
{
  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "C.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method C`2::Print_T

  .method private hidebysig newslot virtual final 
          instance string  'I<U>.Print'(!U u) cil managed
  {
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "C.I<U>.Print(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method C`2::'I<U>.Print'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`1<!T>::.ctor()
    IL_0006:  ret
  } // end of method C`2::.ctor

} // end of class C`2

.class private auto ansi beforefieldinit DBase`2<T,U>
       extends class A`1<!T>
       implements class I`1<!U>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<U>.Print'(!U u) cil managed
  {
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "DBase.I<U>.Print(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method DBase`2::'I<U>.Print'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`1<!T>::.ctor()
    IL_0006:  ret
  } // end of method DBase`2::.ctor

} // end of class DBase`2

.class private auto ansi beforefieldinit D_TUV`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TUV`3::Print_T

  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TUV`3::Print_U

  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TUV`3::'I<V>.Print'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_TUV`3::.ctor

} // end of class D_TUV`3

.class private auto ansi beforefieldinit D_TVU`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TVU`3::Print_T

  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TVU`3::'I<V>.Print'

  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_TVU`3::Print_U

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_TVU`3::.ctor

} // end of class D_TVU`3

.class private auto ansi beforefieldinit D_UTV`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UTV`3::Print_U

  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UTV`3::Print_T

  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UTV`3::'I<V>.Print'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_UTV`3::.ctor

} // end of class D_UTV`3

.class private auto ansi beforefieldinit D_UVT`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UVT`3::Print_U

  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UVT`3::'I<V>.Print'

  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_UVT`3::Print_T

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_UVT`3::.ctor

} // end of class D_UVT`3

.class private auto ansi beforefieldinit D_VTU`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VTU`3::'I<V>.Print'

  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VTU`3::Print_T

  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VTU`3::Print_U

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_VTU`3::.ctor

} // end of class D_VTU`3

.class private auto ansi beforefieldinit D_VUT`3<T,U,V>
       extends class DBase`2<!T,!U>
       implements class I`1<!V>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<V>.Print'(!V v) cil managed
  {
    .override  method instance string class I`1<!V>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.I<V>.Print(V)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VUT`3::'I<V>.Print'

  .method public hidebysig newslot virtual 
          instance string  Print_U(!U u) cil managed
  {
//CHANGE: Add implementation of I<U>.Print
    .override  method instance string class I`1<!U>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_U(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VUT`3::Print_U

  .method public hidebysig newslot virtual 
          instance string  Print_T(!T t) cil managed
  {
//CHANGE: Add implementation of I<T>.Print
    .override  method instance string class I`1<!T>::Print(!0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init ([0] string CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "D.Print_T(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method D_VUT`3::Print_T

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class DBase`2<!T,!U>::.ctor()
    IL_0006:  ret
  } // end of method D_VUT`3::.ctor

} // end of class D_VUT`3


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld_out.res
