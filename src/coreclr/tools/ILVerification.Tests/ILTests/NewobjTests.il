// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly NewobjTests
{
}

.class public auto ansi abstract beforefieldinit AbstractClass
{
    .method public hidebysig newslot abstract virtual instance void AbstractMethod() cil managed
    {
    }
}

.class public auto ansi beforefieldinit NewobjTestsType
        extends [System.Runtime]System.Object
{
    .method public hidebysig instance void InstanceMethod() cil managed
    {
        ret
    }

    .method public hidebysig static void StaticMethod() cil managed
    {
        ret
    }

    .method public hidebysig instance void Newobj.InstanceMethod_Invalid_CtorExpected() cil managed
    {
        newobj      instance void NewobjTestsType::InstanceMethod()
        pop
        ret
    }

    .method public hidebysig instance void Newobj.AbstractMethod_Invalid_CtorSig.CtorExpected() cil managed
    {
        newobj      instance void AbstractClass::AbstractMethod()
        pop
        ret
    }
}
