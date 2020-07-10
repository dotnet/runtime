// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ThisStateTests
{
}

.class public auto ansi beforefieldinit FieldTestsType
        extends [System.Runtime]System.Object
{
    .field private int32 instanceField
    .field private static int32 staticField

    .method public hidebysig instance void 'special.LoadFieldThisInit..ctor_Valid'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.0
        ldfld   int32 FieldTestsType::instanceField
        pop
        ret
    }

    .method public hidebysig instance void 'special.LoadFieldThisUninit..ctor_Valid'(int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed 
    {
        ldarg.0
        dup
        ldfld   int32 FieldTestsType::instanceField
        pop
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.LoadSFieldThisUninit..ctor_Valid'(float64) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(float64) cil managed 
    {
        ldarg.0
        ldsfld   int32 FieldTestsType::staticField
        pop
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.StoreFieldThisInit..ctor_Valid'(int64) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int64) cil managed 
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.0
        ldc.i4.0
        stfld   int32 FieldTestsType::instanceField
        ret
    }

    .method public hidebysig instance void 'special.StoreFieldThisUninit..ctor_Valid'(int32, int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32, int32) cil managed 
    {
        ldarg.0
        dup
        ldc.i4.0
        stfld   int32 FieldTestsType::instanceField
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.StoreSFieldThisUninit..ctor_Valid'(native int) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(native int) cil managed 
    {
        ldc.i4.0
        stsfld   int32 FieldTestsType::staticField
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit ThisStateTestsType
        extends [System.Runtime]System.Object
{
    .method public hidebysig instance void 'special.ThrowThisUninit..ctor_Invalid_UninitStack'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        throw
        ret
    }

    .method public hidebysig instance void 'special.CallCtorOverload..ctor_Valid'(int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed 
    {
        ldarg.0
        call    instance void ThisStateTestsType::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit LoadStoreVarTestsType
        extends [System.Runtime]System.Object
{
    .method public hidebysig instance void 'special.StoreLocUninit..ctor_Invalid_StackUninit'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        .locals init (
            [0] int32
        )

        ldc.i4.0
        stloc.0
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.StoreThisUninit..ctor_Invalid_ThisUninitStore'(int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed 
    {
        newobj  instance void LoadStoreVarTestsType::.ctor()
        starg 0
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.LoadAddrOfThisUninit..ctor_Invalid_ThisUninitStore'(int64) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int64) cil managed 
    {
        ldarga 0
        pop
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
