// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'finally' { }
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
      // Code size       115 (0x73)
      .maxstack  2
      .locals (bool V_0,
               bool V_1,
               bool V_2,
               int32 V_3)
      IL_0000:  ldc.i4.1
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.0
      IL_0003:  stloc.1
      IL_0004:  ldc.i4.0
      IL_0005:  stloc.2
      .try
      {
        IL_0006:  ldstr      "before try...finally"
        IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
        .try
        {
          IL_0010:  ldstr      "inside try"
          IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
          IL_001a:  ldloc.0
          IL_001b:  brfalse.s  IL_0023

					ldc.r8 1.0
					box float64
          			throw

          IL_0023:  leave.s    IL_0032

        }  // end .try
        finally
        {
          IL_0025:  ldc.i4.1
          IL_0026:  stloc.1
          IL_0027:  ldstr      "inside finally"
          IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
          IL_0031:  endfinally
        }  // end handler
        IL_0032:  ldstr      "after try...finally"
        IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
        IL_003c:  leave.s    IL_004d
        
      }  // end .try
      catch [mscorlib]System.Double
      {
        		  unbox float64
        		  ldind.r8
        		  ldc.r8 1.0
        		  ceq
                  brfalse LEAVE
        		
        IL_003f:  ldstr      "inside catch"
        IL_0044:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0049:  ldc.i4.1
        IL_004a:  stloc.2
        LEAVE:
        IL_004b:  leave.s    IL_004d

      }  // end handler
      IL_004d:  ldloc.1
      IL_004e:  brfalse.s  IL_0062

      IL_0050:  ldloc.2
      IL_0051:  brfalse.s  IL_0062

      IL_0053:  ldstr      "Passed => 100"
      IL_0058:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005d:  ldc.i4.s   100
      IL_005f:  stloc.3
      IL_0060:  br.s       IL_0071

      IL_0062:  ldstr      "Failed => 101"
      IL_0067:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006c:  ldc.i4.s   101
      IL_006e:  stloc.3
      IL_006f:  br.s       IL_0071

      IL_0071:  ldloc.3
      IL_0072:  ret
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

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file finally.res
