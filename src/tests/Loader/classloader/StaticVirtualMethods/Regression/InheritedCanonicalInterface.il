// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
    .ver 8:0:0:0
}
.assembly extern xunit.core {}
.assembly InheritedCanonicalInterface {}

.class interface private abstract IResult`1<T>
{
    .method public static virtual abstract int32 Get() cil managed {}
}

.class private ConstantBase extends [System.Runtime]System.Object implements class IResult`1<object>, class IResult`1<string>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<object>::Get()
        .override method int32 class IResult`1<string>::Get()
        ldc.i4.s 42
        ret
    }
}

.class private ConstantDerived`1<T> extends ConstantBase {}

.class private ResultBase`1<T> extends [System.Runtime]System.Object implements class IResult`1<!T>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<!T>::Get()
        ldc.i4.1
        ret
    }
}

// These interface instantiations can coincide at runtime. The derived method
// wins only when T and U are equal, despite both being canonical references.
.class private Reimplemented`2<T,U> extends class ResultBase`1<!T> implements class IResult`1<!U>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<!U>::Get()
        ldc.i4.2
        ret
    }
}

.class interface private abstract IGeneric`1<T>
{
    .method public static virtual abstract class [System.Runtime]System.Type Get<U>() cil managed {}
}

.class private Pair`2<T,U> extends [System.Runtime]System.Object {}
.class private Wrapper`1<T> extends [System.Runtime]System.Object {}

.class private GenericBase`1<T> extends [System.Runtime]System.Object implements class IGeneric`1<!T>
{
    .method public static class [System.Runtime]System.Type Get<U>() cil managed noinlining
    {
        .override method class [System.Runtime]System.Type class IGeneric`1<!T>::Get<[1]>()
        ldtoken class Pair`2<!T,!!U>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        ret
    }
}

.class private GenericDerived`2<TUnused,T> extends class GenericBase`1<class Wrapper`1<!T>> {}

.class interface private abstract IDefault`1<T> implements class IResult`1<!T>
{
    .method private static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<!T>::Get()
        ldc.i4.s 43
        ret
    }
}

.class private Default`1<T> extends [System.Runtime]System.Object implements class IDefault`1<class Wrapper`1<!T>> {}

.class interface private abstract IMoreSpecific`1<T> implements class IDefault`1<!T>
{
    .method private static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<!T>::Get()
        ldc.i4.s 44
        ret
    }
}

.class interface private abstract IAlternative`1<T> implements class IResult`1<!T>
{
    .method private static int32 Get() cil managed
    {
        .override method int32 class IResult`1<!T>::Get()
        ldc.i4.s 45
        ret
    }
}

.class interface private abstract IReabstract`1<T> implements class IDefault`1<!T>
{
    .method private static abstract virtual final int32 Get() cil managed
    {
        .override method int32 class IResult`1<!T>::Get()
    }
}

.class private MostSpecific`1<T> extends [System.Runtime]System.Object implements class IMoreSpecific`1<class Wrapper`1<!T>> {}
.class private Diamond`1<T> extends [System.Runtime]System.Object implements class IDefault`1<class Wrapper`1<!T>>, class IAlternative`1<class Wrapper`1<!T>> {}
.class private Reabstracted`1<T> extends [System.Runtime]System.Object implements class IReabstract`1<class Wrapper`1<!T>> {}

.class interface private abstract IGenericDefault`1<T> implements class IGeneric`1<!T>
{
    .method private static class [System.Runtime]System.Type Get<U>() cil managed noinlining
    {
        .override method class [System.Runtime]System.Type class IGeneric`1<!T>::Get<[1]>()
        ldtoken class Pair`2<!T,!!U>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        ret
    }
}

.class private GenericDefault`2<TUnused,T> extends [System.Runtime]System.Object implements class IGenericDefault`1<class Wrapper`1<!T>> {}

.class private DefaultBase`1<T> extends [System.Runtime]System.Object implements class IGenericDefault`1<!T> {}
.class private DefaultDerived`2<T,U> extends class DefaultBase`1<!T> implements class IGenericDefault`1<!U> {}

.class interface private abstract IConstantDefault implements class IResult`1<object>, class IResult`1<string>
{
    .method private static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IResult`1<object>::Get()
        .override method int32 class IResult`1<string>::Get()
        ldc.i4.s 43
        ret
    }
}

.class private ConstantDefault`1<T> extends [System.Runtime]System.Object implements IConstantDefault {}

.class interface private abstract ICovariant`1<+ T>
{
    .method public static virtual abstract int32 Get() cil managed {}
}

.class private VariantBase extends [System.Runtime]System.Object implements class ICovariant`1<object>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class ICovariant`1<object>::Get()
        ldc.i4.s 31
        ret
    }
}

.class private VariantDerived extends VariantBase implements class ICovariant`1<string>, class ICovariant`1<int32>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class ICovariant`1<string>::Get()
        ldc.i4.s 32
        ret
    }
    .method private static int32 GetInt() cil managed aggressiveinlining
    {
        .override method int32 class ICovariant`1<int32>::Get()
        ldc.i4.s 33
        ret
    }
}

