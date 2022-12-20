// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.40824
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly 'b356258'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {3C4B1460-B1C6-4337-8B12-587C5269CE8D}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000001550000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit FOO
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  call       void FOO::callFoo()
    ldstr "Test SUCCESS"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4 100
    IL_0006:  ret
  } // end of method FOO::Main

  .method public hidebysig static void  callFoo() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    tail.
    IL_0000:  call       void FOO::foo()
    IL_0006:  ret
  }


  .method public hidebysig static void  foo() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method FOO::foo

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method FOO::.ctor

} // end of class FOO


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file foo2.res
