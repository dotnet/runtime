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
.assembly template
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed noinlining
  {
    .entrypoint
    // Code size       74 (0x4a)
    .maxstack 10
    .locals init (string V_0,
             string V_1,
             int32 V_2,
             bool V_3)
      ldstr      "hello"
      stloc.0
      ldloc.0
      ldstr      "h"
      ldstr      "j"
      ldftn   instance string [mscorlib]System.String::Replace(string,
                                                                          string)
			calli		instance string(string,string)
      stloc.1
      ldloc.1
      ldstr      "jello"
      callvirt   instance bool [mscorlib]System.String::Equals(string)
      ldc.i4.0
      ceq
      stloc.3
      ldloc.3
      brtrue.s   IL_0039

      ldstr      "Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   100
      stloc.2
      br.s       IL_0048

    IL_0039:  ldstr      "Failed"
      call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.1
      stloc.2
      br.s       IL_0048

    IL_0048:  ldloc.2
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
