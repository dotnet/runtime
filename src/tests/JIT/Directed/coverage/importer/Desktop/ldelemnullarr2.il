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
.class public auto ansi beforefieldinit ldelemnullarr2
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  f() cil managed
  {
    .maxstack  2
    .locals init (int32[] V_0)
    IL_0002:  ldnull
    IL_0003:  ldc.i4.0
    IL_0004:  ldelem.i4
    IL_0005:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_000a:  ret
  } 

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (int32 V_0)
    .try
    {
      IL_0000:  call       void ldelemnullarr2::f()
      IL_0005:  leave.s    IL_0021

    }  
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0007:  pop
      IL_0008:  ldstr      "Caught expected System.NullReferenceException"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  ldstr      "PASSED"
      IL_0017:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001c:  ldc.i4.s   100
      IL_001e:  stloc.0
      IL_001f:  leave.s    IL_0039

    }  
    IL_0021:  ldstr      "Should have caught System.NullReferenceException"
    IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002b:  ldstr      "FAILED"
    IL_0030:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0035:  ldc.i4.1
    IL_0036:  stloc.0
    IL_0037:  br.s       IL_0039

    IL_0039:  ldloc.0
    IL_003a:  ret
  }
}
