// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}

.assembly legacy library ASSEMBLY_NAME { }
.class sealed public auto ansi beforefieldinit Test_ldfldr4
       extends [mscorlib]System.Object
{
  .field private initonly float32 a
  .field private initonly static float32 b
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (class Test_ldfldr4 V_0,
             int32 V_1)
    IL_0000:  newobj     instance void Test_ldfldr4::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldfld      float32 Test_ldfldr4::a
    IL_000c:  call       void [System.Console]System.Console::WriteLine(float32)
    IL_0011:  ldsfld     float32 Test_ldfldr4::b
    IL_0016:  call       void [System.Console]System.Console::WriteLine(float32)
    IL_001b:  ldc.i4.s   100
    IL_001d:  stloc.1
    IL_001e:  br.s       IL_0020

    IL_0020:  ldloc.1
    IL_0021:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
}
