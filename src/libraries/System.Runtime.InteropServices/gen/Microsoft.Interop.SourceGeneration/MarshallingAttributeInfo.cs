// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Interop
{
    /// <summary>
    /// Type used to pass on default marshalling details.
    /// </summary>
    /// <remarks>
    /// This type used to pass default marshalling details to the various marshalling info parsers.
    /// Since it contains a <see cref="INamedTypeSymbol"/>, it should not be used as a field on any types
    /// derived from <see cref="MarshallingInfo"/>. See remarks on <see cref="MarshallingInfo"/>.
    /// </remarks>
    public sealed record DefaultMarshallingInfo(
        CharEncoding CharEncoding,
        INamedTypeSymbol? StringMarshallingCustomType
    );

    // The following types are modeled to fit with the current prospective spec
    // for C# vNext discriminated unions. Once discriminated unions are released,
    // these should be updated to be implemented as a discriminated union.

    /// <summary>
    /// Base type for marshalling information
    /// </summary>
    /// <remarks>
    /// Types derived from this are used to represent the stub information calculated from the semantic model.
    /// To support incremental generation, they must not include any types derived from <see cref="ISymbol"/>.
    /// </remarks>
    public abstract record MarshallingInfo
    {
        protected MarshallingInfo()
        { }
    }

    /// <summary>
    /// No marshalling information exists for the type.
    /// </summary>
    public sealed record NoMarshallingInfo : MarshallingInfo
    {
        public static readonly MarshallingInfo Instance = new NoMarshallingInfo();

        private NoMarshallingInfo() { }
    }

    /// <summary>
    /// Marshalling information is lacking because of support not because it is
    /// unknown or non-existent.
    /// </summary>
    /// <remarks>
    /// An indication of "missing support" will trigger the fallback logic, which is
    /// the forwarder marshaller.
    /// </remarks>
    public record MissingSupportMarshallingInfo : MarshallingInfo;

    /// <summary>
    /// Character encoding enumeration.
    /// </summary>
    public enum CharEncoding
    {
        Undefined,
        Utf8,
        Utf16,
        Custom
    }

    /// <summary>
    /// Details that are required when scenario supports strings.
    /// </summary>
    public record MarshallingInfoStringSupport(
        CharEncoding CharEncoding
    ) : MarshallingInfo;

    /// <summary>
    /// The provided type was determined to be an "unmanaged" type that can be passed as-is to native code.
    /// </summary>
    /// <param name="IsStrictlyBlittable">Indicates if the type is blittable as defined by the built-in .NET marshallers.</param>
    public sealed record UnmanagedBlittableMarshallingInfo(
        bool IsStrictlyBlittable
    ) : MarshallingInfo;

    public abstract record CountInfo
    {
        private protected CountInfo() { }
    }

    public sealed record NoCountInfo : CountInfo
    {
        public static readonly NoCountInfo Instance = new NoCountInfo();

        private NoCountInfo() { }
    }

    public sealed record ConstSizeCountInfo(int Size) : CountInfo;

    public sealed record CountElementCountInfo(TypePositionInfo ElementInfo) : CountInfo
    {
        public const string ReturnValueElementName = "return-value";
    }

    public sealed record SizeAndParamIndexInfo(int ConstSize, TypePositionInfo? ParamAtIndex) : CountInfo
    {
        public const int UnspecifiedConstSize = -1;

        public const TypePositionInfo UnspecifiedParam = null;

        public static readonly SizeAndParamIndexInfo Unspecified = new(UnspecifiedConstSize, UnspecifiedParam);
    }

    /// <summary>
    /// Custom type marshalling via MarshalUsingAttribute or NativeMarshallingAttribute
    /// </summary>
    public record NativeMarshallingAttributeInfo(
        ManagedTypeInfo EntryPointType,
        CustomTypeMarshallers Marshallers) : MarshallingInfo;

    /// <summary>
    /// Custom type marshalling via MarshalUsingAttribute or NativeMarshallingAttribute for a linear collection
    /// </summary>
    public sealed record NativeLinearCollectionMarshallingInfo(
        ManagedTypeInfo EntryPointType,
        CustomTypeMarshallers Marshallers,
        CountInfo ElementCountInfo,
        ManagedTypeInfo PlaceholderTypeParameter) : NativeMarshallingAttributeInfo(
            EntryPointType,
            Marshallers);

    /// <summary>
    /// Marshalling information is lacking because of support not because it is
    /// unknown or non-existent. Includes information about element types in case
    /// we need to rehydrate the marshalling info into an attribute for the fallback marshaller.
    /// </summary>
    /// <remarks>
    /// An indication of "missing support" will trigger the fallback logic, which is
    /// the forwarder marshaller.
    /// </remarks>
    public sealed record MissingSupportCollectionMarshallingInfo(CountInfo CountInfo, MarshallingInfo ElementMarshallingInfo) : MissingSupportMarshallingInfo;


    /// <summary>
    /// Marshal an exception based on the same rules as the built-in COM system based on the unmanaged type of the native return marshaller.
    /// </summary>
    public sealed record ComExceptionMarshalling : MarshallingInfo
    {
        internal static MarshallingInfo CreateSpecificMarshallingInfo(ManagedTypeInfo unmanagedReturnType)
        {
            return unmanagedReturnType switch
            {
                SpecialTypeInfo(_, _, SpecialType.System_Void) => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsVoidMarshaller}", unmanagedReturnType),
                SpecialTypeInfo(_, _, SpecialType.System_Int32) => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsHResultMarshaller}<int>", unmanagedReturnType),
                SpecialTypeInfo(_, _, SpecialType.System_UInt32) => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsHResultMarshaller}<uint>", unmanagedReturnType),
                SpecialTypeInfo(_, _, SpecialType.System_Single) => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsNaNMarshaller}<float>", unmanagedReturnType),
                SpecialTypeInfo(_, _, SpecialType.System_Double) => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsNaNMarshaller}<double>", unmanagedReturnType),
                _ => CreateWellKnownComExceptionMarshallingData($"{TypeNames.ExceptionAsDefaultMarshaller}<{MarshallerHelpers.GetCompatibleGenericTypeParameterSyntax(SyntaxFactory.ParseTypeName(unmanagedReturnType.FullTypeName))}>", unmanagedReturnType),
            };

            static NativeMarshallingAttributeInfo CreateWellKnownComExceptionMarshallingData(string marshallerName, ManagedTypeInfo unmanagedType)
            {
                ManagedTypeInfo marshallerTypeInfo = new ReferenceTypeInfo(marshallerName, marshallerName);
                return new NativeMarshallingAttributeInfo(marshallerTypeInfo,
                    new CustomTypeMarshallers(ImmutableDictionary<MarshalMode, CustomTypeMarshallerData>.Empty.Add(
                        MarshalMode.UnmanagedToManagedOut,
                        new CustomTypeMarshallerData(
                            marshallerTypeInfo,
                            unmanagedType,
                            HasState: false,
                            MarshallerShape.ToUnmanaged,
                            IsStrictlyBlittable: true,
                            BufferElementType: null,
                            CollectionElementType: null,
                            CollectionElementMarshallingInfo: null
                            ))));
            }
        }
    }
}
