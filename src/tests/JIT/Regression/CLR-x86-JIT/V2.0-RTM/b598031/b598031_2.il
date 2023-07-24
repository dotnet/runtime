// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b598031_2' {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit switchdefaultonly1
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init ([0] int32 V_0)
      ldc.i4.0
      newarr [mscorlib]System.String
      call int32 switchdefaultonly1::MainX(string[])
      ret
  }

  .method public hidebysig static int32
          MainX(string[] args) cil managed noinlining
  {
    // Code size       44 (0x2c)
    .maxstack  2
    .locals init ([0] int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldlen
    IL_0002:  conv.i4
    IL_0003:  stloc.0
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  switch     ( 
                          IL_0010)
    IL_0010:  br.s       IL_0012

    .try
    {
      IL_0012:  ldarg.0
      IL_0013:  ldlen
      IL_0014:  conv.i4
      IL_0015:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_001a:  leave.s    IL_0024

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001c:  pop
      IL_001d:  call       void [mscorlib]System.GC::Collect()
      IL_0022:  leave.s    IL_0024

    }  // end handler
    IL_0024:  br.s       IL_0026

    IL_0026:  ldc.i4     0x64
    IL_002b:  ret
  } // end of method switchdefaultonly1::Main

} // end of class switchdefaultonly1
