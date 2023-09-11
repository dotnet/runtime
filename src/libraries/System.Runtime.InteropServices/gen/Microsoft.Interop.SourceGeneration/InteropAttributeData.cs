// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Flags used to indicate members on source-generated interop attributes.
    /// </summary>
    [Flags]
    public enum InteropAttributeMember
    {
        None = 0,
        SetLastError = 1 << 0,
        StringMarshalling = 1 << 1,
        StringMarshallingCustomType = 1 << 2,
        All = ~None
    }

    /// <summary>
    /// Common data for all source-generated-interop trigger attributes.
    /// This type and derived types should not have any reference that would keep a compilation alive.
    /// </summary>
    public record InteropAttributeData
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public InteropAttributeMember IsUserDefined { get; init; }
        public bool SetLastError { get; init; }
        public StringMarshalling StringMarshalling { get; init; }
        public ManagedTypeInfo? StringMarshallingCustomType { get; init; }
    }

    /// <summary>
    /// Common data for all source-generated-interop trigger attributes that also includes a reference to the Roslyn symbol for StringMarshallingCustomType.
    /// See <seealso cref="InteropAttributeData"/> for a type that doesn't keep a compilation alive.
    /// </summary>
    public record InteropAttributeCompilationData
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public InteropAttributeMember IsUserDefined { get; init; }
        public bool SetLastError { get; init; }
        public StringMarshalling StringMarshalling { get; init; }
        public INamedTypeSymbol? StringMarshallingCustomType { get; init; }
    }

    public static class InteropAttributeDataExtensions
    {
        public static T WithValuesFromNamedArguments<T>(this T t, ImmutableDictionary<string, TypedConstant> namedArguments) where T : InteropAttributeCompilationData
        {
            InteropAttributeMember userDefinedValues = InteropAttributeMember.None;
            bool setLastError = false;
            StringMarshalling stringMarshalling = StringMarshalling.Custom;
            INamedTypeSymbol? stringMarshallingCustomType = null;

            if (namedArguments.TryGetValue(nameof(InteropAttributeCompilationData.SetLastError), out TypedConstant setLastErrorValue))
            {
                userDefinedValues |= InteropAttributeMember.SetLastError;
                if (setLastErrorValue.Value is not bool)
                {
                    return null;
                }
                setLastError = (bool)setLastErrorValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(InteropAttributeCompilationData.StringMarshalling), out TypedConstant stringMarshallingValue))
            {
                userDefinedValues |= InteropAttributeMember.StringMarshalling;
                // TypedConstant's Value property only contains primitive values.
                if (stringMarshallingValue.Value is not int)
                {
                    return null;
                }
                // A boxed primitive can be unboxed to an enum with the same underlying type.
                stringMarshalling = (StringMarshalling)stringMarshallingValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(InteropAttributeCompilationData.StringMarshallingCustomType), out TypedConstant stringMarshallingCustomTypeValue))
            {
                userDefinedValues |= InteropAttributeMember.StringMarshallingCustomType;
                if (stringMarshallingCustomTypeValue.Value is not INamedTypeSymbol)
                {
                    return null;
                }
                stringMarshallingCustomType = (INamedTypeSymbol)stringMarshallingCustomTypeValue.Value;
            }
            return t with
            {
                IsUserDefined = userDefinedValues,
                SetLastError = setLastError,
                StringMarshalling = stringMarshalling,
                StringMarshallingCustomType = stringMarshallingCustomType
            };
        }
    }
}
