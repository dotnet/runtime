// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// non-empty stack before switch
.assembly extern System.Runtime.Extensions { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'switch11'
{


}
.assembly extern xunit.core {}


.class public auto ansi Test_switch11
       extends ['mscorlib']System.Object
{
  .field private static int32 s_exitCode;

  .method public hidebysig static int32 Main() il managed
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
    IL_0006:  stsfld     int32 Test_switch11::s_exitCode
    IL_000b:  ldstr      "Test passed"
    IL_0010:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0015:  pop
              ldsfld     int32 Test_switch11::s_exitCode
              ret
  }

}
