
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly genericmethods
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module genericmethods.exe
// MVID: {62CAFB9A-4CDB-4A62-8A4F-DC7648609070}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01790000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.Type 
          Foo<T>() cil managed
  {
    // Code size       37 (0x25)
    .maxstack  2
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo<T>::Foo<T>: TypeOf(T) = {0}"
    IL_0006:  ldtoken    !!T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldtoken    !!T
    IL_001b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.0
    IL_0024:  ret
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IBar`1<T>
{
  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.Type 
          Bar1<P>() cil managed
  {
    // Code size       37 (0x25)
    .maxstack  2
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar<T>::Foo<P>: TypeOf(P) = {0}"
    IL_0006:  ldtoken    !!P
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldtoken    !!P
    IL_001b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.0
    IL_0024:  ret    
  } // end of method IBar`1::Bar1

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.Type 
          Bar2<K>() cil managed
  {
    // Code size       37 (0x25)
    .maxstack  2
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar<T>::Bar2<K>: TypeOf(K) = {0}"
    IL_0006:  ldtoken    !!K
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldtoken    !!K
    IL_001b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.0
    IL_0024:  ret  
  } // end of method IBar`1::Bar2

  .method public hidebysig newslot virtual 
          instance void  Bar3<P,K>([out] class [mscorlib]System.Type& t,
                                   [out] class [mscorlib]System.Type& u) cil managed
  {
    // Code size       57 (0x39)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar<T>::Bar3<P, K>: TypeOf(P) = {0}, TypeOf(K)"
    + " = {1}"
    IL_0006:  ldtoken    !!P
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  ldtoken    !!K
    IL_0015:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001a:  call       void [mscorlib]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ldarg.1
    IL_0021:  ldtoken    !!P
    IL_0026:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_002b:  stind.ref
    IL_002c:  ldarg.2
    IL_002d:  ldtoken    !!K
    IL_0032:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0037:  stind.ref
    IL_0038:  ret    
  } // end of method IBar`1::Bar3

} // end of class IBar`1

.class private auto ansi beforefieldinit FooBar`1<V>
       extends [mscorlib]System.Object
       implements IFoo,
                  class IBar`1<!V>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method FooBar`1::.ctor

} // end of class FooBar`1

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       223 (0xdf)
    .maxstack  3
    .locals init (class FooBar`1<object> V_0,
             class IFoo V_1,
             class IBar`1<object> V_2,
             class [mscorlib]System.Type V_3,
             class [mscorlib]System.Type V_4,
             int32 V_5)
    IL_0000:  nop
    IL_0001:  newobj     instance void class FooBar`1<object>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.0
    IL_000a:  stloc.2
    IL_000b:  ldstr      "Calling IFoo.Foo<String> on FooBar<Object> - expec"
    + "ting IFoo::Foo<string>() returning typeof(string)"
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0015:  nop
    IL_0016:  ldloc.1
    IL_0017:  callvirt   instance class [mscorlib]System.Type IFoo::Foo<string>()
    IL_001c:  ldtoken    [mscorlib]System.String
    IL_0021:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0026:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_002b:  ldstr      "Calling IFoo.Foo<String> on FooBar<Object>"
    IL_0030:  call       void Test::Assert(bool,
                                           string)
    IL_0035:  nop
    IL_0036:  ldstr      "Calling IBar.Bar1<String> on FooBar<object> - expe"
    + "cting bar.Bar1<string>() returning typeof(string)"
    IL_003b:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0040:  nop
    IL_0041:  ldloc.2
    IL_0042:  callvirt   instance class [mscorlib]System.Type class IBar`1<object>::Bar1<string>()
    IL_0047:  ldtoken    [mscorlib]System.String
    IL_004c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0051:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_0056:  ldstr      "Calling IBar.Bar1<String> on FooBar<object>"
    IL_005b:  call       void Test::Assert(bool,
                                           string)
    IL_0060:  nop
    IL_0061:  ldstr      "Calling IBar.Bar2<String[]> on FooBar<object> - ex"
    + "pecting bar.Bar2<string[]>() returning typeof(string[])"
    IL_0066:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_006b:  nop
    IL_006c:  ldloc.2
    IL_006d:  callvirt   instance class [mscorlib]System.Type class IBar`1<object>::Bar2<string[]>()
    IL_0072:  ldtoken    string[]
    IL_0077:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_007c:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_0081:  ldstr      "Calling IBar.Bar2<String[]> on FooBar<object>"
    IL_0086:  call       void Test::Assert(bool,
                                           string)
    IL_008b:  nop
    IL_008c:  ldstr      "Calling IBar.Bar3<String, String[]> - expecting ba"
    + "r.Bar3<string>() returning typeof(string), typeof(string[])"
    IL_0091:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0096:  nop
    IL_0097:  ldloc.2
    IL_0098:  ldloca.s   V_3
    IL_009a:  ldloca.s   V_4
    IL_009c:  callvirt   instance void class IBar`1<object>::Bar3<string,string[]>(class [mscorlib]System.Type&,
                                                                                   class [mscorlib]System.Type&)
    IL_00a1:  nop
    IL_00a2:  ldloc.3
    IL_00a3:  ldtoken    [mscorlib]System.String
    IL_00a8:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00ad:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_00b2:  brfalse.s  IL_00c7

    IL_00b4:  ldloc.s    V_4
    IL_00b6:  ldtoken    string[]
    IL_00bb:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00c0:  call       bool [mscorlib]System.Type::op_Equality(class [mscorlib]System.Type,
                                                                 class [mscorlib]System.Type)
    IL_00c5:  br.s       IL_00c8

    IL_00c7:  ldc.i4.0
    IL_00c8:  ldstr      "Calling IBar.Bar3<String, String[]>"
    IL_00cd:  call       void Test::Assert(bool,
                                           string)
    IL_00d2:  nop
    IL_00d3:  call       int32 Test::Ret()
    IL_00d8:  stloc.s    V_5
    IL_00da:  br.s       IL_00dc

    IL_00dc:  ldloc.s    V_5
    IL_00de:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field private static bool Pass
  .method public hidebysig static int32  Ret() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     bool Test::Pass
    IL_0006:  brtrue.s   IL_000c

    IL_0008:  ldc.i4.s   101
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   0
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Test::Ret

  .method public hidebysig static void  Assert(bool cond,
                                               string msg) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0015

    IL_0006:  nop
    IL_0007:  ldstr      "PASS"
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  br.s       IL_002e

    IL_0015:  nop
    IL_0016:  ldstr      "FAIL: "
    IL_001b:  ldarg.1
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.0
    IL_0028:  stsfld     bool Test::Pass
    IL_002d:  nop
    IL_002e:  ret
  } // end of method Test::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test::Pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file genericmethods.res
