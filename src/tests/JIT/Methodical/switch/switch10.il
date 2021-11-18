// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// check inlining
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch10'
{


}
.assembly extern xunit.core {}

.module 'switch10.exe'

.class public auto ansi Test_switch10
       extends ['mscorlib']System.Object
{
  .method private hidebysig static int32
          Square(int32 i) il managed
  {
    .maxstack  2
    .locals (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  mul
    IL_0003:  stloc.0
    IL_0004:  br.s       IL_0006

    IL_0006:  ldloc.0
    IL_0007:  ret
  }

  .method private hidebysig static void  DoSwitch(int32 'value') il managed
  {
    .maxstack  2
    .locals (int32 V_0,
             int32 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  stloc.1
    IL_0004:  ldloc.1
    IL_0005:  ldc.i4.1
    IL_0006:  beq.s      IL_000a

    IL_0008:  br.s       IL_0013

    IL_000a:  ldc.i4.3
    IL_000b:  call       int32 Test_switch10::Square(int32)
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0015

    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.0
    IL_0016:  ldc.i4.s   9
    IL_0018:  bne.un.s   IL_002d

    IL_001a:  ldc.i4.s   100
    IL_001c:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0021:  ldstr      "Test passed"
    IL_0026:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_002b:  br.s       IL_003d

    IL_002d:  ldc.i4.1
    IL_002e:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0033:  ldstr      "Test failed"
    IL_0038:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_003d:  ret
  }

  .method public hidebysig static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  call       void Test_switch10::DoSwitch(int32)
    IL_0006:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }


}
