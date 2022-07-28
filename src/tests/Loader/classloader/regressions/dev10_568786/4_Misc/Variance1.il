// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly Variance1
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {DC3FB067-F52C-4120-BD62-DFA180241B4B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x009D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi I`1<-S>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!S 'value') cil managed
  {
  } // end of method I`1::Print

} // end of class I`1

.class public auto ansi beforefieldinit CBase
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CBase::.ctor

} // end of class CBase

.class public auto ansi beforefieldinit CDerived
       extends CBase
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void CBase::.ctor()
    IL_0006:  ret
  } // end of method CDerived::.ctor

} // end of class CDerived

.class public sequential ansi sealed beforefieldinit MyStruct1
       extends [mscorlib]System.ValueType
       implements class I`1<class CDerived>,
                  class I`1<object>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct1::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct1::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct1::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct1::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct1::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct1::Print

} // end of class MyStruct1

.class public sequential ansi sealed beforefieldinit MyStruct2
       extends [mscorlib]System.ValueType
       implements class I`1<object>,
                  class I`1<class CDerived>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct2::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 MyStruct2::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct2::Print

} // end of class MyStruct2

.class public sequential ansi sealed beforefieldinit MyStruct3`2<T,U>
       extends [mscorlib]System.ValueType
       implements class I`1<class CDerived>,
                  class I`1<object>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct3`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct3`2<!T,!U>::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct3`2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct3`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct3`2<!T,!U>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct3`2::Print

} // end of class MyStruct3`2

.class public sequential ansi sealed beforefieldinit MyStruct4`2<T,U>
       extends [mscorlib]System.ValueType
       implements class I`1<object>,
                  class I`1<class CDerived>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct4`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct4`2<!T,!U>::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct4`2::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct4`2<!T,!U>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct4`2<!T,!U>::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct4`2::Print

} // end of class MyStruct4`2

.class public sequential ansi sealed beforefieldinit MyStruct5`1<T>
       extends [mscorlib]System.ValueType
       implements class I`1<class CDerived>,
                  class I`1<!T>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct5`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct5`1<!T>::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct5`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct5`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct5`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct5`1::Print

} // end of class MyStruct5`1

.class public sequential ansi sealed beforefieldinit MyStruct6`1<T>
       extends [mscorlib]System.ValueType
       implements class I`1<!T>,
				  class I`1<class CDerived>
{
  .field public int32 n
  .method public hidebysig newslot virtual final instance string  Print(class CDerived 'value') cil managed
  {
    // Code size       25 (0x19)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct6`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct6`1<!T>::n
    IL_000f:  ldstr      "Print(CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } // end of method MyStruct6`1::Print

  .method public hidebysig newslot virtual final instance string Print(!T 'value') cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 valuetype MyStruct6`1<!T>::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 valuetype MyStruct6`1<!T>::n
    IL_000f:  ldstr      "Print_T("
    IL_0014:  ldtoken    !T
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldstr      ")"
    IL_0023:  call       string [mscorlib]System.String::Concat(object,
                                                                object,
                                                                object)
    IL_0028:  stloc.0
    IL_0029:  br.s       IL_002b

    IL_002b:  ldloc.0
    IL_002c:  ret
  } // end of method MyStruct6`1::Print

} // end of class MyStruct6`1

