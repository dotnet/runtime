// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// add and subtract integers
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch2'
{


}
.assembly extern xunit.core {}
.module 'switch2.exe'

.class public auto ansi Test_switch2
       extends ['mscorlib']System.Object
{
  .method private hidebysig static void  DoSwitch(int32 'value') il managed
  {
    .maxstack  2
    .locals (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldc.i4.5
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.3
    IL_0003:  stloc.1
    IL_0004:  ldarg.0
    IL_0005:  stloc.3
    IL_0006:  ldloc.3
    IL_0007:  ldc.i4.1
    IL_0008:  sub
    IL_0009:  switch     (
                          IL_0018,
                          IL_0033)
    IL_0016:  br.s       IL_004d

    IL_0018:  ldloc.0
    IL_0019:  ldloc.1
    IL_001a:  add
    IL_001b:  stloc.2
    IL_001c:  ldloc.2
    IL_001d:  ldc.i4.8
    IL_001e:  bne.un.s   IL_0031

    IL_0020:  ldc.i4.s   100
    IL_0022:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0027:  ldstr      "Test passed"
    IL_002c:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0031:  br.s       IL_005f

    IL_0033:  ldloc.0
    IL_0034:  ldloc.1
    IL_0035:  sub
    IL_0036:  stloc.2
    IL_0037:  ldloc.2
    IL_0038:  ldc.i4.2
    IL_0039:  bne.un.s   IL_004b

    IL_003b:  ldc.i4.1
    IL_003c:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0041:  ldstr      "Test failed"
    IL_0046:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_004b:  br.s       IL_005f

    IL_004d:  ldc.i4.1
    IL_004e:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_0053:  ldstr      "Test failed"
    IL_0058:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_005d:  br.s       IL_005f

    IL_005f:  ret
  }

  .method public hidebysig static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  call       void Test_switch2::DoSwitch(int32)
    IL_0006:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

 }
