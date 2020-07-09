// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// calling switch in a loop
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch7'
{


}

.module 'switch7.exe'

.class public auto ansi Test
       extends ['mscorlib']System.Object
{
  .field private static int32 count
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
                          IL_0018,
                          IL_0026,
                          IL_0034)
    IL_0016:  br.s       IL_0042

    IL_0018:  ldsfld     int32 Test::count
    IL_001d:  ldc.i4.1
    IL_001e:  add
    IL_001f:  stsfld     int32 Test::count
    IL_0024:  br.s       IL_0044

    IL_0026:  ldsfld     int32 Test::count
    IL_002b:  ldc.i4.1
    IL_002c:  sub
    IL_002d:  stsfld     int32 Test::count
    IL_0032:  br.s       IL_0044

    IL_0034:  ldsfld     int32 Test::count
    IL_0039:  ldc.i4.1
    IL_003a:  add
    IL_003b:  stsfld     int32 Test::count
    IL_0040:  br.s       IL_0044

    IL_0042:  br.s       IL_0044

    IL_0044:  ret
  }

  .method public hidebysig static void Main() il managed
  {
    .entrypoint
    .maxstack  2
    .locals (int32 V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000e

    IL_0004:  ldloc.0
    IL_0005:  call       void Test::DoSwitch(int32)
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  add
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.4
    IL_0010:  ble.s      IL_0004

    IL_0012:  ldsfld     int32 Test::count
    IL_0017:  ldc.i4.1
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
    IL_003d:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

}
