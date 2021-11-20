// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly switchdefaultonly1 {}
.class private auto ansi beforefieldinit switchdefaultonly1
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32  Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       49 (0x31)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldlen
    IL_0002:  conv.i4
    IL_0003:  stloc.0
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  switch     (IL_0014)
    IL_0014:  br.s       IL_0026
    IL_0026:  .try
    {
      ldarg.0
      ldlen
      conv.i4
      call       void [System.Console]System.Console::WriteLine(int32)
      leave.s    IL_002e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      pop
      call       void [mscorlib]System.GC::Collect()
      leave.s    IL_002e

    }  // end handler
    IL_002e:  br.s       IL_0030

    IL_0030:  ldc.i4 100	ret
  }

}
