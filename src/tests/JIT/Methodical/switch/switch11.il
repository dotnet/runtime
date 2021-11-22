// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// non-empty stack before switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch11'
{


}
.assembly extern xunit.core {}

.module 'switch11.exe'

.class public auto ansi Test_switch11
       extends ['mscorlib']System.Object
{
  .method public hidebysig static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
              ldc.i4.5
    IL_0000:  br.s       IL_0002

    IL_0002:  br.s       IL_0004

    IL_0004:  ldc.i4.s   100
    IL_0006:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_000b:  ldstr      "Test passed"
    IL_0010:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0015:  pop
              call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

}
