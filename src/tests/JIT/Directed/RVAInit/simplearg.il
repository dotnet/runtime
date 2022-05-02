// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a repro case for GitHub issue #9401 ( https://github.com/dotnet/runtime/issues/9401 )

// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly 'simplearg'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {07A1F03C-A750-4BCF-8997-C0AC2CBDB844}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001BBF9F70000


// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed SimpleArg.MyValue
       extends [mscorlib]System.ValueType
{
  .field public int32 IntValue
  .field public int32 PadValue
  .field public int64 LongValue
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 init1,
                               int64 init2) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 SimpleArg.MyValue::IntValue
    IL_0007:  ldarg.0
    IL_0008:  ldc.i4.0
    IL_0009:  stfld      int32 SimpleArg.MyValue::PadValue
    IL_000e:  ldarg.0
    IL_000f:  ldarg.2
    IL_0010:  stfld      int64 SimpleArg.MyValue::LongValue
    IL_0015:  ret
  } // end of method MyValue::.ctor

} // end of class SimpleArg.MyValue

.class private auto ansi SimpleArg.Holder
       extends [mscorlib]System.Object
{
  .field public static valuetype SimpleArg.MyValue RvaStatic at PreInitValue   // {31, 0, 12 }
  .field public static valuetype SimpleArg.MyValue NormalStatic                // {69, 0, 11 }
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Holder::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.s   69
    IL_0002:  ldc.i4.s   11
    IL_0004:  conv.i8
    IL_0005:  newobj     instance void SimpleArg.MyValue::.ctor(int32,
                                                                int64)
    IL_000a:  stsfld     valuetype SimpleArg.MyValue SimpleArg.Holder::NormalStatic
    IL_000f:  ret
  } // end of method Holder::.cctor

} // end of class SimpleArg.Holder

.class private auto ansi SimpleArg.Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Method(valuetype SimpleArg.MyValue arg) cil managed noinlining
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarga.s   arg
    IL_0002:  ldfld      int32 SimpleArg.MyValue::IntValue
    IL_0007:  ret
  } // end of method Program::Method

  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       59 (0x3b)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldsfld     valuetype SimpleArg.MyValue SimpleArg.Holder::RvaStatic
    IL_0005:  call       int32 SimpleArg.Program::Method(valuetype SimpleArg.MyValue)
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  ldsfld     valuetype SimpleArg.MyValue SimpleArg.Holder::NormalStatic
    IL_0011:  call       int32 SimpleArg.Program::Method(valuetype SimpleArg.MyValue)
    IL_0016:  add
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  ldc.i4.s   100
    IL_001b:  bne.un.s   IL_0029

    IL_001d:  ldstr      "Passed"
    IL_0022:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0027:  br.s       IL_0039

    IL_0029:  ldstr      "Failed"
    IL_002e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0033:  ldloc.0
    IL_0034:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0039:  ldloc.0
    IL_003a:  ret
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

} // end of class SimpleArg.Program

.data PreInitValue = bytearray(1F 00 00 00
                               00 00 00 00
                               0C 00 00 00 00 00 00 00)

// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file repro2.res
