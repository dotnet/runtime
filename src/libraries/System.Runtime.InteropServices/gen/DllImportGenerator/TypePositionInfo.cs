using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Type used to pass on default marshalling details.
    /// </summary>
    internal sealed record DefaultMarshallingInfo (
        CharEncoding CharEncoding
    );

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

        public bool IsManagedReturnPosition { get => this.ManagedIndex == ReturnIndex; }
        public bool IsNativeReturnPosition { get => this.NativeIndex == ReturnIndex; }

        public int ManagedIndex { get; init; }
        public int NativeIndex { get; init; }

        public MarshallingInfo MarshallingAttributeInfo { get; init; }

        public static TypePositionInfo CreateForParameter(IParameterSymbol paramSymbol, DefaultMarshallingInfo defaultInfo, Compilation compilation, GeneratorDiagnostics diagnostics)
        {
            var marshallingInfo = GetMarshallingInfo(paramSymbol.Type, paramSymbol.GetAttributes(), defaultInfo, compilation, diagnostics);
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = paramSymbol.Type,
                InstanceIdentifier = paramSymbol.Name,
                RefKind = paramSymbol.RefKind,
                RefKindSyntax = RefKindToSyntax(paramSymbol.RefKind),
                MarshallingAttributeInfo = marshallingInfo
            };

            return typeInfo;
        }

        public static TypePositionInfo CreateForType(ITypeSymbol type, IEnumerable<AttributeData> attributes, DefaultMarshallingInfo defaultInfo, Compilation compilation, GeneratorDiagnostics diagnostics)
        {
            var marshallingInfo = GetMarshallingInfo(type, attributes, defaultInfo, compilation, diagnostics);
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = type,
                InstanceIdentifier = string.Empty,
                RefKind = RefKind.None,
                RefKindSyntax = SyntaxKind.None,
                MarshallingAttributeInfo = marshallingInfo
            };

            return typeInfo;
        }

        public static TypePositionInfo CreateForType(ITypeSymbol type, MarshallingInfo marshallingInfo)
        {
            var typeInfo = new TypePositionInfo()
            {
                ManagedType = type,
                InstanceIdentifier = string.Empty,
                RefKind = RefKind.None,
                RefKindSyntax = SyntaxKind.None,
                MarshallingAttributeInfo = marshallingInfo
            };

            return typeInfo;
        }

        private static MarshallingInfo GetMarshallingInfo(ITypeSymbol type, IEnumerable<AttributeData> attributes, DefaultMarshallingInfo defaultInfo, Compilation compilation, GeneratorDiagnostics diagnostics)
        {
            // Look at attributes passed in - usage specific.
            foreach (var attrData in attributes)
            {
                INamedTypeSymbol attributeClass = attrData.AttributeClass!;

                if (SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute), attributeClass))
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute
                    return CreateMarshalAsInfo(attrData, defaultInfo, diagnostics);
                }
                else if (SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute), attributeClass))
                {
                    return CreateNativeMarshallingInfo(type, compilation, attrData, allowGetPinnableReference: false);
                }
            }

            // If we aren't overriding the marshalling at usage time,
            // then fall back to the information on the element type itself.
            foreach (var attrData in type.GetAttributes())
            {
                INamedTypeSymbol attributeClass = attrData.AttributeClass!;

                if (SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(TypeNames.BlittableTypeAttribute), attributeClass))
                {
                    return new BlittableTypeAttributeInfo();
                }
                else if (SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(TypeNames.NativeMarshallingAttribute), attributeClass))
                {
                    return CreateNativeMarshallingInfo(type, compilation, attrData, allowGetPinnableReference: true);
                }
                else if (SymbolEqualityComparer.Default.Equals(compilation.GetTypeByMetadataName(TypeNames.GeneratedMarshallingAttribute), attributeClass))
                {
                    return type.IsConsideredBlittable() ? new BlittableTypeAttributeInfo() : new GeneratedNativeMarshallingAttributeInfo(null! /* TODO: determine naming convention */);
                }
            }

            // If the type doesn't have custom attributes that dictate marshalling,
            // then consider the type itself.
            if (TryCreateTypeBasedMarshallingInfo(type, compilation, out MarshallingInfo infoMaybe))
            {
                return infoMaybe;
            }

            // No marshalling info was computed, but a character encoding was provided.
            // If the type is a character or string then pass on these details.
            if (defaultInfo.CharEncoding != CharEncoding.Undefined
                && (type.SpecialType == SpecialType.System_Char
                    || type.SpecialType == SpecialType.System_String))
            {
                return new MarshallingInfoStringSupport(defaultInfo.CharEncoding);
            }

            return NoMarshallingInfo.Instance;

            static MarshalAsInfo CreateMarshalAsInfo(AttributeData attrData, DefaultMarshallingInfo defaultInfo, GeneratorDiagnostics diagnostics)
            {
                object unmanagedTypeObj = attrData.ConstructorArguments[0].Value!;
                UnmanagedType unmanagedType = unmanagedTypeObj is short
                    ? (UnmanagedType)(short)unmanagedTypeObj
                    : (UnmanagedType)unmanagedTypeObj;
                if (!Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                    || unmanagedType == UnmanagedType.CustomMarshaler
                    || unmanagedType == UnmanagedType.SafeArray)
                {
                    diagnostics.ReportConfigurationNotSupported(attrData, nameof(UnmanagedType), unmanagedType.ToString());
                }
                bool isArrayType = unmanagedType == UnmanagedType.LPArray || unmanagedType == UnmanagedType.ByValArray;
                UnmanagedType unmanagedArraySubType = (UnmanagedType)ArrayMarshalAsInfo.UnspecifiedData;
                int arraySizeConst = ArrayMarshalAsInfo.UnspecifiedData;
                short arraySizeParamIndex = ArrayMarshalAsInfo.UnspecifiedData;

                // All other data on attribute is defined as NamedArguments.
                foreach (var namedArg in attrData.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        default:
                            Debug.Fail($"An unknown member was found on {nameof(MarshalAsAttribute)}");
                            continue;
                        case nameof(MarshalAsAttribute.SafeArraySubType):
                        case nameof(MarshalAsAttribute.SafeArrayUserDefinedSubType):
                        case nameof(MarshalAsAttribute.IidParameterIndex):
                        case nameof(MarshalAsAttribute.MarshalTypeRef):
                        case nameof(MarshalAsAttribute.MarshalType):
                        case nameof(MarshalAsAttribute.MarshalCookie):
                            diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                            break;
                        case nameof(MarshalAsAttribute.ArraySubType):
                            if (!isArrayType)
                            {
                                diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                            }
                            unmanagedArraySubType = (UnmanagedType)namedArg.Value.Value!;
                            break;
                        case nameof(MarshalAsAttribute.SizeConst):
                            if (!isArrayType)
                            {
                                diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                            }
                            arraySizeConst = (int)namedArg.Value.Value!;
                            break;
                        case nameof(MarshalAsAttribute.SizeParamIndex):
                            if (!isArrayType)
                            {
                                diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                            }
                            arraySizeParamIndex = (short)namedArg.Value.Value!;
                            break;
                    }
                }

                return isArrayType
                    ? new ArrayMarshalAsInfo(
                        UnmanagedArrayType: (UnmanagedArrayType)unmanagedType,
                        UnmanagedArraySubType: unmanagedArraySubType,
                        ArraySizeConst: arraySizeConst,
                        ArraySizeParamIndex: arraySizeParamIndex,
                        CharEncoding: defaultInfo.CharEncoding
                    )
                    : new MarshalAsInfo(unmanagedType, defaultInfo.CharEncoding);
            }

            static NativeMarshallingAttributeInfo CreateNativeMarshallingInfo(ITypeSymbol type, Compilation compilation, AttributeData attrData, bool allowGetPinnableReference)
            {
                ITypeSymbol spanOfByte = compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!.Construct(compilation.GetSpecialType(SpecialType.System_Byte));
                INamedTypeSymbol nativeType = (INamedTypeSymbol)attrData.ConstructorArguments[0].Value!;
                SupportedMarshallingMethods methods = 0;
                IPropertySymbol? valueProperty = ManualTypeMarshallingHelper.FindValueProperty(nativeType);
                foreach (var ctor in nativeType.Constructors)
                {
                    if (ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type)
                        && (valueProperty is null or { GetMethod: not null }))
                    {
                        methods |= SupportedMarshallingMethods.ManagedToNative;
                    }
                    else if (ManualTypeMarshallingHelper.IsStackallocConstructor(ctor, type, spanOfByte)
                        && (valueProperty is null or { GetMethod: not null }))
                    {
                        methods |= SupportedMarshallingMethods.ManagedToNativeStackalloc;
                    }
                }

                if (ManualTypeMarshallingHelper.HasToManagedMethod(nativeType, type)
                    && (valueProperty is null or { SetMethod: not null }))
                {
                    methods |= SupportedMarshallingMethods.NativeToManaged;
                }

                if (allowGetPinnableReference && ManualTypeMarshallingHelper.FindGetPinnableReference(type) != null)
                {
                    methods |= SupportedMarshallingMethods.Pinning;
                }

                if (methods == 0)
                {
                    // TODO: Diagnostic since no marshalling methods are supported.
                }

                return new NativeMarshallingAttributeInfo(
                    nativeType,
                    valueProperty?.Type,
                    methods);
            }

            static bool TryCreateTypeBasedMarshallingInfo(ITypeSymbol type, Compilation compilation, out MarshallingInfo marshallingInfo)
            {
                var conversion = compilation.ClassifyCommonConversion(type, compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_SafeHandle)!);
                if (conversion.Exists &&
                    conversion.IsImplicit &&
                    conversion.IsReference &&
                    !type.IsAbstract)
                {
                    marshallingInfo = new SafeHandleMarshallingInfo();
                    return true;
                }
                marshallingInfo = NoMarshallingInfo.Instance;
                return false;
            }
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
