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
// MVID: {CA3A864B-A75E-4A75-9454-0BEF07F1FCF4}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01590000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static bool  Check(string actual,
                                               string expected) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_000f:  stloc.0
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.0
    IL_0013:  ret
  } // end of method HelloWorld::Check

  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       450 (0x1c2)
    .maxstack  3
    .locals init (bool V_0,
             class I`1<int32> V_1,
             class I2`1<int32> V_2,
             class J`2<int32,int32> V_3,
             class J2`2<int32,int32> V_4,
             class K`1<string> V_5,
             class K2`1<string> V_6,
             class J`2<string,string> V_7,
             class J2`2<string,string> V_8,
             int32 V_9,
             bool V_10)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void A::.ctor()
    IL_0008:  castclass  class I`1<int32>
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
    IL_0010:  ldc.i4.1
    IL_0011:  callvirt   instance string class I`1<int32>::Print(int32)
    IL_0016:  ldstr      "A.Print(int)"
    IL_001b:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0020:  and
    IL_0021:  stloc.0
    IL_0022:  ldloc.0
    IL_0023:  ldloc.1
    IL_0024:  ldc.i4.1
    IL_0025:  callvirt   instance string class I`1<int32>::Print(!0)
    IL_002a:  ldstr      "A.Print(int)"
    IL_002f:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0034:  and
    IL_0035:  stloc.0
    IL_0036:  newobj     instance void A::.ctor()
    IL_003b:  castclass  class I2`1<int32>
    IL_0040:  stloc.2
    IL_0041:  ldloc.0
    IL_0042:  ldloc.2
    IL_0043:  ldc.i4.1
    IL_0044:  callvirt   instance string class I2`1<int32>::Print(int32)
    IL_0049:  ldstr      "A.Print(int)"
    IL_004e:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0053:  and
    IL_0054:  stloc.0
    IL_0055:  ldloc.0
    IL_0056:  ldloc.2
    IL_0057:  ldc.i4.1
    IL_0058:  callvirt   instance string class I2`1<int32>::Print(!0)
    IL_005d:  ldstr      "A.Print(int)"
    IL_0062:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0067:  and
    IL_0068:  stloc.0
    IL_0069:  newobj     instance void A::.ctor()
    IL_006e:  castclass  class J`2<int32,int32>
    IL_0073:  stloc.3
    IL_0074:  ldloc.0
    IL_0075:  ldloc.3
    IL_0076:  ldc.i4.1
    IL_0077:  callvirt   instance string class J`2<int32,int32>::Print(!0)
    IL_007c:  ldstr      "A.Print(int)"
    IL_0081:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0086:  and
    IL_0087:  stloc.0
    IL_0088:  ldloc.0
    IL_0089:  ldloc.3
    IL_008a:  ldc.i4.1
    IL_008b:  callvirt   instance string class J`2<int32,int32>::Print(!1)
    IL_0090:  ldstr      "A.Print(int)"
    IL_0095:  call       bool HelloWorld::Check(string,
                                                string)
    IL_009a:  and
    IL_009b:  stloc.0
    IL_009c:  newobj     instance void A::.ctor()
    IL_00a1:  castclass  class J2`2<int32,int32>
    IL_00a6:  stloc.s    V_4
    IL_00a8:  ldloc.0
    IL_00a9:  ldloc.s    V_4
    IL_00ab:  ldc.i4.1
    IL_00ac:  callvirt   instance string class J2`2<int32,int32>::Print(!0)
    IL_00b1:  ldstr      "A.Print(int)"
    IL_00b6:  call       bool HelloWorld::Check(string,
                                                string)
    IL_00bb:  and
    IL_00bc:  stloc.0
    IL_00bd:  ldloc.0
    IL_00be:  ldloc.s    V_4
    IL_00c0:  ldc.i4.1
    IL_00c1:  callvirt   instance string class J2`2<int32,int32>::Print(!1)
    IL_00c6:  ldstr      "A.Print(int)"
    IL_00cb:  call       bool HelloWorld::Check(string,
                                                string)
    IL_00d0:  and
    IL_00d1:  stloc.0
    IL_00d2:  newobj     instance void B::.ctor()
    IL_00d7:  castclass  class K`1<string>
    IL_00dc:  stloc.s    V_5
    IL_00de:  ldloc.0
    IL_00df:  ldloc.s    V_5
    IL_00e1:  ldnull
    IL_00e2:  callvirt   instance string class K`1<string>::Print(string)
    IL_00e7:  ldstr      "B.Print(string)"
    IL_00ec:  call       bool HelloWorld::Check(string,
                                                string)
    IL_00f1:  and
    IL_00f2:  stloc.0
    IL_00f3:  ldloc.0
    IL_00f4:  ldloc.s    V_5
    IL_00f6:  ldnull
    IL_00f7:  callvirt   instance string class K`1<string>::Print(!0)
    IL_00fc:  ldstr      "B.Print(string)"
    IL_0101:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0106:  and
    IL_0107:  stloc.0
    IL_0108:  newobj     instance void B::.ctor()
    IL_010d:  castclass  class K2`1<string>
    IL_0112:  stloc.s    V_6
    IL_0114:  ldloc.0
    IL_0115:  ldloc.s    V_6
    IL_0117:  ldnull
    IL_0118:  callvirt   instance string class K2`1<string>::Print(string)
    IL_011d:  ldstr      "B.Print(string)"
    IL_0122:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0127:  and
    IL_0128:  stloc.0
    IL_0129:  ldloc.0
    IL_012a:  ldloc.s    V_6
    IL_012c:  ldnull
    IL_012d:  callvirt   instance string class K2`1<string>::Print(!0)
    IL_0132:  ldstr      "B.Print(string)"
    IL_0137:  call       bool HelloWorld::Check(string,
                                                string)
    IL_013c:  and
    IL_013d:  stloc.0
    IL_013e:  newobj     instance void B::.ctor()
    IL_0143:  castclass  class J`2<string,string>
    IL_0148:  stloc.s    V_7
    IL_014a:  ldloc.0
    IL_014b:  ldloc.s    V_7
    IL_014d:  ldnull
    IL_014e:  callvirt   instance string class J`2<string,string>::Print(!0)
    IL_0153:  ldstr      "B.Print(string)"
    IL_0158:  call       bool HelloWorld::Check(string,
                                                string)
    IL_015d:  and
    IL_015e:  stloc.0
    IL_015f:  ldloc.0
    IL_0160:  ldloc.s    V_7
    IL_0162:  ldnull
    IL_0163:  callvirt   instance string class J`2<string,string>::Print(!1)
    IL_0168:  ldstr      "B.Print(string)"
    IL_016d:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0172:  and
    IL_0173:  stloc.0
    IL_0174:  newobj     instance void B::.ctor()
    IL_0179:  castclass  class J2`2<string,string>
    IL_017e:  stloc.s    V_8
    IL_0180:  ldloc.0
    IL_0181:  ldloc.s    V_8
    IL_0183:  ldnull
    IL_0184:  callvirt   instance string class J2`2<string,string>::Print(!0)
    IL_0189:  ldstr      "B.Print(string)"
    IL_018e:  call       bool HelloWorld::Check(string,
                                                string)
    IL_0193:  and
    IL_0194:  stloc.0
    IL_0195:  ldloc.0
    IL_0196:  ldloc.s    V_8
    IL_0198:  ldnull
    IL_0199:  callvirt   instance string class J2`2<string,string>::Print(!1)
    IL_019e:  ldstr      "B.Print(string)"
    IL_01a3:  call       bool HelloWorld::Check(string,
                                                string)
    IL_01a8:  and
    IL_01a9:  stloc.0
    IL_01aa:  ldloc.0
    IL_01ab:  ldc.i4.0
    IL_01ac:  ceq
    IL_01ae:  stloc.s    V_10
    IL_01b0:  ldloc.s    V_10
    IL_01b2:  brtrue.s   IL_01ba

    IL_01b4:  ldc.i4.s   100
    IL_01b6:  stloc.s    V_9
    IL_01b8:  br.s       IL_01bf

    IL_01ba:  ldc.i4.m1
    IL_01bb:  stloc.s    V_9
    IL_01bd:  br.s       IL_01bf

    IL_01bf:  ldloc.s    V_9
    IL_01c1:  ret
  } // end of method HelloWorld::Main

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
          instance string  Print(int32 i) cil managed
  {
  } // end of method I`1::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method I`1::Print

} // end of class I`1

