// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace JitTest_ldnull_il
{
  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       36 (0x24)
      .maxstack  2
      .locals (class JitTest_ldnull_il.TestClass V_0,
               class JitTest_ldnull_il.TestClass V_1)
      ldnull
      isinst JitTest_ldnull_il.TestClass
      stloc.0
      
      ldnull
      castclass JitTest_ldnull_il.TestClass
      stloc.1
      
      IL_0004:  ldloc.0
      IL_0005:  brtrue.s   IL_000a

      IL_0007:  ldloc.1
      IL_0008:  brfalse.s  IL_0017

      IL_000a:  ldstr      "Failed => 101"
      IL_000f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0014:  ldc.i4.s   101
      IL_0016:  ret

      IL_0017:  ldstr      "Passed => 100"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0021:  ldc.i4.s   100
      IL_0023:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_ldnull_il

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file ldnull.res
