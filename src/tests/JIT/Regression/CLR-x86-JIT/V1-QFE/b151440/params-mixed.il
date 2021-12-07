// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly test
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Test_params_mixed
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed noinlining
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       83 (0x53)
    .maxstack  5
    .locals init (string V_0,
             string V_1,
             string V_2,
             int32 V_3,
             bool V_4)
      ldstr      ", world"
      stloc.0
      ldstr      "hello"
      stloc.1
      ldloc.0
      ldc.i4.0
      ldloc.1
      ldftn   instance string [mscorlib]System.String::Insert(int32,
                                                                         string)
      calli   instance string (int32,
                                                                         string)
      stloc.2
      ldloc.2
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldloc.2
      ldloc.1
      ldloc.0
      call       string [mscorlib]System.String::Concat(string,
                                                                string)
      call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
      ldc.i4.0
      ceq
      stloc.s    V_4
      ldloc.s    V_4
      brtrue.s   IL_0042

      ldstr      "String.Insert test passed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   100
      stloc.3
      br.s       IL_0051

    IL_0042:  ldstr      "String.Insert test failed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.1
      stloc.3
      br.s       IL_0051

    IL_0051:  ldloc.3
      ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
