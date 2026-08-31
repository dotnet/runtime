// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (0B D1 C7 0E 65 8B 67 F1 ED 21 D0 6D D4 DD 89 7A   // ....e.g..!.m...z
           37 E9 11 BE )                                     // 7...
  .ver 2:0:3600:0
}
.assembly 'vsw188290'
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 )

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {31E86D7D-808F-4F1C-A755-94B3084C6466}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03FA0000


// =============== CLASS MEMBERS DECLARATION ===================

.class sealed private auto ansi beforefieldinit C<([mscorlib]System.Object) T>
       extends [mscorlib]System.ValueType
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       75 (0x4b)
    .maxstack  4
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_vsw188290::run
    IL_000c:  ldtoken    !0
    IL_0011:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0016:  ldsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_001b:  beq.s      IL_004a

    IL_001d:  ldstr      "C.typeof, typeof(T) = "
    IL_0022:  ldtoken    !0
    IL_0027:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_002c:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0031:  ldstr      ", Test.t = "
    IL_0036:  ldsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_003b:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_0040:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string,
                                                                string)
    IL_0045:  call       void Test_vsw188290::fail(string)
    IL_004a:  ret
  } // end of method C::.ctor

} // end of valuetype C

.class public auto ansi beforefieldinit Test_vsw188290
       extends [mscorlib]System.Object
{
  .field public static class [mscorlib]System.Type t
  .field public static bool run
  .field private static int32 failures
  .method public hidebysig static void  fail(string msg) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldstr      "Failure: {0}"
    IL_0005:  ldarg.0
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_000b:  ldsfld     int32 Test_vsw188290::failures
    IL_0010:  ldc.i4.1
    IL_0011:  add
    IL_0012:  stsfld     int32 Test_vsw188290::failures
    IL_0017:  ret
  } // end of method Test::fail

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       247 (0xf7)
    .maxstack  5
    .locals init (valuetype C<int32>[] V_0,
             valuetype C<string>[] V_1,
             valuetype C<string[]>[] V_2,
             valuetype C<object>[] V_3,
             int32 V_4)

    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     bool Test_vsw188290::run
    IL_0006:  ldtoken    [mscorlib]System.Int32
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  stsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_0015:  ldc.i4.1
    IL_0016:  newarr     valuetype C<int32>
    IL_001b:  stloc.0
    IL_001c:  ldloc.0
    IL_001d:  callvirt   instance void [mscorlib]System.Array::Initialize()
    IL_0022:  ldsfld     bool Test_vsw188290::run
    IL_0027:  brtrue.s   IL_0033
    IL_0029:  ldstr      "constructor not run"
    IL_002e:  call       void Test_vsw188290::fail(string)



    IL_0033:   ldstr      "C<int> passed"  call void [System.Console]System.Console::WriteLine(string)

               ldc.i4.0
    IL_0034:  stsfld     bool Test_vsw188290::run
    IL_0039:  ldtoken    [mscorlib]System.String
    IL_003e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0043:  stsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_0048:  ldc.i4.1
    IL_0049:  newarr     valuetype C<string>
    IL_004e:  stloc.1
    IL_004f:  ldloc.1
    IL_0050:  callvirt   instance void [mscorlib]System.Array::Initialize()
    IL_0055:  ldsfld     bool Test_vsw188290::run
    IL_005a:  brtrue.s   IL_0066

    IL_005c:  ldstr      "constructor not run"
    IL_0061:  call       void Test_vsw188290::fail(string)
    IL_0066:   ldstr      "C<string> passed" call void [System.Console]System.Console::WriteLine(string)

               ldc.i4.0
    IL_0067:  stsfld     bool Test_vsw188290::run
    IL_006c:  ldtoken    string[]
    IL_0071:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0076:  stsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_007b:  ldc.i4.1
    IL_007c:  newarr     valuetype C<string[]>
    IL_0081:  stloc.2
    IL_0082:  ldloc.2
    IL_0083:  callvirt   instance void [mscorlib]System.Array::Initialize()
    IL_0088:  ldsfld     bool Test_vsw188290::run
    IL_008d:  brtrue.s   IL_0099

    IL_008f:  ldstr      "constructor not run"
    IL_0094:  call       void Test_vsw188290::fail(string)
    IL_0099:  ldstr      "C<string[]> passed" call void [System.Console]System.Console::WriteLine(string)

               ldc.i4.0
    IL_009a:  stsfld     bool Test_vsw188290::run
    IL_009f:  ldtoken    [mscorlib]System.Object
    IL_00a4:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00a9:  stsfld     class [mscorlib]System.Type Test_vsw188290::t
    IL_00ae:  ldc.i4.1
    IL_00af:  newarr     valuetype C<object>
    IL_00b4:  stloc.3
    IL_00b5:  ldloc.3
    IL_00b6:  callvirt   instance void [mscorlib]System.Array::Initialize()
    IL_00bb:  ldsfld     bool Test_vsw188290::run
    IL_00c0:  brtrue.s   IL_00cc

    IL_00c2:  ldstr      "constructor not run"
    IL_00c7:  call       void Test_vsw188290::fail(string)
    IL_00cc:  ldstr      "C<object> passed" call void [System.Console]System.Console::WriteLine(string)

               ldsfld     int32 Test_vsw188290::failures
    IL_00d1:  ldc.i4.0
    IL_00d2:  ble.s      IL_00e4

    IL_00d4:  ldstr      "Test Failed"
    IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00de:  ldc.i4.s   99
    IL_00e0:  stloc.s    V_4
    IL_00e2:  br.s       IL_00f4

    IL_00e4:  ldstr      "Test Passed"
    IL_00e9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ee:  ldc.i4.s   100
    IL_00f0:  stloc.s    V_4
    IL_00f2:  br.s       IL_00f4

    IL_00f4:  ldloc.s    V_4
    IL_00f6:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_vsw188290::failures
    IL_0006:  ret
  } // end of method Test::.cctor

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

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file initialize-struct.res