.class interface private abstract auto ansi I2`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method I2`1::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(int32 i) cil managed
  {
  } // end of method I2`1::Print

} // end of class I2`1

.class interface private abstract auto ansi J`2<T,U>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method J`2::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(!U u) cil managed
  {
  } // end of method J`2::Print

} // end of class J`2

.class interface private abstract auto ansi J2`2<T,U>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!U u) cil managed
  {
  } // end of method J2`2::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method J2`2::Print

} // end of class J2`2

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
       implements class I`1<int32>,
                  class I2`1<int32>,
                  class J`2<int32,int32>,
                  class J2`2<int32,int32>
{
  .method public hidebysig newslot virtual 
          instance string  Print(int32 i) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A.Print(int)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class interface private abstract auto ansi K`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(string s) cil managed
  {
  } // end of method K`1::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method K`1::Print

} // end of class K`1

.class interface private abstract auto ansi K2`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method K2`1::Print

  .method public hidebysig newslot abstract virtual 
          instance string  Print(string s) cil managed
  {
  } // end of method K2`1::Print

} // end of class K2`1

.class private auto ansi beforefieldinit B
       extends [mscorlib]System.Object
       implements class K`1<string>,
                  class K2`1<string>,
                  class J`2<string,string>,
                  class J2`2<string,string>
{
  .method public hidebysig newslot virtual 
          instance string  Print(string s) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print(string)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
