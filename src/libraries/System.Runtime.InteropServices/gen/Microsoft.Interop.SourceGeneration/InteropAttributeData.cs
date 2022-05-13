// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
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
    /// Common data for all source-generated-interop trigger attributes
    /// </summary>
    public record InteropAttributeData
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
        public static T WithValuesFromNamedArguments<T>(this T t, ImmutableDictionary<string, TypedConstant> namedArguments) where T : InteropAttributeData
        {
            InteropAttributeMember userDefinedValues = InteropAttributeMember.None;
            bool setLastError = false;
            StringMarshalling stringMarshalling = StringMarshalling.Custom;
            INamedTypeSymbol? stringMarshallingCustomType = null;

            if (namedArguments.TryGetValue(nameof(InteropAttributeData.SetLastError), out TypedConstant setLastErrorValue))
            {
                userDefinedValues |= InteropAttributeMember.SetLastError;
                if (setLastErrorValue.Value is not bool)
                {
                    return null;
                }
                setLastError = (bool)setLastErrorValue.Value!;
            }
            if (namedArguments.TryGetValue(nameof(InteropAttributeData.StringMarshalling), out TypedConstant stringMarshallingValue))
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
            if (namedArguments.TryGetValue(nameof(InteropAttributeData.StringMarshallingCustomType), out TypedConstant stringMarshallingCustomTypeValue))
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
