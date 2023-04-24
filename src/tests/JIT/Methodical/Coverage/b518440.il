// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}


.assembly extern mscorlib{}
.assembly b518440{}

.namespace Test_b518440 {
.class public A
{
  .method public hidebysig static int32 main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 2
  
    call void Test_b518440.A::Test()

    ldstr "Test SUCCESS"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4 100
    ret
  }

  .method public static void Test()
  {
    // Code size       45 (0x2d)
    .maxstack  2
    .locals init (bool V_0,
             class [mscorlib]System.Exception V_1,
             class [mscorlib]System.Exception V_2,
             class [mscorlib]System.Exception V_3,
             class [mscorlib]System.Exception V_4)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    .try
    {
      .try
      {
        IL_000c:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0011:  throw

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0012:  dup
        IL_0013:  stloc.3
        IL_0014:  stloc.1
        IL_0015:  leave.s    IL_0018

      }  // end handler
    }  // end .try
    finally
    {
      IL_0017:  endfinally
    }  // end handler
    IL_0018:  ldloc.0
    IL_0019:  brfalse.s  IL_001b

    .try
    {
      IL_001b:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0020:  throw

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0021:  dup
      IL_0022:  stloc.s    V_4
      IL_0024:  stloc.2
      IL_0025:  leave.s    IL_002c

    }  // end handler

    IL_002c:  ret
  } // end of method Test_b518440.A::main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_b518440.A::.ctor
}
}
