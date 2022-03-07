// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'b68757' {}
.assembly extern xunit.core {}
.class private bug1
{
  .class auto ansi nested public beforefieldinit CL1
         extends [mscorlib]System.Object
  {
    .field public float64 a1
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.r8     2.
      IL_000a:  stfld      float64 bug1/CL1::a1
      IL_000f:  ldarg.0
      IL_0010:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0015:  ret
    }
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  4
    .locals ([0] class bug1/CL1 cl1)
    IL_0000:  newobj     instance void bug1/CL1::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldstr      "The result should be 4"
    IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0010:  ldc.r8     4.
    IL_0019:  ldloc.0
    IL_001a:  ldfld      float64 bug1/CL1::a1
    IL_001f:  div
    IL_0020:  ldloc.0
    IL_0021:  ldfld      float64 bug1/CL1::a1
    IL_0026:  ldloc.0
    IL_0027:  ldfld      float64 bug1/CL1::a1
    IL_002c:  ldc.r8     3.
    IL_0035:  sub
    IL_0036:  mul
    IL_0037:  sub
    IL_0038:  call       void [System.Console]System.Console::WriteLine(float64)
    		ldc.i4 100
    IL_003d:  ret
  } 
}
