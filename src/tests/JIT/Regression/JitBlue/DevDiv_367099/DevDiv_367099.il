// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'DevDiv_367099' {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void P::TestCatchReturn()
    IL_0006:  nop
    IL_0007:  ldc.i4.s   100
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_000c

    IL_000c:  ldloc.0
    IL_000d:  ret
  } // end of method P::Main

  .method public hidebysig static void  TestCatchReturn() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  1
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  nop
      IL_0003:  leave.s    IL_001b

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0005:  pop
      IL_0006:  nop
      .try
      {
        IL_0007:  nop
        .try
        {
          .try
          {
            IL_0008:  nop
            IL_0009:  leave.s    IL_001c

          }  // end .try
          catch [mscorlib]System.Object 
          {
            IL_000b:  pop
            IL_000c:  nop
            IL_000d:  leave.s    IL_001c

          }  // end handler
        }  // end .try
        finally
        {
          IL_000f:  nop
          IL_0010:  nop
          IL_0011:  endfinally
        }  // end handler
      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0012:  pop
        IL_0013:  nop
        IL_0014:  nop
        IL_0015:  leave.s    IL_0017

      }  // end handler
      IL_0017:  nop
      IL_0018:  nop
      IL_0019:  leave.s    IL_001b

    }  // end handler
    IL_001b:  nop
    IL_001c:  nop
    IL_001d:  ret
  } // end of method P::TestCatchReturn

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method P::.ctor

} // end of class P
