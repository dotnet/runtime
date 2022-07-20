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
// MVID: {30A53143-221A-4FEF-A46B-0607BADC72A0}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x003A0000


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
    // Code size       313 (0x139)
    .maxstack  2
    .locals init (class A`2<int32,int32> V_0,
             string V_1,
             string V_2,
             class A`2<string,string> V_3,
             string V_4,
             string V_5,
             class B`2<int32,int32> V_6,
             string V_7,
             string V_8,
             class B`2<string,string> V_9,
             string V_10,
             string V_11,
             int32 V_12,
             bool V_13)
    IL_0000:  nop
    IL_0001:  newobj     instance void A2_IntInt::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.1
    IL_0009:  callvirt   instance string class A`2<int32,int32>::CallPrintT(!0)
    IL_000e:  stloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldc.i4.1
    IL_0011:  callvirt   instance string class A`2<int32,int32>::CallPrintU(!1)
    IL_0016:  stloc.2
    IL_0017:  newobj     instance void A2_StringString::.ctor()
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  ldstr      ""
    IL_0023:  callvirt   instance string class A`2<string,string>::CallPrintT(!0)
    IL_0028:  stloc.s    V_4
    IL_002a:  ldloc.3
    IL_002b:  ldstr      ""
    IL_0030:  callvirt   instance string class A`2<string,string>::CallPrintU(!1)
    IL_0035:  stloc.s    V_5
    IL_0037:  newobj     instance void B2_IntInt::.ctor()
    IL_003c:  stloc.s    V_6
    IL_003e:  ldloc.s    V_6
    IL_0040:  ldc.i4.1
    IL_0041:  callvirt   instance string class B`2<int32,int32>::CallPrintT(!0)
    IL_0046:  stloc.s    V_7
    IL_0048:  ldloc.s    V_6
    IL_004a:  ldc.i4.1
    IL_004b:  callvirt   instance string class B`2<int32,int32>::CallPrintU(!1)
    IL_0050:  stloc.s    V_8
    IL_0052:  newobj     instance void B2_StringString::.ctor()
    IL_0057:  stloc.s    V_9
    IL_0059:  ldloc.s    V_9
    IL_005b:  ldstr      ""
    IL_0060:  callvirt   instance string class B`2<string,string>::CallPrintT(!0)
    IL_0065:  stloc.s    V_10
    IL_0067:  ldloc.s    V_9
    IL_0069:  ldstr      ""
    IL_006e:  callvirt   instance string class B`2<string,string>::CallPrintU(!1)
    IL_0073:  stloc.s    V_11
    IL_0075:  ldloc.1
    IL_0076:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007b:  nop
    IL_007c:  ldloc.2
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0082:  nop
    IL_0083:  ldloc.s    V_4
    IL_0085:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008a:  nop
    IL_008b:  ldloc.s    V_5
    IL_008d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0092:  nop
    IL_0093:  ldloc.s    V_7
    IL_0095:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009a:  nop
    IL_009b:  ldloc.s    V_8
    IL_009d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a2:  nop
    IL_00a3:  ldloc.s    V_10
    IL_00a5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00aa:  nop
    IL_00ab:  ldloc.s    V_11
    IL_00ad:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b2:  nop
    IL_00b3:  ldloc.1
    IL_00b4:  ldstr      "A.Print(T)"
    IL_00b9:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00be:  brfalse.s  IL_0124

    IL_00c0:  ldloc.2
    IL_00c1:  ldstr      "A2_IntInt.Print()"
    IL_00c6:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00cb:  brfalse.s  IL_0124

    IL_00cd:  ldloc.s    V_4
    IL_00cf:  ldstr      "A.Print(T)"
    IL_00d4:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00d9:  brfalse.s  IL_0124

    IL_00db:  ldloc.s    V_5
    IL_00dd:  ldstr      "A2_StringString.Print()"
    IL_00e2:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00e7:  brfalse.s  IL_0124

    IL_00e9:  ldloc.s    V_7
    IL_00eb:  ldstr      "B2_IntInt.Print()"
    IL_00f0:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00f5:  brfalse.s  IL_0124

    IL_00f7:  ldloc.s    V_8
    IL_00f9:  ldstr      "B.Print(U)"
    IL_00fe:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0103:  brfalse.s  IL_0124

    IL_0105:  ldloc.s    V_10
    IL_0107:  ldstr      "B2_StringString.Print()"
    IL_010c:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0111:  brfalse.s  IL_0124

    IL_0113:  ldloc.s    V_11
    IL_0115:  ldstr      "B.Print(U)"
    IL_011a:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_011f:  ldc.i4.0
    IL_0120:  ceq
    IL_0122:  br.s       IL_0125

    IL_0124:  ldc.i4.1
    IL_0125:  stloc.s    V_13
    IL_0127:  ldloc.s    V_13
    IL_0129:  brtrue.s   IL_0131

    IL_012b:  ldc.i4.s   100
    IL_012d:  stloc.s    V_12
    IL_012f:  br.s       IL_0136

    IL_0131:  ldc.i4.m1
    IL_0132:  stloc.s    V_12
    IL_0134:  br.s       IL_0136

    IL_0136:  ldloc.s    V_12
    IL_0138:  ret
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

