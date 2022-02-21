// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// checking fallthrough in switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch3'
{


}
.assembly extern xunit.core {}


.class public auto ansi Test_switch3
       extends ['mscorlib']System.Object
{
  .field private static int32 s_exitCode;

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
                          IL_0016)
    IL_0012:  br.s       IL_0029

    IL_0014:  br.s       IL_0016

    IL_0016:  ldc.i4.s   100
    IL_0018:  stsfld     int32 Test_switch3::s_exitCode
    IL_001d:  ldstr      "Test passed"
    IL_0022:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0027:  br.s       IL_003b

    IL_0029:  ldc.i4.1
    IL_002a:  stsfld     int32 Test_switch3::s_exitCode
    IL_002f:  ldstr      "Test failed"
    IL_0034:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0039:  br.s       IL_003b

    IL_003b:  ret
  }

  .method public hidebysig static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  call       void Test_switch3::DoSwitch(int32)
              ldsfld     int32 Test_switch3::s_exitCode
              ret
  }


}
