// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestMethodDebugInformation
{
}

.class abstract auto ansi BaseTestMethodDebugInformationType extends [System.Runtime]System.Object
{
    // testing empty body methods
    .method family hidebysig newslot virtual abstract 
        instance int32 Foo() cil managed { }
    
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
          .maxstack  8
          IL_0000:  ldarg.0
          IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
          IL_0006:  ret
    }
}

.class auto ansi beforefieldinit TestMethodDebugInformationType extends BaseTestMethodDebugInformationType
{
    .method family hidebysig virtual 
        instance int32 Foo() cil managed
    {
         IL_0000:  nop
         IL_0001:  ret
    }
}
