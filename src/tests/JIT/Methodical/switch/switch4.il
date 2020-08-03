// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// loop in switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch4'
{


}

.module 'switch4.exe'

.class public auto ansi Test
       extends ['mscorlib']System.Object
{
  .field public static int32 count
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

    IL_0008:  br.s       IL_0025

    IL_000a:  ldloc.0
    IL_000b:  pop
    IL_000c:  br.s       IL_001e

    IL_000e:  ldsfld     int32 Test::count
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  stsfld     int32 Test::count
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  add
    IL_001d:  stloc.0
    IL_001e:  ldloc.0
    IL_001f:  ldc.i4.s   10
    IL_0021:  blt.s      IL_000e

    IL_0023:  br.s       IL_0027

    IL_0025:  br.s       IL_0027

    IL_0027:  ldsfld     int32 Test::count
    IL_002c:  ldc.i4.s   10
    IL_002e:  bne.un.s   IL_0043

    IL_0030:  ldc.i4.s   100
    IL_0032:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0037:  ldstr      "Test passed"
    IL_003c:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0041:  br.s       IL_0053

    IL_0043:  ldc.i4.1
    IL_0044:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0049:  ldstr      "Test failed"
    IL_004e:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0053:  ret
  }

  .method public hidebysig static void Main() il managed
  {
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  call       void Test::DoSwitch(int32)
    IL_0006:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

}
