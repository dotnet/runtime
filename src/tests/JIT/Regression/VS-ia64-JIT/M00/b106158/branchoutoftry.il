// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v1.1.904.chk
.assembly extern mscorlib
{
}
.assembly 'branchoutoftry'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {55BD0E59-8783-4990-A94D-348582FD2ED5}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03EC0000

// =============== CLASS MEMBERS DECLARATION ===================

.namespace hello
{
  .class private auto ansi beforefieldinit Class1
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 
            Main(string[] args) cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       44 (0x2c)
      .maxstack  2
      .try
      {
        IL_0000:  ldstr      "In try"
        IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
        IL_000a:  leave.s    IL_0021

      }  // end .try
      finally
      {
        IL_000c:  ldstr      "In finally"
        IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0016:  endfinally
      }  // end handler
      IL_0017:  ldstr      "Unreached"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0021:  ldstr      "After try"
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4 100
      IL_002b:  ret
    } // end of method Class1::Main

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Class1::.ctor

  } // end of class Class1

} // end of namespace hello


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
