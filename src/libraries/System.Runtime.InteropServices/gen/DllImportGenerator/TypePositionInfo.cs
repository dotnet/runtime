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
    internal enum ByValueContentsMarshalKind
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
    internal sealed record TypePositionInfo
    {
        public const int UnsetIndex = int.MinValue;
        public const int ReturnIndex = UnsetIndex + 1;

// We don't need the warnings around not setting the various
// non-nullable fields/properties on this type in the constructor
// since we always use a property initializer.
#pragma warning disable 8618
        private TypePositionInfo()
        {
            this.ManagedIndex = UnsetIndex;
            this.NativeIndex = UnsetIndex;
        }
#pragma warning restore

        public string InstanceIdentifier { get; init; }
        public ITypeSymbol ManagedType { get; init; }

        public RefKind RefKind { get; init; }
        public SyntaxKind RefKindSyntax { get; init; }

        public bool IsByRef => RefKind != RefKind.None;

        public ByValueContentsMarshalKind ByValueContentsMarshalKind { get; init; }

        public bool IsManagedReturnPosition { get => this.ManagedIndex == ReturnIndex; }
        public bool IsNativeReturnPosition { get => this.NativeIndex == ReturnIndex; }

        public int ManagedIndex { get; init; }
        public int NativeIndex { get; init; }

        public MarshallingInfo MarshallingAttributeInfo { get; init; }

        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol, MarshallingInfo marshallingInfo, Compilation compilation)
        {
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = paramSymbol.Type,
                InstanceIdentifier = ParseToken(paramSymbol.Name).IsReservedKeyword() ? $"@{paramSymbol.Name}" : paramSymbol.Name,
                RefKind = paramSymbol.RefKind,
                RefKindSyntax = RefKindToSyntax(paramSymbol.RefKind),
                MarshallingAttributeInfo = marshallingInfo,
                ByValueContentsMarshalKind = GetByValueContentsMarshalKind(paramSymbol.GetAttributes(), compilation)
            };

            return typeInfo;
        }

        public static TypePositionInfo CreateForType(ITypeSymbol type, MarshallingInfo marshallingInfo, string identifier = "")
        {
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = type,
                InstanceIdentifier = identifier,
                RefKind = RefKind.None,
                RefKindSyntax = SyntaxKind.None,
                MarshallingAttributeInfo = marshallingInfo
            };

            return typeInfo;
        }

        private static ByValueContentsMarshalKind GetByValueContentsMarshalKind(IEnumerable<AttributeData> attributes, Compilation compilation)
        {
            INamedTypeSymbol outAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_OutAttribute)!;
            INamedTypeSymbol inAttributeType = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_InAttribute)!;

            ByValueContentsMarshalKind marshalKind = ByValueContentsMarshalKind.Default;

            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, outAttributeType))
                {
                    marshalKind |= ByValueContentsMarshalKind.Out;
                }
                else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, inAttributeType))
                {
                    marshalKind |= ByValueContentsMarshalKind.In;
                }
            }

            return marshalKind;
        }

        private static SyntaxKind RefKindToSyntax(RefKind refKind)
        {
            return refKind switch
            {
                RefKind.In => SyntaxKind.InKeyword,
                RefKind.Ref => SyntaxKind.RefKeyword,
                RefKind.Out => SyntaxKind.OutKeyword,
                RefKind.None => SyntaxKind.None,
                _ => throw new NotImplementedException("Support for some RefKind"),
            };
        }
    }
}
