
//  Microsoft (R) .NET Framework IL Disassembler.  Version 3.5.21022.8
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.module module.netmodule
// MVID: {6EC23D5F-E5C7-4DA2-9E39-4E18B7D0CB11}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00E90000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Module.Exported
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit NestedClass
         extends [mscorlib]System.Object
  {
    .method public hidebysig virtual instance string 
            ToString() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  1
      .locals init (string V_0)
      IL_0000:  nop
      IL_0001:  ldstr      "test"
      IL_0006:  stloc.0
      IL_0007:  br.s       IL_0009

      IL_0009:  ldloc.0
      IL_000a:  ret
    } // end of method NestedClass::ToString

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method NestedClass::.ctor

  } // end of class NestedClass

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Exported::.ctor

} // end of class Module.Exported


