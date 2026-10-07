// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

public abstract partial class Delegate
{
    /// <summary>Gets a delegate type with the specified parameter types and return type.</summary>
    /// <param name="typeArgs">The parameter types followed by the return type. Use <see cref="Void"/> for a delegate that does not return a value.</param>
    /// <returns>A delegate type with the specified signature.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeArgs"/> or one of its elements is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="typeArgs"/> is empty or contains an invalid signature type.</exception>
    /// <exception cref="PlatformNotSupportedException">The runtime does not support creating the requested delegate type.</exception>
    /// <remarks>
    /// Uses a predefined <see cref="Action"/> or <see cref="Func{TResult}"/> type when possible.
    /// Otherwise, creates a runtime-implemented delegate type. Custom delegate types require closed runtime types.
    /// Repeated requests for the same custom signature return the same type.
    /// A custom type referencing collectible types is collectible and keeps those types alive while it is in use.
    /// </remarks>
    [RequiresDynamicCode("Creating a delegate type may require generating code at runtime.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "Only statically referenced Func and Action definitions, without member-preservation constraints, are instantiated.")]
    public static Type GetDelegateType(params Type[] typeArgs)
    {
        ArgumentNullException.ThrowIfNull(typeArgs);
        if (typeArgs.Length == 0)
        {
            throw new ArgumentException(SR.Arg_EmptyArray, nameof(typeArgs));
        }

        bool needsCustomType = typeArgs.Length > 17;
        for (int i = 0; i < typeArgs.Length; i++)
        {
            Type type = typeArgs[i];
            if (type is null)
            {
                throw new ArgumentNullException($"{nameof(typeArgs)}[{i}]");
            }
            if (i != typeArgs.Length - 1 && type == typeof(void))
            {
                throw new ArgumentException(SR.Arg_InvalidTypeInSignature, nameof(typeArgs));
            }

            needsCustomType |= type.IsByRef || type.IsByRefLike || type.IsPointer || type.IsFunctionPointer;
        }

        if (needsCustomType)
        {
            return DelegateTypeFactory.GetCustomDelegateType(typeArgs);
        }

        bool returnsVoid = typeArgs[^1] == typeof(void);
        Type definition = DelegateTypeFactory.GetPredefinedType(typeArgs.Length, returnsVoid);
        if (definition == typeof(Action))
        {
            return definition;
        }

        return definition.MakeGenericType(returnsVoid ? typeArgs[..^1] : typeArgs);
    }

    private static partial class DelegateTypeFactory
    {
        internal static Type GetPredefinedType(int signatureLength, bool returnsVoid) =>
            returnsVoid ? signatureLength switch
            {
                1 => typeof(Action),
                2 => typeof(Action<>),
                3 => typeof(Action<,>),
                4 => typeof(Action<,,>),
                5 => typeof(Action<,,,>),
                6 => typeof(Action<,,,,>),
                7 => typeof(Action<,,,,,>),
                8 => typeof(Action<,,,,,,>),
                9 => typeof(Action<,,,,,,,>),
                10 => typeof(Action<,,,,,,,,>),
                11 => typeof(Action<,,,,,,,,,>),
                12 => typeof(Action<,,,,,,,,,,>),
                13 => typeof(Action<,,,,,,,,,,,>),
                14 => typeof(Action<,,,,,,,,,,,,>),
                15 => typeof(Action<,,,,,,,,,,,,,>),
                16 => typeof(Action<,,,,,,,,,,,,,,>),
                _ => typeof(Action<,,,,,,,,,,,,,,,>)
            } : signatureLength switch
            {
                1 => typeof(Func<>),
                2 => typeof(Func<,>),
                3 => typeof(Func<,,>),
                4 => typeof(Func<,,,>),
                5 => typeof(Func<,,,,>),
                6 => typeof(Func<,,,,,>),
                7 => typeof(Func<,,,,,,>),
                8 => typeof(Func<,,,,,,,>),
                9 => typeof(Func<,,,,,,,,>),
                10 => typeof(Func<,,,,,,,,,>),
                11 => typeof(Func<,,,,,,,,,,>),
                12 => typeof(Func<,,,,,,,,,,,>),
                13 => typeof(Func<,,,,,,,,,,,,>),
                14 => typeof(Func<,,,,,,,,,,,,,>),
                15 => typeof(Func<,,,,,,,,,,,,,,>),
                16 => typeof(Func<,,,,,,,,,,,,,,,>),
                _ => typeof(Func<,,,,,,,,,,,,,,,,>)
            };

#if !CORECLR
        internal static Type GetCustomDelegateType(Type[] typeArgs) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionEmit);
#endif
    }
}