.class private auto ansi beforefieldinit A`2<T,U>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Print(!T t) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A.Print(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A`2::Print

  .method public hidebysig newslot virtual 
          instance string  Print(!U u) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A.Print(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A`2::Print

  .method public hidebysig instance string 
          CallPrintT(!T t) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string class A`2<!T,!U>::Print(!0)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method A`2::CallPrintT

  .method public hidebysig instance string 
          CallPrintU(!U u) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string class A`2<!T,!U>::Print(!1)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method A`2::CallPrintU

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`2::.ctor

} // end of class A`2

.class private auto ansi beforefieldinit A2_IntInt
       extends class A`2<int32,int32>
{
  .method public hidebysig virtual 
          instance string  Print(int32 i) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A2_IntInt.Print()"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A2_IntInt::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`2<int32,int32>::.ctor()
    IL_0006:  ret
  } // end of method A2_IntInt::.ctor

} // end of class A2_IntInt

.class private auto ansi beforefieldinit A2_StringString
       extends class A`2<string,string>
{
  .method public hidebysig virtual 
          instance string  Print(string s) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A2_StringString.Print()"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method A2_StringString::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class A`2<string,string>::.ctor()
    IL_0006:  ret
  } // end of method A2_StringString::.ctor

} // end of class A2_StringString

.class private auto ansi beforefieldinit B`2<T,U>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Print(!U u) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::Print

  .method public hidebysig newslot virtual 
          instance string  Print(!T t) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B`2::Print

  .method public hidebysig instance string 
          CallPrintT(!T t) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string class B`2<!T,!U>::Print(!0)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method B`2::CallPrintT

  .method public hidebysig instance string 
          CallPrintU(!U u) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  callvirt   instance string class B`2<!T,!U>::Print(!1)
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  } // end of method B`2::CallPrintU

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B`2::.ctor

} // end of class B`2

.class private auto ansi beforefieldinit B2_IntInt
       extends class B`2<int32,int32>
{
  .method public hidebysig virtual 
          instance string  Print(int32 i) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B2_IntInt.Print()"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B2_IntInt::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class B`2<int32,int32>::.ctor()
    IL_0006:  ret
  } // end of method B2_IntInt::.ctor

} // end of class B2_IntInt

.class private auto ansi beforefieldinit B2_StringString
       extends class B`2<string,string>
{
  .method public hidebysig virtual 
          instance string  Print(string s) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B2_StringString.Print()"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method B2_StringString::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class B`2<string,string>::.ctor()
    IL_0006:  ret
  } // end of method B2_StringString::.ctor

} // end of class B2_StringString


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
