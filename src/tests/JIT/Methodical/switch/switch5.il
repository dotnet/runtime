// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Stack implementation
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch5'
{


}
.assembly extern xunit.core {}

.module 'switch5.exe'

.class public auto ansi TestStack
       extends ['mscorlib']System.Object
{
  .field public int32[] Stack
  .field public int32 top
  .method public hidebysig instance void
          DoSwitch(int32 'value') il managed
  {
    .maxstack  2
    .locals (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  stloc.1
    IL_0002:  ldloc.1
    IL_0003:  ldc.i4.1
    IL_0004:  sub
    IL_0005:  switch     (
                          IL_0014,
                          IL_001d)
    IL_0012:  br.s       IL_0026

    IL_0014:  ldarg.0
    IL_0015:  ldc.i4.5
    IL_0016:  call       instance void TestStack::push(int32)
    IL_001b:  br.s       IL_0063

    IL_001d:  ldarg.0
    IL_001e:  call       instance int32 TestStack::'pop'()
    IL_0023:  stloc.0
    IL_0024:  br.s       IL_0063

    IL_0026:  ldarg.0
    IL_0027:  ldfld      int32[] TestStack::Stack
    IL_002c:  ldarg.0
    IL_002d:  ldfld      int32 TestStack::top
    IL_0032:  ldelem.i4
    IL_0033:  ldc.i4.5
    IL_0034:  bne.un.s   IL_0051

    IL_0036:  ldarg.0
    IL_0037:  ldfld      int32 TestStack::top
    IL_003c:  brtrue.s   IL_0051

    IL_003e:  ldc.i4.s   100
    IL_0040:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0045:  ldstr      "Test passed"
    IL_004a:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_004f:  br.s       IL_0061

    IL_0051:  ldc.i4.1
    IL_0052:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0057:  ldstr      "Test failed"
    IL_005c:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0061:  br.s       IL_0063

    IL_0063:  ret
  }

  .method public hidebysig instance void
          push(int32 i) il managed
  {
    .maxstack  4
    .locals (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32[] TestStack::Stack
    IL_0006:  ldarg.0
    IL_0007:  dup
    IL_0008:  ldfld      int32 TestStack::top
    IL_000d:  ldc.i4.1
    IL_000e:  add
    IL_000f:  dup
    IL_0010:  stloc.0
    IL_0011:  stfld      int32 TestStack::top
    IL_0016:  ldloc.0
    IL_0017:  ldarg.1
    IL_0018:  stelem.i4
    IL_0019:  ret
  }

  .method public hidebysig instance int32
          'pop'() il managed
  {
    .maxstack  4
    .locals (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32[] TestStack::Stack
    IL_0006:  ldarg.0
    IL_0007:  dup
    IL_0008:  ldfld      int32 TestStack::top
    IL_000d:  dup
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.1
    IL_0010:  sub
    IL_0011:  stfld      int32 TestStack::top
    IL_0016:  ldloc.1
    IL_0017:  ldelem.i4
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void .ctor() il managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.5
    IL_0002:  newarr     ['mscorlib']System.Int32
    IL_0007:  stfld      int32[] TestStack::Stack
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.m1
    IL_000e:  stfld      int32 TestStack::top
    IL_0013:  ldarg.0
    IL_0014:  call       instance void ['mscorlib']System.Object::.ctor()
    IL_0019:  ret
  }

}

.class public auto ansi Test_switch5
       extends ['mscorlib']System.Object
{
  .method public hidebysig static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals (class TestStack V_0)
    IL_0000:  newobj     instance void TestStack::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.1
    IL_0008:  call       instance void TestStack::DoSwitch(int32)
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.1
    IL_000f:  call       instance void TestStack::DoSwitch(int32)
    IL_0014:  ldloc.0
    IL_0015:  ldc.i4.2
    IL_0016:  call       instance void TestStack::DoSwitch(int32)
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.3
    IL_001d:  call       instance void TestStack::DoSwitch(int32)
    IL_0022:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

}
