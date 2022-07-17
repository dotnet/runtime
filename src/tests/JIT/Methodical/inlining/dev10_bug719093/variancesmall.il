// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 4:0:0:0
}
.assembly 'variancesmall'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.namespace Test_variancesmall_il
{
.class interface private abstract auto ansi I`1<-S>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!S 'value') cil managed
  {
  } 

} 

.class public auto ansi beforefieldinit CBase
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class public auto ansi beforefieldinit CDerived
       extends Test_variancesmall_il.CBase
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Test_variancesmall_il.CBase::.ctor()
    IL_0006:  ret
  } 

} 

.class public sequential ansi sealed beforefieldinit MyStruct2
       extends [mscorlib]System.ValueType
       implements class Test_variancesmall_il.I`1<object>,
                  class Test_variancesmall_il.I`1<class Test_variancesmall_il.CDerived>
{
  .field public int32 n
  .method public hidebysig newslot virtual final 
          instance string  Print(class Test_variancesmall_il.CDerived 'value') cil managed
  {
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 Test_variancesmall_il.MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 Test_variancesmall_il.MyStruct2::n
    IL_000f:  ldstr      "Print(Test_variancesmall_il.CDerived)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } 

  .method public hidebysig newslot virtual final 
          instance string  Print(object 'value') cil managed
  {
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  dup
    IL_0003:  ldfld      int32 Test_variancesmall_il.MyStruct2::n
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stfld      int32 Test_variancesmall_il.MyStruct2::n
    IL_000f:  ldstr      "Print(object)"
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.0
    IL_0018:  ret
  } 

} 

.class private auto ansi beforefieldinit Conversion`2<.ctor (class Test_variancesmall_il.I`1<!TCBase>) TMyStruct,TCBase>
       extends [mscorlib]System.Object
{
  .field public !TCBase 'value'
  .method public hidebysig instance string 
          Caller1() cil managed
  {
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
    IL_0025:  ldfld      !1 class Test_variancesmall_il.Conversion`2<!TMyStruct,!TCBase>::'value'
    IL_002a:  constrained. !TMyStruct
    IL_0030:  callvirt   instance string class Test_variancesmall_il.I`1<!TCBase>::Print(!0)
    IL_0035:  stloc.1
    IL_0036:  br.s       IL_0038

    IL_0038:  ldloc.1
    IL_0039:  ret
  } 

  .method public hidebysig instance string 
          Caller2(!TMyStruct& 'instance') cil managed
  {
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.0
    IL_0003:  ldfld      !1 class Test_variancesmall_il.Conversion`2<!TMyStruct,!TCBase>::'value'
    IL_0008:  constrained. !TMyStruct
    IL_000e:  callvirt   instance string class Test_variancesmall_il.I`1<!TCBase>::Print(!0)
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } 

  .method public hidebysig instance string 
          Caller3(!TMyStruct& 'instance',
                  !TCBase param) cil managed
  {
    .maxstack  2
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  constrained. !TMyStruct
    IL_0009:  callvirt   instance string class Test_variancesmall_il.I`1<!TCBase>::Print(!0)
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 


.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Verification(string res1,
                       string res2,
                       string res3,
                       string expected,
                       int32 n) cil managed
  {
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
  } 

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  6
    .locals init (int32 V_0,
             valuetype Test_variancesmall_il.MyStruct2 V_2,
             class Test_variancesmall_il.Conversion`2<valuetype Test_variancesmall_il.MyStruct2,class Test_variancesmall_il.CBase> V_12,
             string V_21,
             string V_22,
             string V_23,
             int32 V_24)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_000b:  ldloca.s   V_2
    IL_000d:  initobj    Test_variancesmall_il.MyStruct2
    IL_005a:  newobj     instance void class Test_variancesmall_il.Conversion`2<valuetype Test_variancesmall_il.MyStruct2,class Test_variancesmall_il.CBase>::.ctor()
    IL_005f:  stloc.s    V_12
    IL_00e2:  ldstr      "TEST2:"
    IL_00e7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ec:  nop
    IL_00ed:  ldloc.s    V_12
    IL_00ef:  callvirt   instance string class Test_variancesmall_il.Conversion`2<valuetype Test_variancesmall_il.MyStruct2,class Test_variancesmall_il.CBase>::Caller1()
    IL_00f4:  stloc.s    V_21
    IL_00f6:  ldloc.s    V_12
    IL_00f8:  ldloca.s   V_2
    IL_00fa:  callvirt   instance string class Test_variancesmall_il.Conversion`2<valuetype Test_variancesmall_il.MyStruct2,class Test_variancesmall_il.CBase>::Caller2(!0&)
    IL_00ff:  stloc.s    V_22
    IL_0101:  ldloc.s    V_12
    IL_0103:  ldloca.s   V_2
    IL_0105:  newobj     instance void class Test_variancesmall_il.CBase::.ctor()
    IL_010a:  callvirt   instance string class Test_variancesmall_il.Conversion`2<valuetype Test_variancesmall_il.MyStruct2,class Test_variancesmall_il.CBase>::Caller3(!0&,
                                                                                                          !1)
    IL_010f:  stloc.s    V_23
    IL_0111:  ldloc.0
    IL_0112:  ldloc.s    V_21
    IL_0114:  ldloc.s    V_22
    IL_0116:  ldloc.s    V_23
    IL_0118:  ldstr      "Print(object)"
    IL_011d:  ldloca.s   V_2
    IL_011f:  ldfld      int32 Test_variancesmall_il.MyStruct2::n
    IL_0124:  call       int32 Test_variancesmall_il.Program::Verification(string,
                                                     string,
                                                     string,
                                                     string,
                                                     int32)
    IL_0129:  add
    IL_012a:  stloc.0

    IL_0373:  ldloc.0
    IL_0374:  brfalse.s  IL_0379

    IL_0376:  ldc.i4.m1
    IL_0377:  br.s       IL_037b

    IL_0379:  ldc.i4.s   100
    IL_037b:  stloc.s    V_24
    IL_037d:  br.s       IL_037f

    IL_037f:  ldloc.s    V_24
    IL_0381:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
}
