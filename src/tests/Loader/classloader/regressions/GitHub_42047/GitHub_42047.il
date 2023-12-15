// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern xunit.core {}
.assembly testchangesimpofvirtslot {}

.class private auto ansi beforefieldinit A
{
    .method public hidebysig newslot virtual instance class A M () cil managed
    {
        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance void Dummy1 () cil managed { ret }
    .method public hidebysig newslot virtual instance void Dummy2 () cil managed { ret }
    .method public hidebysig newslot virtual instance void Dummy3 () cil managed { ret }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class private auto ansi beforefieldinit B extends A
{
    .method public hidebysig newslot virtual instance class B M () cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
        .override method instance class A A::M()

        ldnull
        ret
    }

    .method public hidebysig newslot virtual instance void DummyB1 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB2 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB3 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB4 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB5 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB6 () cil managed { ret }
    .method public hidebysig newslot virtual instance void DummyB7 () cil managed { ret }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call instance void A::.ctor()
        ret
    }
}

.class private auto ansi beforefieldinit C extends B
{
    .method public hidebysig virtual instance class C M () cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
        .override method instance class A A::M()
        ldnull
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call instance void B::.ctor()
        ret
    }
}

.class private auto ansi beforefieldinit D extends C
{
    .method public hidebysig virtual instance class D M () cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
        .override method instance class A A::M()
        ldnull
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call instance void C::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit Program
{
    .method public hidebysig static int32 Main () cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        newobj instance void C::.ctor()
        callvirt instance class B B::M()
        pop

        newobj instance void D::.ctor()
        callvirt instance class A A::M()
        pop

        ldc.i4.s 100
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
