// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern common {}
.assembly 'test_552940' {}



.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Test(int32 count) cil managed
  {
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    .try
    {
      .try
      {
        IL_0002:  nop
        .try
        {
          IL_0003:  ldarg.0
          IL_0004:  dup
          IL_0005:  ldc.i4.1
          IL_0006:  sub
          IL_0007:  starg.s    count
          IL_0009:  brtrue.s   IL_000d

          IL_000b:  leave.s    IL_0016

          IL_000d:  leave.s    IL_0002

        }  
        finally
        {
          IL_000f:  ldloc.0
          IL_0010:  ldc.i4.0
          IL_0011:  bge.s      IL_0015

          IL_0013:  ldc.i4.0
          IL_0014:  stloc.0
          IL_0015:  endfinally
        }  
        IL_0016:  leave.s    IL_001d

      }  
      finally
      {
        IL_0018:  ldloc.0
        IL_0019:  ldc.i4.1
        IL_001a:  add
        IL_001b:  stloc.0
        IL_001c:  endfinally
      }  
      IL_001d:  leave.s    IL_0022

    }  
    catch [mscorlib]System.Exception 
    {
      IL_001f:  pop
      IL_0020:  rethrow
    }  
    IL_0022:  ldloc.0
    IL_0023:  ret
  } 

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  call       int32 Program::Test(int32)
    IL_0006:  stloc.0
    IL_0007:  ldstr      "Finally Exec Count: {0}"
    IL_000c:  ldloc.0
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0017:  ldc.i4.1
    IL_0018:  call       int32 Program::Test(int32)
    IL_001d:  ldc.i4.1
    IL_001e:  bne.un.s   IL_002d

    IL_0020:  ldstr      "PASS"
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002a:  ldc.i4.s   100
    IL_002c:  ret

    IL_002d:  ldstr      "FAIL"
    IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0037:  ldc.i4.1
    IL_0038:  ret
  } 


} 
