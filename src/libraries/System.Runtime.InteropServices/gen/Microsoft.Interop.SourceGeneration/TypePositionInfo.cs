// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{

    /// <summary>
    /// Describes how to marshal the contents of a value in comparison to the value itself.
    /// Only makes sense for array-like types. For example, an "out" array doesn't change the
    /// pointer to the array value, but it marshals the contents of the native array back to the
    /// contents of the managed array.
    /// </summary>
    [Flags]
    public enum ByValueContentsMarshalKind
    {
        /// <summary>
        /// Marshal contents from managed to native only.
        /// This is the default behavior.
        /// </summary>
        Default = 0x0,
        /// <summary>
        /// Marshal contents from managed to native only.
        /// This is the default behavior.
        /// </summary>
        In = 0x1,
        /// <summary>
        /// Marshal contents from native to managed only.
        /// </summary>
        Out = 0x2,
        /// <summary>
        /// Marshal contents both to and from native.
        /// </summary>
        InOut = In | Out
    }

    /// <summary>
    /// Positional type information involved in unmanaged/managed scenarios.
    /// </summary>
    public sealed record TypePositionInfo(ManagedTypeInfo ManagedType, MarshallingInfo MarshallingAttributeInfo)
    {
        public const int UnsetIndex = int.MinValue;
        public const int ReturnIndex = UnsetIndex + 1;
        public const int ExceptionIndex = UnsetIndex + 2;

        public static bool IsSpecialIndex(int index)
        {
            return index is UnsetIndex or ReturnIndex or ExceptionIndex;
        }

        public static int IncrementIndex(int index)
        {
            return IsSpecialIndex(index) ? index : index + 1;
        }

        public string InstanceIdentifier { get; init; } = string.Empty;

        public RefKind RefKind { get; init; } = RefKind.None;

        public bool IsByRef => RefKind != RefKind.None;

        public ScopedKind ScopedKind { get; init; } = ScopedKind.None;

        public ByValueContentsMarshalKind ByValueContentsMarshalKind { get; init; }

        public (Location? InLocation, Location? OutLocation) ByValueMarshalAttributeLocations { get; init; }

        public bool IsManagedReturnPosition { get => ManagedIndex == ReturnIndex; }
        public bool IsNativeReturnPosition { get => NativeIndex == ReturnIndex; }
        public bool IsManagedExceptionPosition { get => ManagedIndex == ExceptionIndex; }

        public int ManagedIndex { get; init; } = UnsetIndex;
        public int NativeIndex { get; init; } = UnsetIndex;

        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol, MarshallingInfo marshallingInfo, Compilation compilation)
        {
            var (byValueContentsMarshalKind, inLocation, outLocation) = GetByValueContentsMarshalKind(paramSymbol.GetAttributes(), compilation);

            var typeInfo = new TypePositionInfo(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(paramSymbol.Type), marshallingInfo)
            {
                InstanceIdentifier = ParseToken(paramSymbol.Name).IsReservedKeyword() ? $"@{paramSymbol.Name}" : paramSymbol.Name,
                RefKind = paramSymbol.RefKind,
                ByValueContentsMarshalKind = byValueContentsMarshalKind,
                ByValueMarshalAttributeLocations = (inLocation, outLocation),
                ScopedKind = paramSymbol.ScopedKind
            };

            return typeInfo;
        }

        public static Location GetLocation(TypePositionInfo info, IMethodSymbol methodSymbol)
        {
            if (info.ManagedIndex is UnsetIndex)
                return Location.None;

            if (info.ManagedIndex is ReturnIndex or ExceptionIndex)
                return methodSymbol.Locations[0];

            return methodSymbol.Parameters[info.ManagedIndex].Locations[0];
        }

        private static (ByValueContentsMarshalKind, Location? inAttribute, Location? outAttribute) GetByValueContentsMarshalKind(IEnumerable<AttributeData> attributes, Compilation compilation)
        {
            INamedTypeSymbol outAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_OutAttribute)!;
            INamedTypeSymbol inAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_InAttribute)!;

            ByValueContentsMarshalKind marshalKind = ByValueContentsMarshalKind.Default;
            Location? inAttributeLocation = null;
            Location? outAttributeLocation = null;

            foreach (AttributeData attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, outAttributeType))
                {
                    marshalKind |= ByValueContentsMarshalKind.Out;
                    outAttributeLocation = attr.ApplicationSyntaxReference.SyntaxTree.GetLocation(attr.ApplicationSyntaxReference.Span);
                }
                else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, inAttributeType))
                {
                    marshalKind |= ByValueContentsMarshalKind.In;
                    inAttributeLocation = attr.ApplicationSyntaxReference.SyntaxTree.GetLocation(attr.ApplicationSyntaxReference.Span);
                }
            }

            return (marshalKind, inAttributeLocation, outAttributeLocation);
        }
    }
}
