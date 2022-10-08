// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly isinst_catch { }
.assembly extern xunit.core {}
.namespace JitTest_isinst_catch_SEH_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals (int32 V_0,
               int32 V_1,
               int32 V_4)
      .try
      {
        IL_0000:  ldc.i4.5
        IL_0001:  stloc.0
        IL_0002:  ldloc.0
        IL_0003:  stloc.1
        IL_0004:  ldloc.0
        IL_0005:  ldloc.1
        IL_0006:  ldloc.0
        IL_0007:  sub
        IL_0008:  div
        IL_0009:  call       void [System.Console]System.Console::WriteLine(int32)
        IL_000e:  ldstr      "failed (1)"
        IL_0013:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0018:  ldc.i4.s   101
        IL_001a:  stloc.s    V_4
        IL_001c:  leave.s    IL_003b
      }
      catch [mscorlib]System.Exception 
      {
        IL_0020:  isinst  [mscorlib]System.DivideByZeroException
        IL_0027:  call       void [System.Console]System.Console::WriteLine(object)
        IL_002c:  ldstr      "*** TEST PASSED ***"
        IL_0031:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0036:  leave.s    IL_0038
      }
      IL_0038:  ldc.i4.s   100
      IL_003a:  ret
      IL_003b:  ldloc.s    V_4
      IL_003d:  ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }
  }
}
