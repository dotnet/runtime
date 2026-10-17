// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public interface INotImplemented
{
}

public interface INotImplementedByClass
{
}

public interface INotImplementedByCom
{
}

public interface IVariant<out T>
{
}

public interface ISource
{
}

public interface ITarget
{
}

public interface ISourceForOutsideTarget
{
}

public interface IDerivedFromOutside : IDisposable
{
}

public interface ITargetOfDerived
{
}

public interface IGenericSource<T>
{
}

public interface ITargetOfGeneric
{
}

public struct SourceStruct
{
    public int Value;
}

public struct TargetStruct
{
    public int Value;
}

public struct ControlStruct
{
    public int Value;
}

public sealed class SealedClass
{
}

public sealed class VariantImplementation : IVariant<string>
{
}

[ComImport]
[Guid("8F4E2A61-5C9B-4D3E-A7F0-1B2C3D4E5F60")]
public sealed class ComClass
{
}

public static class CastFolding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CharIsByte(char c) => (object)c is byte;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool StructIsOtherStruct(SourceStruct s) => (object)s is TargetStruct;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool StructIsInterface(ControlStruct s) => (object)s is INotImplemented;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SealedClassIsInterface(SealedClass c) => (object)c is INotImplementedByClass;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool VariantCast(VariantImplementation v) => (object)v is IVariant<SealedClass>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ComClassIsInterface(ComClass c) => (object)c is INotImplementedByCom;

    // Each guard gets a signal of its own: an unfolded IsAssignableFrom or IsAssignableTo leaves an import
    // for its call, while the type handle imports for typeof remain even when the cast folds.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool InterfaceIsInterface() => typeof(ITarget).IsAssignableFrom(typeof(ISource));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool InterfaceIsOutsideInterface() => typeof(IComparable).IsAssignableFrom(typeof(ISourceForOutsideTarget));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool DerivedFromOutsideIsInterface() => typeof(IDerivedFromOutside).IsAssignableTo(typeof(ITargetOfDerived));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SharedGenericIsInterface<T>() where T : class => typeof(ITargetOfGeneric).IsAssignableFrom(typeof(IGenericSource<T>));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CallSharedGeneric() => SharedGenericIsInterface<string>();
}
