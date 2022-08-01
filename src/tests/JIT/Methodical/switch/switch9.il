// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//Nested Switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch9'
{


}
.assembly extern xunit.core {}


.class public auto ansi Test_switch9
       extends ['mscorlib']System.Object
{
  .field private static int32 s_exitCode;

  .method private hidebysig static void  DoSwitch(int32 val1,
                                                  int32 val2) il managed
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
                          IL_0061)
    IL_0012:  br.s       IL_0073

    IL_0014:  ldarg.1
    IL_0015:  stloc.0
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  switch     (
                          IL_0028,
                          IL_003b)
    IL_0026:  br.s       IL_004d

    IL_0028:  ldc.i4.s   100
    IL_002a:  stsfld     int32 Test_switch9::s_exitCode
    IL_002f:  ldstr      "Test passed"
    IL_0034:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0039:  br.s       IL_005f

    IL_003b:  ldc.i4.1
    IL_003c:  stsfld     int32 Test_switch9::s_exitCode
    IL_0041:  ldstr      "Test failed"
    IL_0046:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_004b:  br.s       IL_005f

    IL_004d:  ldc.i4.1
    IL_004e:  stsfld     int32 Test_switch9::s_exitCode
    IL_0053:  ldstr      "Test failed"
    IL_0058:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_005d:  br.s       IL_005f

    IL_005f:  br.s       IL_0085

    IL_0061:  ldc.i4.1
    IL_0062:  stsfld     int32 Test_switch9::s_exitCode
    IL_0067:  ldstr      "Test failed"
    IL_006c:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0071:  br.s       IL_0085

    IL_0073:  ldc.i4.1
    IL_0074:  stsfld     int32 Test_switch9::s_exitCode
    IL_0079:  ldstr      "Test failed"
    IL_007e:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0083:  br.s       IL_0085

    IL_0085:  ret
  }

  .method public hidebysig static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.1
    IL_0002:  call       void Test_switch9::DoSwitch(int32,
                                             int32)
              ldsfld     int32 Test_switch9::s_exitCode
              ret
  }

}