.class interface private abstract IContravariant`1<- T>
{
    .method public static virtual abstract int32 Get() cil managed {}
}

.class interface private abstract IContravariantDefault implements class IContravariant`1<object>
{
    .method private static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class IContravariant`1<object>::Get()
        ldc.i4.s 49
        ret
    }
}

.class private ContravariantDefault`1<T> extends [System.Runtime]System.Object implements IContravariantDefault {}

.class private VariantStringBase extends [System.Runtime]System.Object implements class ICovariant`1<string>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class ICovariant`1<string>::Get()
        ldc.i4.s 31
        ret
    }
}

.class private RuntimeVariant`1<T> extends VariantStringBase implements class ICovariant`1<!T>
{
    .method public static int32 Get() cil managed aggressiveinlining
    {
        .override method int32 class ICovariant`1<!T>::Get()
        ldc.i4.s 32
        ret
    }
}

.class public InheritedCanonicalInterface extends [System.Runtime]System.Object
{
    // Explicit constructed constraints exercise scanner resolution as well as
    // the JIT, without relying on inlining to expose the implementing family.
    .method private static int32 ReadConstant<T>() cil managed noinlining
    {
        constrained. class ConstantDerived`1<!!T>
        call int32 class IResult`1<!!T>::Get()
        ret
    }

    .method private static int32 ReadReimplemented<T,U>() cil managed noinlining
    {
        constrained. class Reimplemented`2<!!T,!!U>
        call int32 class IResult`1<!!T>::Get()
        ret
    }

    .method private static int32 ReadDefault<T>() cil managed noinlining
    {
        constrained. class Default`1<!!T>
        call int32 class IResult`1<class Wrapper`1<!!T>>::Get()
        ret
    }

    .method private static int32 ReadDefaultConstant<T>() cil managed noinlining
    {
        constrained. class ConstantDefault`1<!!T>
        call int32 class IResult`1<!!T>::Get()
        ret
    }

    .method private static int32 ReadVariant<T>() cil managed noinlining
    {
        constrained. VariantDerived
        call int32 class ICovariant`1<!!T>::Get()
        ret
    }

    .method private static int32 ReadMostSpecific<T>() cil managed noinlining
    {
        constrained. class MostSpecific`1<!!T>
        call int32 class IResult`1<class Wrapper`1<!!T>>::Get()
        ret
    }

    .method private static int32 ReadContravariantDefault<T>() cil managed noinlining
    {
        constrained. class ContravariantDefault`1<!!T>
        call int32 class IContravariant`1<!!T>::Get()
        ret
    }

    // Unlike ReadVariant, the selected method changes within the shared body.
    .method private static int32 ReadRuntimeVariant<T>() cil managed noinlining
    {
        constrained. class RuntimeVariant`1<!!T>
        call int32 class ICovariant`1<string>::Get()
        ret
    }

    .method private static int32 ReadDiamond<T>() cil managed noinlining
    {
        constrained. class Diamond`1<!!T>
        call int32 class IResult`1<class Wrapper`1<!!T>>::Get()
        ret
    }

    .method private static int32 ReadReabstracted<T>() cil managed noinlining
    {
        constrained. class Reabstracted`1<!!T>
        call int32 class IResult`1<class Wrapper`1<!!T>>::Get()
        ret
    }

    .method private static void CheckFailures<T>() cil managed noinlining
    {
        // Known constructed constraints can fail during compilation/type creation
        // rather than runtime dispatch. None of these cases may become a successful call.
        .maxstack 1
        .try
        {
            call int32 InheritedCanonicalInterface::ReadDiamond<!!T>()
            pop
            ldstr "Expected ambiguous default implementation"
            newobj instance void [System.Runtime]System.Exception::.ctor(string)
            throw
        }
        catch [System.Runtime]System.Runtime.AmbiguousImplementationException
        {
            pop
            leave REABSTRACTION
        }
        catch [System.Runtime]System.InvalidProgramException
        {
            pop
            leave REABSTRACTION
        }
        catch [System.Runtime]System.TypeLoadException
        {
            pop
            call bool [System.Runtime]System.Runtime.CompilerServices.RuntimeFeature::get_IsDynamicCodeSupported()
            brfalse AOT_DIAMOND
            rethrow
        AOT_DIAMOND:
            leave REABSTRACTION
        }
    REABSTRACTION:
        .try
        {
            call int32 InheritedCanonicalInterface::ReadReabstracted<!!T>()
            pop
            ldstr "Expected reabstracted implementation failure"
            newobj instance void [System.Runtime]System.Exception::.ctor(string)
            throw
        }
        catch [System.Runtime]System.EntryPointNotFoundException
        {
            pop
            leave DONE
        }
        catch [System.Runtime]System.InvalidProgramException
        {
            pop
            leave DONE
        }
        catch [System.Runtime]System.TypeLoadException
        {
            pop
            call bool [System.Runtime]System.Runtime.CompilerServices.RuntimeFeature::get_IsDynamicCodeSupported()
            brfalse AOT_REABSTRACTION
            rethrow
        AOT_REABSTRACTION:
            leave DONE
        }
    DONE:
        ret
    }

    .method private static void CheckDefaultOwners<T,U>() cil managed noinlining
    {
        .maxstack 2
        ldtoken class Pair`2<!!T,int32>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        constrained. class DefaultDerived`2<!!T,!!U>
        call class [System.Runtime]System.Type class IGeneric`1<!!T>::Get<int32>()
        bne.un FAIL

        ldtoken class Pair`2<!!U,int32>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        constrained. class DefaultDerived`2<!!T,!!U>
        call class [System.Runtime]System.Type class IGeneric`1<!!U>::Get<int32>()
        bne.un FAIL
        ret
    FAIL:
        ldstr "Confused default-interface owner parameters"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method private static void CheckGeneric<TUnused,T,U>() cil managed noinlining
    {
        .maxstack 3
        .locals init (class [System.Runtime]System.Type expected)
        ldtoken class Pair`2<class Wrapper`1<!!T>,!!U>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        stloc.0

        ldloc.0
        constrained. class GenericDerived`2<!!TUnused,!!T>
        call class [System.Runtime]System.Type class IGeneric`1<class Wrapper`1<!!T>>::Get<!!U>()
        bne.un FAIL

        ldloc.0
        ldnull
        constrained. class GenericDerived`2<!!TUnused,!!T>
        ldftn class [System.Runtime]System.Type class IGeneric`1<class Wrapper`1<!!T>>::Get<!!U>()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        callvirt instance !0 class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::Invoke()
        bne.un FAIL

        ldloc.0
        constrained. class GenericDefault`2<!!TUnused,!!T>
        call class [System.Runtime]System.Type class IGeneric`1<class Wrapper`1<!!T>>::Get<!!U>()
        bne.un FAIL

        ldloc.0
        ldnull
        constrained. class GenericDefault`2<!!TUnused,!!T>
        ldftn class [System.Runtime]System.Type class IGeneric`1<class Wrapper`1<!!T>>::Get<!!U>()
        newobj instance void class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::.ctor(object, native int)
        callvirt instance !0 class [System.Runtime]System.Func`1<class [System.Runtime]System.Type>::Invoke()
        bne.un FAIL
        ret
    FAIL:
        ldstr "Incorrect inherited method context"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method public static int32 TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 2
        call int32 InheritedCanonicalInterface::ReadConstant<object>()
        ldc.i4.s 42
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadConstant<string>()
        ldc.i4.s 42
        bne.un FAIL

        call int32 InheritedCanonicalInterface::ReadReimplemented<string,object>()
        ldc.i4.1
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadReimplemented<object,string>()
        ldc.i4.1
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadReimplemented<string,string>()
        ldc.i4.2
        bne.un FAIL

        call int32 InheritedCanonicalInterface::ReadDefault<object>()
        ldc.i4.s 43
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadDefault<string>()
        ldc.i4.s 43
        bne.un FAIL

        call int32 InheritedCanonicalInterface::ReadDefaultConstant<object>()
        ldc.i4.s 43
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadDefaultConstant<string>()
        ldc.i4.s 43
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadVariant<object>()
        ldc.i4.s 32
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadVariant<string>()
        ldc.i4.s 32
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadVariant<int32>()
        ldc.i4.s 33
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadMostSpecific<object>()
        ldc.i4.s 44
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadMostSpecific<string>()
        ldc.i4.s 44
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadContravariantDefault<object>()
        ldc.i4.s 49
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadContravariantDefault<string>()
        ldc.i4.s 49
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadRuntimeVariant<object>()
        ldc.i4.s 31
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadRuntimeVariant<string>()
        ldc.i4.s 32
        bne.un FAIL
        call int32 InheritedCanonicalInterface::ReadRuntimeVariant<object>()
        ldc.i4.s 31
        bne.un FAIL

        call void InheritedCanonicalInterface::CheckFailures<object>()
        call void InheritedCanonicalInterface::CheckFailures<string>()

        call void InheritedCanonicalInterface::CheckDefaultOwners<object,string>()
        call void InheritedCanonicalInterface::CheckDefaultOwners<string,object>()
        call void InheritedCanonicalInterface::CheckDefaultOwners<string,string>()

        call void InheritedCanonicalInterface::CheckGeneric<string,object,string>()
        call void InheritedCanonicalInterface::CheckGeneric<object,string,object>()
        call void InheritedCanonicalInterface::CheckGeneric<string,object,int32>()
        call void InheritedCanonicalInterface::CheckGeneric<string,object,string>()
        ldc.i4.s 100
        ret
    FAIL:
        ldc.i4.1
        ret
    }
}
