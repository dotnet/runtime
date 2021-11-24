// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//simple switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch1'
{


}
.assembly extern xunit.core {}

.module 'switch1.exe'

.class public auto ansi Test_switch1
       extends ['mscorlib']System.Object
{
  .method private hidebysig static void  DoSwitch(int32 'value') il managed
  {
    .maxstack  2
    .locals (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldloc.0
    IL_0003:  ldc.i4.1
    IL_0004:  sub
    IL_0005:  switch     (
                          IL_0014,
                          IL_0027)
    IL_0012:  br.s       IL_0033

    IL_0014:  ldc.i4.s   100
    IL_0016:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_001b:  ldstr      "Test passed"
    IL_0020:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0025:  br.s       IL_003f

    IL_0027:  ldstr      "Test failed"
    IL_002c:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0031:  br.s       IL_003f

    IL_0033:  ldstr      "Test failed"
    IL_0038:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_003d:  br.s       IL_003f

    IL_003f:  ret
  }

  .method public hidebysig static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  call       void Test_switch1::DoSwitch(int32)
    IL_0006:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }


}
