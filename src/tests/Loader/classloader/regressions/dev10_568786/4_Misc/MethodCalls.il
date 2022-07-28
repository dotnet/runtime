// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'MethodCalls'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {D3FCE52A-664A-48CB-A101-57680CEB43F1}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00400000


// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed beforefieldinit MyStruct`2<T,U>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .method public hidebysig static string 
          Method(!T param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Method(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::Method

  .method public hidebysig static string 
          Method(!U param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Method(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::Method

  .method public hidebysig static string 
          GenMethod<M>(!T param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GenMethod<M>(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::GenMethod

  .method public hidebysig static string 
          GenMethod<M>(!U param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GenMethod<M>(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::GenMethod

  .method public hidebysig instance string 
          GenInstMethod<M>(!T param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GenInstMethod<M>(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::GenInstMethod

  .method public hidebysig instance string 
          GenInstMethod<M>(!U param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "GenInstMethod<M>(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::GenInstMethod

  .method public hidebysig instance string 
          VirtMethod(!T param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "VirtMethod(T)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::VirtMethod

  .method public hidebysig instance string 
          VirtMethod(!U param) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "VirtMethod(U)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyStruct`2::VirtMethod

} // end of class MyStruct`2

.class private auto ansi beforefieldinit Caller`4<T,U,V,W>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance string 
          CallMethodT(!U param) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string valuetype MyStruct`2<!U,!V>::Method(!0)
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Caller`4::CallMethodT

  .method public hidebysig instance string 
          CallMethodU(!V param) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string valuetype MyStruct`2<!U,!V>::Method(!1)
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Caller`4::CallMethodU

  .method public hidebysig instance string 
          CallGenMethodT(!U param) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string valuetype MyStruct`2<!U,!V>::GenMethod<!3>(!0)
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Caller`4::CallGenMethodT

  .method public hidebysig instance string 
          CallGenMethodU(!V param) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  call       string valuetype MyStruct`2<!U,!V>::GenMethod<!3>(!1)
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Caller`4::CallGenMethodU

  .method public hidebysig instance string 
          CallGenInstMethodT(!U param) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0,
             valuetype MyStruct`2<!U,!V> V_1)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    valuetype MyStruct`2<!U,!V>
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  call       instance string valuetype MyStruct`2<!U,!V>::GenInstMethod<!3>(!0)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Caller`4::CallGenInstMethodT

  .method public hidebysig instance string 
          CallGenInstMethodU(!V param) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0,
             valuetype MyStruct`2<!U,!V> V_1)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    valuetype MyStruct`2<!U,!V>
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  call       instance string valuetype MyStruct`2<!U,!V>::GenInstMethod<!3>(!1)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Caller`4::CallGenInstMethodU

  .method public hidebysig virtual instance string 
          CallVirtMethodT(!U param) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0,
             valuetype MyStruct`2<!U,!V> V_1)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    valuetype MyStruct`2<!U,!V>
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  call       instance string valuetype MyStruct`2<!U,!V>::VirtMethod(!0)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Caller`4::CallVirtMethodT

  .method public hidebysig virtual instance string 
          CallVirtMethodU(!V param) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0,
             valuetype MyStruct`2<!U,!V> V_1)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    valuetype MyStruct`2<!U,!V>
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  call       instance string valuetype MyStruct`2<!U,!V>::VirtMethod(!1)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Caller`4::CallVirtMethodU

  .method public hidebysig instance string 
          CallParentMethod() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  1
    .locals init (string V_0,
             valuetype MyStruct`2<!U,!V> V_1)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_1
    IL_0003:  initobj    valuetype MyStruct`2<!U,!V>
    IL_0009:  ldloc.1
    IL_000a:  stloc.1
    IL_000b:  ldloca.s   V_1
    IL_000d:  constrained. valuetype MyStruct`2<!U,!V>
    IL_0013:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } // end of method Caller`4::CallParentMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Caller`4::.ctor

} // end of class Caller`4

.class private auto ansi beforefieldinit Test_MethodCalls
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       375 (0x177)
    .maxstack  2
    .locals init (int32 V_0,
             class Caller`4<valuetype MyStruct`2<string,object>,string,object,object> V_1,
             string V_2,
             string V_3,
             int32 V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::.ctor()
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldstr      "mystring"
    IL_000f:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallMethodT(!1)
    IL_0014:  stloc.2
    IL_0015:  ldloc.1
    IL_0016:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_001b:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallMethodU(!2)
    IL_0020:  stloc.3
    IL_0021:  ldloc.2
    IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0027:  nop
    IL_0028:  ldloc.3
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  nop
    IL_002f:  ldloc.2
    IL_0030:  ldstr      "Method(T)"
    IL_0035:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_003a:  brfalse.s  IL_004c

    IL_003c:  ldloc.2
    IL_003d:  ldstr      "Method(U)"
    IL_0042:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0047:  ldc.i4.0
    IL_0048:  ceq
    IL_004a:  br.s       IL_004d

    IL_004c:  ldc.i4.1
    IL_004d:  stloc.s    V_5
    IL_004f:  ldloc.s    V_5
    IL_0051:  brtrue.s   IL_0057

    IL_0053:  ldloc.0
    IL_0054:  ldc.i4.1
    IL_0055:  add
    IL_0056:  stloc.0
    IL_0057:  ldloc.1
    IL_0058:  ldstr      "mystring"
    IL_005d:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallGenMethodT(!1)
    IL_0062:  stloc.2
    IL_0063:  ldloc.1
    IL_0064:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0069:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallGenMethodU(!2)
    IL_006e:  stloc.3
    IL_006f:  ldloc.2
    IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0075:  nop
    IL_0076:  ldloc.3
    IL_0077:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007c:  nop
    IL_007d:  ldloc.2
    IL_007e:  ldstr      "GenMethod<M>(T)"
    IL_0083:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0088:  brfalse.s  IL_009a

    IL_008a:  ldloc.2
    IL_008b:  ldstr      "GenMethod<M>(U)"
    IL_0090:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0095:  ldc.i4.0
    IL_0096:  ceq
    IL_0098:  br.s       IL_009b

    IL_009a:  ldc.i4.1
    IL_009b:  stloc.s    V_5
    IL_009d:  ldloc.s    V_5
    IL_009f:  brtrue.s   IL_00a5

    IL_00a1:  ldloc.0
    IL_00a2:  ldc.i4.1
    IL_00a3:  add
    IL_00a4:  stloc.0
    IL_00a5:  ldloc.1
    IL_00a6:  ldstr      "mystring"
    IL_00ab:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallGenInstMethodT(!1)
    IL_00b0:  stloc.2
    IL_00b1:  ldloc.1
    IL_00b2:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_00b7:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallGenInstMethodU(!2)
    IL_00bc:  stloc.3
    IL_00bd:  ldloc.2
    IL_00be:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c3:  nop
    IL_00c4:  ldloc.3
    IL_00c5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ca:  nop
    IL_00cb:  ldloc.2
    IL_00cc:  ldstr      "GenInstMethod<M>(T)"
    IL_00d1:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00d6:  brfalse.s  IL_00e8

    IL_00d8:  ldloc.2
    IL_00d9:  ldstr      "GenInstMethod<M>(U)"
    IL_00de:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00e3:  ldc.i4.0
    IL_00e4:  ceq
    IL_00e6:  br.s       IL_00e9

    IL_00e8:  ldc.i4.1
    IL_00e9:  stloc.s    V_5
    IL_00eb:  ldloc.s    V_5
    IL_00ed:  brtrue.s   IL_00f3

    IL_00ef:  ldloc.0
    IL_00f0:  ldc.i4.1
    IL_00f1:  add
    IL_00f2:  stloc.0
    IL_00f3:  ldloc.1
    IL_00f4:  ldstr      "mystring"
    IL_00f9:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallVirtMethodT(!1)
    IL_00fe:  stloc.2
    IL_00ff:  ldloc.1
    IL_0100:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0105:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallVirtMethodU(!2)
    IL_010a:  stloc.3
    IL_010b:  ldloc.2
    IL_010c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0111:  nop
    IL_0112:  ldloc.3
    IL_0113:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0118:  nop
    IL_0119:  ldloc.2
    IL_011a:  ldstr      "VirtMethod(T)"
    IL_011f:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0124:  brfalse.s  IL_0136

    IL_0126:  ldloc.2
    IL_0127:  ldstr      "VirtMethod(U)"
    IL_012c:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0131:  ldc.i4.0
    IL_0132:  ceq
    IL_0134:  br.s       IL_0137

    IL_0136:  ldc.i4.1
    IL_0137:  stloc.s    V_5
    IL_0139:  ldloc.s    V_5
    IL_013b:  brtrue.s   IL_0141

    IL_013d:  ldloc.0
    IL_013e:  ldc.i4.1
    IL_013f:  add
    IL_0140:  stloc.0
    IL_0141:  ldloc.1
    IL_0142:  callvirt   instance string class Caller`4<valuetype MyStruct`2<string,object>,string,object,object>::CallParentMethod()
    IL_0147:  stloc.2
    IL_0148:  ldloc.2
    IL_0149:  call       void [System.Console]System.Console::WriteLine(string)
    IL_014e:  nop
    IL_014f:  ldloc.2
    IL_0150:  ldstr      "MyStruct`2[System.String,System.Object]"
    IL_0155:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_015a:  ldc.i4.0
    IL_015b:  ceq
    IL_015d:  stloc.s    V_5
    IL_015f:  ldloc.s    V_5
    IL_0161:  brtrue.s   IL_0167

    IL_0163:  ldloc.0
    IL_0164:  ldc.i4.1
    IL_0165:  add
    IL_0166:  stloc.0
    IL_0167:  ldloc.0
    IL_0168:  brfalse.s  IL_016e

    IL_016a:  ldc.i4.s   -2
    IL_016c:  br.s       IL_0170

    IL_016e:  ldc.i4.s   100
    IL_0170:  stloc.s    V_4
    IL_0172:  br.s       IL_0174

    IL_0174:  ldloc.s    V_4
    IL_0176:  ret
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
// WARNING: Created Win32 resource file MethodCalls.res
