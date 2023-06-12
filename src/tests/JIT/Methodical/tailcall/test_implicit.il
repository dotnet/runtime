// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly ASSEMBLY_NAME
{


  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001



.namespace Test_test_implicit_il
{
.class public auto ansi beforefieldinit Class1
       extends [mscorlib]System.Object
{
  .field private static bool s_failure
  .field private static int32 MaxDepth
  .field private int32 'value'
  .method public hidebysig instance int32
          Recurse1(int32 depth) cil managed
  {
    .maxstack  5
    .locals init (int32 V_0)
              ldsfld     bool Test_test_implicit_il.Class1::s_failure
              brtrue.s   IL_0003

    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_004b

    IL_000c:  ldarg.0
    IL_000d:  dup
    IL_000e:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0013:  ldarg.1
    IL_0014:  add
    IL_0015:  stfld      int32 Test_test_implicit_il.Class1::'value'
    IL_001a:  ldarg.1
    IL_001b:  ldc.i4     0x80
    IL_0020:  rem
    IL_0021:  ldc.i4.s   43
    IL_0023:  bne.un.s   IL_0031

    IL_0025:  ldarg.0
    IL_0026:  ldarg.1
    IL_0027:  ldc.i4.1
    IL_0028:  sub
    IL_0029:  tail. callvirt   instance int32 Test_test_implicit_il.Class1::Recurse3(int32)
ret
    IL_002e:  stloc.0
    IL_002f:  br.s       IL_004b

    IL_0031:  ldarg.0
    IL_0032:  ldarg.1
    IL_0033:  ldc.i4.1
    IL_0034:  sub
    IL_0035:  ldarg.1
    IL_0036:  box        [mscorlib]System.Int32
    IL_003b:  ldarg.1
    IL_003c:  ldc.i4.1
    IL_003d:  add
    IL_003e:  box        [mscorlib]System.Int32
    IL_0043:  tail. call       instance int32 Test_test_implicit_il.Class1::Recurse2(int32,
                                                         object,
                                                         object)
ret
    IL_0048:  stloc.0
    IL_0049:  br.s       IL_004b

    IL_004b:  ldloc.0
    IL_004c:  ret
  }

  .method public hidebysig instance int32
          Recurse2(int32 depth,
                   object o1,
                   object o2) cil managed
  {
    .maxstack  4
    .locals init (string V_0,
             int32 V_1)
              ldsfld     bool Test_test_implicit_il.Class1::s_failure
              brtrue.s   IL_0003

    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_007b

    IL_000c:  ldarg.1
    IL_000d:  ldsfld     int32 Test_test_implicit_il.Class1::MaxDepth
    IL_0012:  ldc.i4.s   10
    IL_0014:  div
    IL_0015:  rem
    IL_0016:  ldc.i4.s   100
    IL_0018:  bne.un.s   IL_0053

    IL_001a:  call       string [System.Runtime.Extensions]System.Environment::get_StackTrace()
    IL_001f:  stloc.0
    IL_0020:  ldloc.0
    IL_0021:  ldstr      "Main"
    IL_0026:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_002b:  ldc.i4.m1
    IL_002c:  bne.un.s   IL_004e

    IL_002e:  ldstr      "Unexpected stack trace: "
    IL_0033:  ldloc.0
    IL_0034:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003e:  ldstr      "Test Failed"
    IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0048:  ldc.i4.1
              stsfld     bool Test_test_implicit_il.Class1::s_failure
              br.s       IL_0003

    IL_004e:  call       void [mscorlib]System.GC::Collect()
    IL_0053:  ldarg.0
    IL_0054:  dup
    IL_0055:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_005a:  ldarg.2
    IL_005b:  unbox      [mscorlib]System.Int32
    IL_0060:  ldind.i4
    IL_0061:  ldarg.3
    IL_0062:  unbox      [mscorlib]System.Int32
    IL_0067:  ldind.i4
    IL_0068:  add
    IL_0069:  add
    IL_006a:  stfld      int32 Test_test_implicit_il.Class1::'value'
    IL_006f:  ldarg.0
    IL_0070:  ldarg.1
    IL_0071:  ldc.i4.1
    IL_0072:  sub
    IL_0073:  tail. callvirt   instance int32 Test_test_implicit_il.Class1::Recurse3(int32)
ret
    IL_0078:  stloc.1
    IL_0079:  br.s       IL_007b

    IL_007b:  ldloc.1
    IL_007c:  ret
  }

  .method public hidebysig newslot virtual
          instance int32  Recurse3(int32 depth) cil managed
  {
    .maxstack  5
    .locals init (int32 V_0)
              ldsfld     bool Test_test_implicit_il.Class1::s_failure
              brtrue.s   IL_0003

    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_003f

    IL_000c:  ldarg.1
    IL_000d:  ldc.i4     0x80
    IL_0012:  rem
    IL_0013:  ldc.i4.s   21
    IL_0015:  bne.un.s   IL_0023

    IL_0017:  ldarg.0
    IL_0018:  ldarg.1
    IL_0019:  ldc.i4.1
    IL_001a:  sub
    IL_001b:  tail. call       instance int32 Test_test_implicit_il.Class1::Recurse1(int32)
ret
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_003f

    IL_0023:  ldarg.0
    IL_0024:  ldarg.1
    IL_0025:  ldc.i4.1
    IL_0026:  sub
    IL_0027:  ldarg.1
    IL_0028:  ldc.i4.1
    IL_0029:  add
    IL_002a:  box        [mscorlib]System.Int32
    IL_002f:  ldarg.1
    IL_0030:  ldc.i4.2
    IL_0031:  add
    IL_0032:  box        [mscorlib]System.Int32
    IL_0037:  tail. callvirt   instance int32 Test_test_implicit_il.Class1::Recurse4(int32,
                                                         object,
                                                         object)
ret
    IL_003c:  stloc.0
    IL_003d:  br.s       IL_003f

    IL_003f:  ldloc.0
    IL_0040:  ret
  }

  .method public hidebysig newslot virtual
          instance int32  Recurse4(int32 depth,
                                   object o1,
                                   object o2) cil managed
  {
    .maxstack  4
    .locals init (string V_0,
             int32 V_1)
              ldsfld     bool Test_test_implicit_il.Class1::s_failure
              brtrue.s   IL_0003

    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_007e

    IL_000c:  ldarg.1
    IL_000d:  ldsfld     int32 Test_test_implicit_il.Class1::MaxDepth
    IL_0012:  ldc.i4.s   10
    IL_0014:  div
    IL_0015:  rem
    IL_0016:  ldc.i4     0xc8
    IL_001b:  bne.un.s   IL_0056

    IL_001d:  call       void [mscorlib]System.GC::Collect()
    IL_0022:  call       string [System.Runtime.Extensions]System.Environment::get_StackTrace()
    IL_0027:  stloc.0
    IL_0028:  ldloc.0
    IL_0029:  ldstr      "Main"
    IL_002e:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0033:  ldc.i4.m1
    IL_0034:  bne.un.s   IL_0056

    IL_0036:  ldstr      "Unexpected stack trace: "
    IL_003b:  ldloc.0
    IL_003c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  ldstr      "Test Failed"
    IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0050:  ldc.i4.1
              stsfld     bool Test_test_implicit_il.Class1::s_failure
              br.s       IL_0003

    IL_0056:  ldarg.0
    IL_0057:  dup
    IL_0058:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_005d:  ldarg.2
    IL_005e:  unbox      [mscorlib]System.Int32
    IL_0063:  ldind.i4
    IL_0064:  ldarg.3
    IL_0065:  unbox      [mscorlib]System.Int32
    IL_006a:  ldind.i4
    IL_006b:  add
    IL_006c:  add
    IL_006d:  stfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0072:  ldarg.0
    IL_0073:  ldarg.1
    IL_0074:  ldc.i4.1
    IL_0075:  sub
    IL_0076:  tail. call       instance int32 Test_test_implicit_il.Class1::Recurse1(int32)
ret
    IL_007b:  stloc.1
    IL_007c:  br.s       IL_007e

    IL_007e:  ldloc.1
    IL_007f:  ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class Test_test_implicit_il.Class1 V_0,
             int32 V_1,
             int32 V_2,
             object[] V_3)
    IL_0000:  ldstr      "Test Start"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  newobj     instance void Test_test_implicit_il.Class1::.ctor()
    IL_000f:  stloc.0
    IL_0010:  ldloc.0
    IL_0011:  ldsfld     int32 Test_test_implicit_il.Class1::MaxDepth
    IL_0016:  callvirt   instance int32 Test_test_implicit_il.Class1::Recurse1(int32)
    IL_001b:  pop
              ldsfld     bool Test_test_implicit_il.Class1::s_failure
              brtrue.s   IL_0060

    IL_001c:  ldc.i4     41963520
    IL_001d:  stloc.1
    IL_001e:  ldloc.0
    IL_001f:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_0024:  ldloc.1
    IL_0025:  beq.s      IL_006e

    IL_0027:  ldc.i4.4
    IL_0028:  newarr     [mscorlib]System.Object
    IL_002d:  stloc.3
    IL_002e:  ldloc.3
    IL_002f:  ldc.i4.0
    IL_0030:  ldstr      "Expected result: "
    IL_0035:  stelem.ref
    IL_0036:  ldloc.3
    IL_0037:  ldc.i4.1
    IL_0038:  ldc.i4.1
    IL_0039:  box        [mscorlib]System.Int32
    IL_003e:  stelem.ref
    IL_003f:  ldloc.3
    IL_0040:  ldc.i4.2
    IL_0041:  ldstr      " Actual result: "
    IL_0046:  stelem.ref
    IL_0047:  ldloc.3
    IL_0048:  ldc.i4.3
    IL_0049:  ldloc.0
    IL_004a:  ldfld      int32 Test_test_implicit_il.Class1::'value'
    IL_004f:  box        [mscorlib]System.Int32
    IL_0054:  stelem.ref
    IL_0055:  ldloc.3
    IL_0056:  call       string [mscorlib]System.String::Concat(object[])
    IL_005b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0060:  ldstr      "Test Failed"
    IL_0065:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006a:  ldc.i4.0
    IL_006b:  stloc.2
    IL_006c:  br.s       IL_007d

    IL_006e:  ldstr      "Test Passed"
    IL_0073:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0078:  ldc.i4.s   100
    IL_007a:  stloc.2
    IL_007b:  br.s       IL_007d

    IL_007d:  ldloc.2
    IL_007e:  ret
  }

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4     0x2000
    IL_0005:  stsfld     int32 Test_test_implicit_il.Class1::MaxDepth
    IL_000a:  ret
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
