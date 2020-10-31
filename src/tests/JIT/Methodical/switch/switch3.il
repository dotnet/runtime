// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// checking fallthrough in switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch3'
{


}

.module 'switch3.exe'

.class public auto ansi Test
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
                          IL_0016)
    IL_0012:  br.s       IL_0029

    IL_0014:  br.s       IL_0016

    IL_0016:  ldc.i4.s   100
    IL_0018:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_001d:  ldstr      "Test passed"
    IL_0022:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0027:  br.s       IL_003b

    IL_0029:  ldc.i4.1
    IL_002a:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_002f:  ldstr      "Test failed"
    IL_0034:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0039:  br.s       IL_003b

    IL_003b:  ret
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
