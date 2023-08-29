// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}





// Metadata version: v2.0.20910
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'ddb148379'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {29D488A1-3C00-4F7C-86C0-B123ECE8AAA6}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x002E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IMyInterface`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance int32  foo<U>() cil managed
  {
  } // end of method IMyInterface`1::foo

} // end of class IMyInterface`1

.class private sequential ansi sealed beforefieldinit MyStruct`1<T>
       extends [mscorlib]System.ValueType
       implements class IMyInterface`1<!T>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual final 
          instance int32  foo<U>() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldtoken    class [mscorlib]System.Collections.Generic.Dictionary`2<!T,!!U>
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              dup
    IL_000a:  call       void [System.Console]System.Console::WriteLine(object)
              callvirt   instance string [mscorlib]System.Object::ToString()
              ldstr  "System.Collections.Generic.Dictionary`2[System.String,System.Object]"
              call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
              ldc.i4.0
              beq        IL_000f
              ldstr "FAIL"
              call void [System.Console]System.Console::WriteLine(string)
              ldc.i4 99
              ret
    IL_000f:  ldc.i4 100
              ret
  } // end of method MyStruct`1::foo

} // end of class MyStruct`1

.class private sequential ansi sealed beforefieldinit MyStruct2`1<T>
       extends [mscorlib]System.ValueType
       implements class IMyInterface`1<!T>
{
  .pack 0
  .size 1
  .method public hidebysig newslot virtual final 
          instance int32  foo<U>() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldtoken    class [mscorlib]System.Collections.Generic.Dictionary`2<!!U,!T>
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
              dup
    IL_000a:  call       void [System.Console]System.Console::WriteLine(object)
              callvirt   instance string [mscorlib]System.Object::ToString()
              ldstr  "System.Collections.Generic.Dictionary`2[System.Object,System.String]"
              call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
              ldc.i4.0
              beq        IL_000f
              ldstr "FAIL"
              call void [System.Console]System.Console::WriteLine(string)
              ldc.i4 99
              ret
             
    IL_000f:  ldc.i4 100
              ret
    
  } // end of method MyStruct2`1::foo

} // end of class MyStruct2`1

.class public auto ansi beforefieldinit My
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       35 (0x23)
    .maxstack  8
    .locals init (valuetype MyStruct`1<string> V_0,
             valuetype MyStruct2`1<string> V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    valuetype MyStruct`1<string>
    IL_0008:  ldloca.s   V_0
    IL_000a:  constrained. valuetype MyStruct`1<string>
    IL_0010:  callvirt   instance int32 class IMyInterface`1<string>::foo<object>()
              ldc.i4 100
              beq IL_0015
              ldc.i4 99
              br IL_0022
    IL_0015:  ldloca.s   V_1
    IL_0017:  constrained. valuetype MyStruct2`1<string>
    IL_001d:  callvirt   instance int32 class IMyInterface`1<string>::foo<object>()
              ldc.i4 100
              beq IL_0020
              ldc.i4 99
              br.s IL_0022
    IL_0020:  ldc.i4 100
    IL_0022:  ret
  } // end of method My::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method My::.ctor

} // end of class My


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
