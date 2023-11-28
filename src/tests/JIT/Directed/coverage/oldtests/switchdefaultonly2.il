// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}
.class public auto ansi beforefieldinit switchdefaultonly1
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    ldc.i4.0
    call int32 switchdefaultonly1::MainX(int32)
    ret
  }

  .method public hidebysig static int32
          MainX(int32 arg) cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
              ldarg.0
    IL_0003:  stloc.0
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  switch     (IL_0014)
    IL_0014:  br.s       IL_0025

    IL_001c:  br.s       IL_0030

    IL_001e:  ldc.i4.2
    IL_001f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0024:  br.s       IL_0030
    IL_0025:  ldc.i4.s 0
    IL_0026:  newarr [mscorlib]System.String
    IL_0027:  ldlen
    IL_0028:  conv.i4
    IL_0029:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_002e:  br.s       IL_0030

    IL_0030:  ldc.i4 100	ret
  }

}
