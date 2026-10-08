// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly FieldTests
{
}

.class private auto ansi beforefieldinit ConstrainedClass`1<([System.Runtime]System.Collections.IEnumerable) T>
    extends [System.Runtime]System.Object
{
    .field public static int32 StaticField

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit FieldTestsType
        extends [System.Runtime]System.Object
{
    .field public initonly int32 InstanceInitonlyField
    .field public static initonly int32 StaticInitonlyField

    .field private initonly int32 InstanceExternalInitField

    .method public hidebysig specialname instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) 'special.StoreExternalInitField.set_MyProperty_Valid'(int32 'value') cil managed { ret }
    .method public hidebysig specialname instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) set_MyProperty(int32 'value') cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32 FieldTestsType::InstanceExternalInitField
        ret
    }

    .method public instance void Stsfld.UnsatisfiedParentConstraints_Invalid_UnsatisfiedFieldParentInst() cil managed
    {
        ldc.i4.0
        stsfld   int32 class ConstrainedClass`1<int32>::StaticField
        ret
    }

    .method public instance void Stfld.InitonlyFieldOutsideCtor_Invalid_InitOnly() cil managed
    {
        ldarg.0
        ldc.i4.0
        stfld   int32 FieldTestsType::InstanceInitonlyField
        ret
    }

    // TODO: verification of readonly references https://github.com/dotnet/runtime/issues/57444
    .method public instance void Ldflda.InitonlyFieldOutsideCtor_Valid() cil managed
    {
        ldarg.0
        ldflda  int32 FieldTestsType::InstanceInitonlyField
        pop
        ret
    }

    .method public hidebysig instance void 'special.StoreInitonlyField..ctor_Valid'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.0
        ldc.i4.0
        stfld   int32 FieldTestsType::InstanceInitonlyField
        ret
    }

    .method public hidebysig instance void 'special.LoadAddrInitonlyField..ctor_Valid'(int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.0
        ldflda  int32 FieldTestsType::InstanceInitonlyField
        pop
        ret
    }

    .method public hidebysig instance void 'special.LoadAddrInitonlyField..ctor_Valid'(int64) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int64) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.0
        ldflda  int32 FieldTestsType::InstanceInitonlyField
        pop
        ret
    }

    .method public hidebysig instance void 'special.StoreInitonlyFieldOtherType..ctor_Invalid_InitOnly'(class OtherType c) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(class OtherType c) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.1
        ldc.i4.0
        stfld   int32 OtherType::InstanceInitonlyField
        ret
    }

    .method public hidebysig instance void 'special.StoreInitonlyFieldOtherInstance..ctor_Invalid_InitOnly'(class FieldTestsType c) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(class FieldTestsType c) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldarg.1
        ldc.i4.0
        stfld   int32 FieldTestsType::InstanceInitonlyField
        ret
    }

    .method public hidebysig instance void 'special.StsfldInitonlyInCtor..ctor_Invalid_InitOnly'(bool) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(bool) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldc.i4.0
        stsfld  int32 FieldTestsType::StaticInitonlyField
        ret
    }

    .method public hidebysig instance void 'special.LdsfldInitonlyInCtor..ctor_Invalid_InitOnly'(char) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(char) cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ldsflda int32 FieldTestsType::StaticInitonlyField
        pop
        ret
    }

    .method public hidebysig instance void 'special.LdsfldStslfdInitonlyCctor..cctor_Valid'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .cctor() cil managed
    {
        ldsflda int32 FieldTestsType::StaticInitonlyField
        pop
        ldc.i4.0
        stsfld  int32 FieldTestsType::StaticInitonlyField
        ret
    }
}

.class public auto ansi beforefieldinit OtherType
        extends [System.Runtime]System.Object
{
    .field public static class OtherType Instance

    .field public initonly int32 InstanceInitonlyField
    .field public static initonly int32 StaticInitonlyField

    .method public hidebysig instance void 'special.LdfldStlfdInitonlyCctor..cctor_Invalid_InitOnly'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .cctor() cil managed
    {
        ldsfld class OtherType OtherType::Instance
        ldflda int32 OtherType::InstanceInitonlyField
        pop
        ldsfld class OtherType OtherType::Instance
        ldc.i4.0
        stfld  int32 OtherType::InstanceInitonlyField
        ret
    }
}