.class private auto ansi beforefieldinit Conversion`2<.ctor (class I`1<!TCBase>) TMyStruct,TCBase>
       extends [mscorlib]System.Object
{
  .field public !TCBase 'value'
  .method public hidebysig instance string 
          Caller1() cil managed
  {
    // Code size       58 (0x3a)
    .maxstack  2
    .locals init (!TMyStruct V_0,
             string V_1,
             !TMyStruct V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_2
    IL_0003:  initobj    !TMyStruct
    IL_0009:  ldloc.2
    IL_000a:  box        !TMyStruct
    IL_000f:  brfalse.s  IL_001c

    IL_0011:  ldloca.s   V_2
    IL_0013:  initobj    !TMyStruct
    IL_0019:  ldloc.2
    IL_001a:  br.s       IL_0021

    IL_001c:  call       !!0 [mscorlib]System.Activator::CreateInstance<!TMyStruct>()
    IL_0021:  stloc.0
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      !1 class Conversion`2<!TMyStruct,!TCBase>::'value'
    IL_002a:  constrained. !TMyStruct
    IL_0030:  callvirt   instance string class I`1<!TCBase>::Print(!0)
    IL_0035:  stloc.1
    IL_0036:  br.s       IL_0038

    IL_0038:  ldloc.1
    IL_0039:  ret
  } // end of method Conversion`2::Caller1

  .method public hidebysig instance string 
          Caller2(!TMyStruct& 'instance') cil managed
  {
    // Code size       24 (0x18)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.0
    IL_0003:  ldfld      !1 class Conversion`2<!TMyStruct,!TCBase>::'value'
    IL_0008:  constrained. !TMyStruct
    IL_000e:  callvirt   instance string class I`1<!TCBase>::Print(!0)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Conversion`2::Caller2

  .method public hidebysig instance string 
          Caller3(!TMyStruct& 'instance',
                  !TCBase param) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  constrained. !TMyStruct
    IL_0009:  callvirt   instance string class I`1<!TCBase>::Print(!0)
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Conversion`2::Caller3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Conversion`2::.ctor

} // end of class Conversion`2


.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Verification(string res1,
                       string res2,
                       string res3,
                       string expected,
                       int32 n) cil managed
  {
    // Code size       150 (0x96)
    .maxstack  2
    .locals init (int32 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "   "
    IL_0006:  ldarg.0
    IL_0007:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldstr      "   "
    IL_0017:  ldarg.1
    IL_0018:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0022:  nop
    IL_0023:  ldstr      "   "
    IL_0028:  ldarg.2
    IL_0029:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_002e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0033:  nop
    IL_0034:  ldstr      "   "
    IL_0039:  ldarg.s    n
    IL_003b:  box        [mscorlib]System.Int32
    IL_0040:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004a:  nop
    IL_004b:  ldarg.0
    IL_004c:  ldarg.3
    IL_004d:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0052:  brfalse.s  IL_0070

    IL_0054:  ldarg.1
    IL_0055:  ldarg.3
    IL_0056:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005b:  brfalse.s  IL_0070

    IL_005d:  ldarg.2
    IL_005e:  ldarg.3
    IL_005f:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0064:  brfalse.s  IL_0070

    IL_0066:  ldarg.s    n
    IL_0068:  ldc.i4.2
    IL_0069:  ceq
    IL_006b:  ldc.i4.0
    IL_006c:  ceq
    IL_006e:  br.s       IL_0071

    IL_0070:  ldc.i4.1
    IL_0071:  stloc.1
    IL_0072:  ldloc.1
    IL_0073:  brtrue.s   IL_0085

    IL_0075:  nop
    IL_0076:  ldstr      "PASS\n"
    IL_007b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0080:  nop
    IL_0081:  ldc.i4.0
    IL_0082:  stloc.0
    IL_0083:  br.s       IL_0094

    IL_0085:  ldstr      "FAIL\n"
    IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008f:  nop
    IL_0090:  ldc.i4.1
    IL_0091:  stloc.0
    IL_0092:  br.s       IL_0094

    IL_0094:  ldloc.0
    IL_0095:  ret
  } // end of method Program::Verification

  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       898 (0x382)
    .maxstack  6
    .locals init (int32 V_0,
             valuetype MyStruct1 V_1,
             valuetype MyStruct2 V_2,
             valuetype MyStruct3`2<object,object> V_3,
             valuetype MyStruct4`2<object,object> V_4,
             valuetype MyStruct5`1<object> V_5,
             valuetype MyStruct6`1<object> V_6,
             class Conversion`2<valuetype MyStruct1,class CBase> V_11,
             class Conversion`2<valuetype MyStruct2,class CBase> V_12,
             class Conversion`2<valuetype MyStruct3`2<object,object>,class CBase> V_13,
             class Conversion`2<valuetype MyStruct4`2<object,object>,class CBase> V_14,
             class Conversion`2<valuetype MyStruct5`1<object>,class CBase> V_15,
             class Conversion`2<valuetype MyStruct6`1<object>,class CBase> V_16,
             string V_21,
             string V_22,
             string V_23,
             int32 V_24)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    MyStruct1
    IL_000b:  ldloca.s   V_2
    IL_000d:  initobj    MyStruct2
    IL_0013:  ldloca.s   V_3
    IL_0015:  initobj    valuetype MyStruct3`2<object,object>
    IL_001b:  ldloca.s   V_4
    IL_001d:  initobj    valuetype MyStruct4`2<object,object>
    IL_0023:  ldloca.s   V_5
    IL_0025:  initobj    valuetype MyStruct5`1<object>
    IL_002b:  ldloca.s   V_6
    IL_002d:  initobj    valuetype MyStruct6`1<object>
    IL_0053:  newobj     instance void class Conversion`2<valuetype MyStruct1,class CBase>::.ctor()
    IL_0058:  stloc.s    V_11
    IL_005a:  newobj     instance void class Conversion`2<valuetype MyStruct2,class CBase>::.ctor()
    IL_005f:  stloc.s    V_12
    IL_0061:  newobj     instance void class Conversion`2<valuetype MyStruct3`2<object,object>,class CBase>::.ctor()
    IL_0066:  stloc.s    V_13
    IL_0068:  newobj     instance void class Conversion`2<valuetype MyStruct4`2<object,object>,class CBase>::.ctor()
    IL_006d:  stloc.s    V_14
    IL_006f:  newobj     instance void class Conversion`2<valuetype MyStruct5`1<object>,class CBase>::.ctor()
    IL_0074:  stloc.s    V_15
    IL_0076:  newobj     instance void class Conversion`2<valuetype MyStruct6`1<object>,class CBase>::.ctor()
    IL_007b:  stloc.s    V_16
    IL_0099:  ldstr      "TEST1:"
    IL_009e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a3:  nop
    IL_00a4:  ldloc.s    V_11
    IL_00a6:  callvirt   instance string class Conversion`2<valuetype MyStruct1,class CBase>::Caller1()
    IL_00ab:  stloc.s    V_21
    IL_00ad:  ldloc.s    V_11
    IL_00af:  ldloca.s   V_1
    IL_00b1:  callvirt   instance string class Conversion`2<valuetype MyStruct1,class CBase>::Caller2(!0&)
    IL_00b6:  stloc.s    V_22
    IL_00b8:  ldloc.s    V_11
    IL_00ba:  ldloca.s   V_1
    IL_00bc:  newobj     instance void class CBase::.ctor()
    IL_00c1:  callvirt   instance string class Conversion`2<valuetype MyStruct1,class CBase>::Caller3(!0&,
                                                                                                          !1)
    IL_00c6:  stloc.s    V_23
    IL_00c8:  ldloc.0
    IL_00c9:  ldloc.s    V_21
    IL_00cb:  ldloc.s    V_22
    IL_00cd:  ldloc.s    V_23
    IL_00cf:  ldstr      "Print(object)"
    IL_00d4:  ldloca.s   V_1
    IL_00d6:  ldfld      int32 MyStruct1::n
    IL_00db:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_00e0:  add
    IL_00e1:  stloc.0
    IL_00e2:  ldstr      "TEST2:"
    IL_00e7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ec:  nop
    IL_00ed:  ldloc.s    V_12
    IL_00ef:  callvirt   instance string class Conversion`2<valuetype MyStruct2,class CBase>::Caller1()
    IL_00f4:  stloc.s    V_21
    IL_00f6:  ldloc.s    V_12
    IL_00f8:  ldloca.s   V_2
    IL_00fa:  callvirt   instance string class Conversion`2<valuetype MyStruct2,class CBase>::Caller2(!0&)
    IL_00ff:  stloc.s    V_22
    IL_0101:  ldloc.s    V_12
    IL_0103:  ldloca.s   V_2
    IL_0105:  newobj     instance void class CBase::.ctor()
    IL_010a:  callvirt   instance string class Conversion`2<valuetype MyStruct2,class CBase>::Caller3(!0&,
                                                                                                          !1)
    IL_010f:  stloc.s    V_23
    IL_0111:  ldloc.0
    IL_0112:  ldloc.s    V_21
    IL_0114:  ldloc.s    V_22
    IL_0116:  ldloc.s    V_23
    IL_0118:  ldstr      "Print(object)"
    IL_011d:  ldloca.s   V_2
    IL_011f:  ldfld      int32 MyStruct2::n
    IL_0124:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0129:  add
    IL_012a:  stloc.0
    IL_012b:  ldstr      "TEST3:"
    IL_0130:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0135:  nop
    IL_0136:  ldloc.s    V_13
    IL_0138:  callvirt   instance string class Conversion`2<valuetype MyStruct3`2<object,object>,class CBase>::Caller1()
    IL_013d:  stloc.s    V_21
    IL_013f:  ldloc.s    V_13
    IL_0141:  ldloca.s   V_3
    IL_0143:  callvirt   instance string class Conversion`2<valuetype MyStruct3`2<object,object>,class CBase>::Caller2(!0&)
    IL_0148:  stloc.s    V_22
    IL_014a:  ldloc.s    V_13
    IL_014c:  ldloca.s   V_3
    IL_014e:  newobj     instance void class CBase::.ctor()
    IL_0153:  callvirt   instance string class Conversion`2<valuetype MyStruct3`2<object,object>,class CBase>::Caller3(!0&,
                                                                                                                           !1)
    IL_0158:  stloc.s    V_23
    IL_015a:  ldloc.0
    IL_015b:  ldloc.s    V_21
    IL_015d:  ldloc.s    V_22
    IL_015f:  ldloc.s    V_23
    IL_0161:  ldstr      "Print(object)"
    IL_0166:  ldloca.s   V_3
    IL_0168:  ldfld      int32 valuetype MyStruct3`2<object,object>::n
    IL_016d:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0172:  add
    IL_0173:  stloc.0
    IL_0174:  ldstr      "TEST4:"
    IL_0179:  call       void [System.Console]System.Console::WriteLine(string)
    IL_017e:  nop
    IL_017f:  ldloc.s    V_14
    IL_0181:  callvirt   instance string class Conversion`2<valuetype MyStruct4`2<object,object>,class CBase>::Caller1()
    IL_0186:  stloc.s    V_21
    IL_0188:  ldloc.s    V_14
    IL_018a:  ldloca.s   V_4
    IL_018c:  callvirt   instance string class Conversion`2<valuetype MyStruct4`2<object,object>,class CBase>::Caller2(!0&)
    IL_0191:  stloc.s    V_22
    IL_0193:  ldloc.s    V_14
    IL_0195:  ldloca.s   V_4
    IL_0197:  newobj     instance void class CBase::.ctor()
    IL_019c:  callvirt   instance string class Conversion`2<valuetype MyStruct4`2<object,object>,class CBase>::Caller3(!0&,
                                                                                                                           !1)
    IL_01a1:  stloc.s    V_23
    IL_01a3:  ldloc.0
    IL_01a4:  ldloc.s    V_21
    IL_01a6:  ldloc.s    V_22
    IL_01a8:  ldloc.s    V_23
    IL_01aa:  ldstr      "Print(object)"
    IL_01af:  ldloca.s   V_4
    IL_01b1:  ldfld      int32 valuetype MyStruct4`2<object,object>::n
    IL_01b6:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_01bb:  add
    IL_01bc:  stloc.0
    IL_01bd:  ldstr      "TEST5:"
    IL_01c2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01c7:  nop
    IL_01c8:  ldloc.s    V_15
    IL_01ca:  callvirt   instance string class Conversion`2<valuetype MyStruct5`1<object>,class CBase>::Caller1()
    IL_01cf:  stloc.s    V_21
    IL_01d1:  ldloc.s    V_15
    IL_01d3:  ldloca.s   V_5
    IL_01d5:  callvirt   instance string class Conversion`2<valuetype MyStruct5`1<object>,class CBase>::Caller2(!0&)
    IL_01da:  stloc.s    V_22
    IL_01dc:  ldloc.s    V_15
    IL_01de:  ldloca.s   V_5
    IL_01e0:  newobj     instance void class CBase::.ctor()
    IL_01e5:  callvirt   instance string class Conversion`2<valuetype MyStruct5`1<object>,class CBase>::Caller3(!0&,
                                                                                                                    !1)
    IL_01ea:  stloc.s    V_23
    IL_01ec:  ldloc.0
    IL_01ed:  ldloc.s    V_21
    IL_01ef:  ldloc.s    V_22
    IL_01f1:  ldloc.s    V_23
    IL_01f3:  ldstr      "Print_T(System.Object)"
    IL_01f8:  ldloca.s   V_5
    IL_01fa:  ldfld      int32 valuetype MyStruct5`1<object>::n
    IL_01ff:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0204:  add
    IL_0205:  stloc.0
    IL_0206:  ldstr      "TEST6:"
    IL_020b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0210:  nop
    IL_0211:  ldloc.s    V_16
    IL_0213:  callvirt   instance string class Conversion`2<valuetype MyStruct6`1<object>,class CBase>::Caller1()
    IL_0218:  stloc.s    V_21
    IL_021a:  ldloc.s    V_16
    IL_021c:  ldloca.s   V_6
    IL_021e:  callvirt   instance string class Conversion`2<valuetype MyStruct6`1<object>,class CBase>::Caller2(!0&)
    IL_0223:  stloc.s    V_22
    IL_0225:  ldloc.s    V_16
    IL_0227:  ldloca.s   V_6
    IL_0229:  newobj     instance void class CBase::.ctor()
    IL_022e:  callvirt   instance string class Conversion`2<valuetype MyStruct6`1<object>,class CBase>::Caller3(!0&,
                                                                                                                    !1)
    IL_0233:  stloc.s    V_23
    IL_0235:  ldloc.0
    IL_0236:  ldloc.s    V_21
    IL_0238:  ldloc.s    V_22
    IL_023a:  ldloc.s    V_23
    IL_023c:  ldstr      "Print_T(System.Object)"
    IL_0241:  ldloca.s   V_6
    IL_0243:  ldfld      int32 valuetype MyStruct6`1<object>::n
    IL_0248:  call       int32 Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_024d:  add
    IL_024e:  stloc.0
    IL_0373:  ldloc.0
    IL_0374:  brfalse.s  IL_0379

    IL_0376:  ldc.i4.m1
    IL_0377:  br.s       IL_037b

    IL_0379:  ldc.i4.s   100
    IL_037b:  stloc.s    V_24
    IL_037d:  br.s       IL_037f

    IL_037f:  ldloc.s    V_24
    IL_0381:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program


