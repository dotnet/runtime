// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'rethrow' { }
.namespace JitTest
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       120 (0x78)
      .maxstack  2
      .locals (bool V_0,
               bool V_1,
               bool V_2,
               object V_3,
               int32 V_4)
      IL_0000:  ldc.i4.1
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.0
      IL_0003:  stloc.1
      IL_0004:  ldc.i4.0
      IL_0005:  stloc.2
      .try
      {
        IL_0006:  ldstr      "before 1st try...catch"
        IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
        .try
        {
          IL_0010:  ldstr      "inside 1st try"
          IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
          IL_001a:  ldloc.0
          IL_001b:  brfalse.s  IL_0023

          			ldc.r4 1.0
          			box float32
          			throw

          IL_0023:  leave.s    IL_0034

        }  // end .try
        catch [mscorlib]System.Single
        {
          			stloc.3
          			ldloc.3
          			unbox float32
          			ldind.r4
          			ldc.r4 1.0
          			ceq
          			brfalse NEXT
          			
          			ldc.i4.1
          			stloc.1
          			ldstr      "inside 1st catch"
          			call       void [System.Console]System.Console::WriteLine(string)
          			
          NEXT:
          			ldloc.3
          			unbox float32
          			ldc.r4 11.0
          			stind.r4
          			rethrow

        }  // end handler
        IL_0034:  ldstr      "after 1st try...catch"
        IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
        IL_003e:  leave.s    IL_004f

      }  // end .try
      catch [mscorlib]System.Single
      {
          			unbox float32
          			ldind.r4
          			ldc.r4 11.0
          			ceq
          			brfalse NEXT1
          			
        			ldstr      "inside catch"
        			call       void [System.Console]System.Console::WriteLine(string)
        			ldc.i4.1
        			stloc.2
        			
        NEXT1:
        
        			leave.s    IL_004f

      }  // end handler
      IL_004f:  ldloc.1
      IL_0050:  brfalse.s  IL_0065

      IL_0052:  ldloc.2
      IL_0053:  brfalse.s  IL_0065

      IL_0055:  ldstr      "Passed => 100"
      IL_005a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005f:  ldc.i4.s   100
      IL_0061:  stloc.s    V_4
      IL_0063:  br.s       IL_0075

      IL_0065:  ldstr      "Failed => 101"
      IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006f:  ldc.i4.s   101
      IL_0071:  stloc.s    V_4
      IL_0073:  br.s       IL_0075

      IL_0075:  ldloc.s    V_4
      IL_0077:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest
