// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// try --  catch in switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }
.assembly extern System.Console { auto }

.assembly 'switch6'
{


}

.module 'switch6.exe'

.class public auto ansi Test
       extends ['mscorlib']System.Object
{
  .method private hidebysig static void  DoSwitch(int32 'value') il managed
  {
    .maxstack  2
    .locals (int32[] V_0,
             int32 V_1,
             class ['mscorlib']System.Exception V_2,
             int32 V_3)
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     ['mscorlib']System.Int32
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.0
    IL_0008:  stloc.1
    IL_0009:  ldarg.0
    IL_000a:  stloc.3
    IL_000b:  ldloc.3
    IL_000c:  ldc.i4.1
    IL_000d:  beq.s      IL_0011

    IL_000f:  br.s       IL_002a

    .try
    {
      IL_0011:  ldloc.0
      IL_0012:  ldc.i4.4
      IL_0013:  ldelem.i4
      IL_0014:  stloc.1
      IL_0015:  leave.s    IL_0028

    }
    catch ['mscorlib']System.Exception
    {
      IL_0017:  stloc.2
      IL_0018:  ldstr      "Exception {0} caught"
      IL_001d:  ldloc.2
      IL_001e:  call       void [System.Console]System.Console::WriteLine(class System.String,
                                                                      class System.Object)
      IL_0023:  ldc.i4.s   100
      IL_0025:  stloc.1
      IL_0026:  leave.s    IL_0028

    }
    IL_0028:  br.s       IL_002c

    IL_002a:  br.s       IL_002c

    IL_002c:  ldloc.1
    IL_002d:  ldc.i4.s   100
    IL_002f:  bne.un.s   IL_0044

    IL_0031:  ldc.i4.s   100
    IL_0033:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0038:  ldstr      "Test passed"
    IL_003d:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0042:  br.s       IL_0054

    IL_0044:  ldc.i4.1
    IL_0045:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_004a:  ldstr      "Test failed"
    IL_004f:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0054:  ret
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
