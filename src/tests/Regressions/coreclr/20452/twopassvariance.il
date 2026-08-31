// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}

.assembly twopassvariance { }

.class Base { }

.class Derived extends Base { }

.class SuperDerived extends Derived { }

.class MegaSuperDerived extends SuperDerived { }

.class interface IFoo<-T>
{
    .method public newslot virtual instance class [mscorlib]System.Type Gimme()
    {
        ldtoken class IFoo<!0>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ret
    }
}

.class interface IBar<T> implements class IFoo<!T>
{
    .method public virtual final instance class [mscorlib]System.Type Gimme()
    {
        .override method instance class [mscorlib]System.Type class IFoo<!T>::Gimme()
        ldtoken class IBar<!0>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ret
    }
}

.class interface IBaz<T> implements class IFoo<!T>
{
    .method public virtual final instance class [mscorlib]System.Type Gimme()
    {
        .override method instance class [mscorlib]System.Type class IFoo<!T>::Gimme()
        ldtoken class IBar<!0>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ret
    }
}

.class interface IQux implements class IFoo<class Base>, class IFoo<class Derived>
{
    .method public virtual final instance class [mscorlib]System.Type Gimme()
    {
        .override method instance class [mscorlib]System.Type class IFoo<class Base>::Gimme()
        ldtoken IQux
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ret
    }
}

.class Fooer1
    implements class IFoo<class Base>, class IFoo<class Derived>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class Fooer2
    implements class IFoo<class Derived>, class IFoo<class Base>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class Fooer3
    implements class IBaz<class Base>, class IBar<class Derived>, class IFoo<object>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class Fooer4
    implements IQux, class IBar<class Derived>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class Fooer5
    implements class IBar<class Derived>, IQux
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class Fooer6
    implements class IFoo<class Base>, class IBar<class Base>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class public Fooer7
    implements class IBar<class Base>, class IFoo<class Base>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit twopassvariance
    extends [mscorlib]System.Object
{
    .method public static int32 main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        newobj instance void Fooer1::.ctor()
        castclass class IFoo<class SuperDerived>
        callvirt instance class [mscorlib]System.Type class IFoo<class SuperDerived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IFoo<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer1_IFoo_SuperDerivedOK
        ldc.i4.1
        ret

    Fooer1_IFoo_SuperDerivedOK:
        newobj instance void Fooer2::.ctor()
        castclass class IFoo<class SuperDerived>
        callvirt instance class [mscorlib]System.Type class IFoo<class SuperDerived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IFoo<class Derived>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer2_IFoo_SuperDerivedOK
        ldc.i4.2
        ret

    Fooer2_IFoo_SuperDerivedOK:
        newobj instance void Fooer3::.ctor()
        castclass class IFoo<class Base>
        callvirt instance class [mscorlib]System.Type class IFoo<class Base>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer3_IFoo_BaseOK
        ldc.i4.3
        ret

    Fooer3_IFoo_BaseOK:
        newobj instance void Fooer3::.ctor()
        castclass class IFoo<class Derived>
        callvirt instance class [mscorlib]System.Type class IFoo<class Derived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Derived>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer3_IFoo_DerivedOK
        ldc.i4.4
        ret

    Fooer3_IFoo_DerivedOK:
        newobj instance void Fooer3::.ctor()
        castclass class IFoo<class SuperDerived>
        callvirt instance class [mscorlib]System.Type class IFoo<class SuperDerived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer3_IFoo_SuperDerivedOK
        ldc.i4.5
        ret

    Fooer3_IFoo_SuperDerivedOK:
        newobj instance void Fooer4::.ctor()
        castclass class IFoo<class Base>
        callvirt instance class [mscorlib]System.Type class IFoo<class Base>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken IQux
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer4_IFoo_BaseOK
        ldc.i4.6
        ret

    Fooer4_IFoo_BaseOK:
        newobj instance void Fooer4::.ctor()
        castclass class IFoo<class Derived>
        callvirt instance class [mscorlib]System.Type class IFoo<class Derived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Derived>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer4_IFoo_DerivedOK
        ldc.i4.7
        ret

    Fooer4_IFoo_DerivedOK:
        newobj instance void Fooer4::.ctor()
        castclass class IFoo<class SuperDerived>
        callvirt instance class [mscorlib]System.Type class IFoo<class SuperDerived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken IQux
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer4_IFoo_SuperDerivedOK
        ldc.i4.8
        ret

    Fooer4_IFoo_SuperDerivedOK:
        newobj instance void Fooer5::.ctor()
        castclass class IFoo<class SuperDerived>
        callvirt instance class [mscorlib]System.Type class IFoo<class SuperDerived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Derived>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer5_IFoo_SuperDerivedOK
        ldc.i4 9
        ret

    Fooer5_IFoo_SuperDerivedOK:
        newobj instance void Fooer6::.ctor()
        castclass class IFoo<class Base>
        callvirt instance class [mscorlib]System.Type class IFoo<class Base>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer6_IFoo_BaseOK
        ldc.i4 10
        ret

    Fooer6_IFoo_BaseOK:
        newobj instance void Fooer6::.ctor()
        castclass class IFoo<class Derived>
        callvirt instance class [mscorlib]System.Type class IFoo<class Derived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer6_IFoo_DerivedOK
        ldc.i4 11
        ret

    Fooer6_IFoo_DerivedOK:
        newobj instance void Fooer7::.ctor()
        castclass class IFoo<class Base>
        callvirt instance class [mscorlib]System.Type class IFoo<class Base>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer7_IFoo_BaseOK
        ldc.i4 12
        ret

    Fooer7_IFoo_BaseOK:
        newobj instance void Fooer7::.ctor()
        castclass class IFoo<class Derived>
        callvirt instance class [mscorlib]System.Type class IFoo<class Derived>::Gimme()
        dup
        callvirt instance string [mscorlib]System.Object::ToString()
        call void [mscorlib]System.Console::WriteLine(string)
        ldtoken class IBar<class Base>
        call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        ceq
        brtrue Fooer7_IFoo_DerivedOK
        ldc.i4 13
        ret

    Fooer7_IFoo_DerivedOK:

        ldc.i4 100
        ret
    }
}
