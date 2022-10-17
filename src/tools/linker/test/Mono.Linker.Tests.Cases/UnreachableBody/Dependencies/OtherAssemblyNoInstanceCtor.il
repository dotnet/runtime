
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
}
.assembly other
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module other.dll

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor
       extends [mscorlib]System.Object
{
  .class auto ansi nested public beforefieldinit Foo
         extends [mscorlib]System.Object
  {
    .method public hidebysig instance void 
            Method() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor/Foo::UsedByMethod()
      IL_0006:  ret
    } // end of method Foo::Method

    .method private hidebysig instance void 
            UsedByMethod() cil managed
    {
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method Foo::UsedByMethod

  } // end of class Foo

  .method public hidebysig static void  UnusedSanityCheck() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}