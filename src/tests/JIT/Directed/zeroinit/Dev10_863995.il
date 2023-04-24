// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// Metadata version: v4.0.21104
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly Dev10_863995
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {264E6DC2-22B6-4B9F-A4E7-582E65FCAB22}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000000260000


// =============== CLASS MEMBERS DECLARATION ===================


.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .field public static bool IsInit
  .field public static bool AllPassed
  .method public hidebysig static void  F(object o) cil managed noinlining
  {
    // 
    .maxstack  24
    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
      IL_0003:  leave      IL_0008

    }  // end handler
    IL_0008:  ret
  } // end of method Program::F

  .method private hidebysig static void  Test8() cil managed noinlining
  {
    // 
    .maxstack  24
    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
      IL_0003:  ldstr      "1"
      IL_0008:  newobj     instance void TwoStringInt::.ctor(string)
      IL_000d:  box        TwoStringInt
      IL_0012:  call       void Program::F(object)
      IL_0017:  ldsfld     bool Program::IsInit
      IL_001c:  brtrue     IL_0027

      IL_0021:  ldc.i4.0
      IL_0022:  stsfld     bool Program::AllPassed
      IL_0027:  leave      IL_002c

    }  // end handler
    IL_002c:  ret
  } // end of method Program::Test8

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // 
    .maxstack  17
    .locals init (int32 V_0)
    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
      IL_001c:  call       void Program::Test8()
      IL_0053:  ldsfld     bool Program::AllPassed
      IL_0058:  brfalse    IL_0072

      IL_005d:  ldstr      "!!!!!!!!!! TEST PASSED !!!!!!!!!!!!!"
      IL_0062:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0067:  ldc.i4.s   100
      IL_0069:  stloc      V_0
      IL_006d:  leave      IL_0087

      IL_0072:  ldstr      "!!!!!!!!!! TEST FAILED !!!!!!!!!!!!!"
      IL_0077:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007c:  ldc.i4.s   101
      IL_007e:  stloc      V_0
      IL_0082:  leave      IL_0087

    }  // end handler
    IL_0087:  ldloc      V_0
    IL_008b:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // 
    .maxstack  24
/*    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
*/      IL_0003:  ldarg.0
      IL_0004:  call       instance void [mscorlib]System.Object::.ctor()
//      IL_0009:  leave      IL_000e

//    }  // end handler
    IL_000e:  ret
  } // end of method Program::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // 
    .maxstack  24
/*    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
*/      IL_0003:  ldc.i4.1
      IL_0004:  stsfld     bool Program::IsInit
      IL_0009:  ldc.i4.1
      IL_000a:  stsfld     bool Program::AllPassed
//      IL_000f:  leave      IL_0014

//    }  // end handler
    IL_0014:  ret
  } // end of method Program::.cctor

} // end of class Program

.class public sequential ansi sealed beforefieldinit TwoStringInt
       extends [mscorlib]System.ValueType
{
  .field public string isString
  .field public int32 isInt
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string i) cil managed noinlining
  {
    // 
    .maxstack  19
/*    .try
    {
      IL_0000:  ldnull
      IL_0001:  throw

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0002:  pop
*/      IL_0003:  ldarg.0
      IL_0004:  ldfld      string TwoStringInt::isString
      IL_0009:  brtrue     IL_001c

      IL_000e:  ldarg.0
      IL_000f:  ldfld      int32 TwoStringInt::isInt
      IL_0014:  ldc.i4.0
      IL_0015:  ceq
      IL_0017:  br         IL_001d

      IL_001c:  ldc.i4.0
      IL_001d:  stsfld     bool Program::IsInit
      IL_0022:  ldstr      "TwoStringInt: isString: {0} isInt: {1}"
      IL_0027:  ldarg.0
      IL_0028:  ldfld      string TwoStringInt::isString
      IL_002d:  brfalse    IL_003d

      IL_0032:  ldarg.0
      IL_0033:  ldfld      string TwoStringInt::isString
      IL_0038:  br         IL_0042

      IL_003d:  ldstr      "<null>"
      IL_0042:  ldarg.0
      IL_0043:  ldfld      int32 TwoStringInt::isInt
      IL_0048:  box        [mscorlib]System.Int32
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object,
                                                                    object)
//      IL_0052:  leave      IL_0057

//    }  // end handler
    IL_0057:  ret
  } // end of method TwoStringInt::.ctor

} // end of class TwoStringInt



// =============================================================

// 
