// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
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
    /// This type used to pass default marshalling details to <see cref="MarshallingAttributeInfoParser"/>.
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
    /// Simple User-application of System.Runtime.InteropServices.MarshalAsAttribute
    /// </summary>
    public sealed record MarshalAsInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding) : MarshallingInfoStringSupport(CharEncoding)
    {
        // UnmanagedType.LPUTF8Str is not in netstandard2.0, so we define a constant for the value here.
        // See https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype
        internal const UnmanagedType UnmanagedType_LPUTF8Str = (UnmanagedType)0x30;
    }

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
    /// The type of the element is a SafeHandle-derived type with no marshalling attributes.
    /// </summary>
    public sealed record SafeHandleMarshallingInfo(bool AccessibleDefaultConstructor, bool IsAbstract) : MarshallingInfo;

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

    public sealed class MarshallingAttributeInfoParser
    {
        private readonly Compilation _compilation;
        private readonly IGeneratorDiagnostics _diagnostics;
        private readonly DefaultMarshallingInfo _defaultInfo;
        private readonly ISymbol _contextSymbol;
        private readonly ITypeSymbol _marshalAsAttribute;
        private readonly ITypeSymbol _marshalUsingAttribute;

        public MarshallingAttributeInfoParser(
            Compilation compilation,
            IGeneratorDiagnostics diagnostics,
            DefaultMarshallingInfo defaultInfo,
            ISymbol contextSymbol)
        {
            _compilation = compilation;
            _diagnostics = diagnostics;
            _defaultInfo = defaultInfo;
            _contextSymbol = contextSymbol;
            _marshalAsAttribute = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
            _marshalUsingAttribute = compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute)!;
        }

        public MarshallingInfo ParseMarshallingInfo(
            ITypeSymbol managedType,
            IEnumerable<AttributeData> useSiteAttributes)
        {
            return ParseMarshallingInfo(managedType, useSiteAttributes, ImmutableHashSet<string>.Empty);
        }

        private MarshallingInfo ParseMarshallingInfo(
            ITypeSymbol managedType,
            IEnumerable<AttributeData> useSiteAttributes,
            ImmutableHashSet<string> inspectedElements)
        {
            Dictionary<int, AttributeData> marshallingAttributesByIndirectionDepth = new();
            int maxIndirectionLevelDataProvided = 0;
            foreach (AttributeData attribute in useSiteAttributes)
            {
                if (TryGetAttributeIndirectionLevel(attribute, out int indirectionLevel))
                {
                    if (marshallingAttributesByIndirectionDepth.ContainsKey(indirectionLevel))
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(attribute, nameof(SR.DuplicateMarshallingInfo), indirectionLevel.ToString());
                        return NoMarshallingInfo.Instance;
                    }
                    marshallingAttributesByIndirectionDepth.Add(indirectionLevel, attribute);
                    maxIndirectionLevelDataProvided = Math.Max(maxIndirectionLevelDataProvided, indirectionLevel);
                }
            }

            int maxIndirectionDepthUsed = 0;
            MarshallingInfo info = GetMarshallingInfo(
                managedType,
                marshallingAttributesByIndirectionDepth,
                indirectionLevel: 0,
                inspectedElements,
                ref maxIndirectionDepthUsed);
            if (maxIndirectionDepthUsed < maxIndirectionLevelDataProvided)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(
                    marshallingAttributesByIndirectionDepth[maxIndirectionLevelDataProvided],
                    nameof(SR.ExtraneousMarshallingInfo),
                    maxIndirectionLevelDataProvided.ToString(),
                    maxIndirectionDepthUsed.ToString());
            }
            return info;
        }

        private MarshallingInfo GetMarshallingInfo(
            ITypeSymbol type,
            Dictionary<int, AttributeData> useSiteAttributes,
            int indirectionLevel,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionDepthUsed)
        {
            maxIndirectionDepthUsed = Math.Max(indirectionLevel, maxIndirectionDepthUsed);
            CountInfo parsedCountInfo = NoCountInfo.Instance;

            if (useSiteAttributes.TryGetValue(indirectionLevel, out AttributeData useSiteAttribute))
            {
                INamedTypeSymbol attributeClass = useSiteAttribute.AttributeClass!;

                if (indirectionLevel == 0
                    && SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute), attributeClass))
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute
                    return CreateInfoFromMarshalAs(type, useSiteAttribute, inspectedElements, ref maxIndirectionDepthUsed);
                }
                else if (SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute), attributeClass))
                {
                    if (parsedCountInfo != NoCountInfo.Instance)
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(useSiteAttribute, nameof(SR.DuplicateCountInfo));
                        return NoMarshallingInfo.Instance;
                    }
                    parsedCountInfo = CreateCountInfo(useSiteAttribute, inspectedElements);
                    if (useSiteAttribute.ConstructorArguments.Length != 0)
                    {
                        return CreateNativeMarshallingInfo(
                            type,
                            (INamedTypeSymbol)useSiteAttribute.ConstructorArguments[0].Value!,
                            useSiteAttribute,
                            isMarshalUsingAttribute: true,
                            indirectionLevel,
                            parsedCountInfo,
                            useSiteAttributes,
                            inspectedElements,
                            ref maxIndirectionDepthUsed);
                    }
                }
            }

            // If we aren't overriding the marshalling at usage time,
            // then fall back to the information on the element type itself.
            foreach (AttributeData typeAttribute in type.GetAttributes())
            {
                INamedTypeSymbol attributeClass = typeAttribute.AttributeClass!;

                if (attributeClass.ToDisplayString() == TypeNames.NativeMarshallingAttribute)
                {
                    return CreateNativeMarshallingInfo(
                        type,
                        (INamedTypeSymbol)typeAttribute.ConstructorArguments[0].Value!,
                        typeAttribute,
                        isMarshalUsingAttribute: false,
                        indirectionLevel,
                        parsedCountInfo,
                        useSiteAttributes,
                        inspectedElements,
                        ref maxIndirectionDepthUsed);
                }
            }

            // If the type doesn't have custom attributes that dictate marshalling,
            // then consider the type itself.
            if (TryCreateTypeBasedMarshallingInfo(
                type,
                parsedCountInfo,
                indirectionLevel,
                useSiteAttributes,
                inspectedElements,
                ref maxIndirectionDepthUsed,
                out MarshallingInfo infoMaybe))
            {
                return infoMaybe;
            }

            return NoMarshallingInfo.Instance;
        }

        private CountInfo CreateCountInfo(AttributeData marshalUsingData, ImmutableHashSet<string> inspectedElements)
        {
            int? constSize = null;
            string? elementName = null;
            foreach (KeyValuePair<string, TypedConstant> arg in marshalUsingData.NamedArguments)
            {
                if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.ConstantElementCount)
                {
                    constSize = (int)arg.Value.Value!;
                }
                else if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName)
                {
                    if (arg.Value.Value is null)
                    {
                        _diagnostics.ReportConfigurationNotSupported(marshalUsingData, ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName, "null");
                        return NoCountInfo.Instance;
                    }
                    elementName = (string)arg.Value.Value!;
                }
            }

            if (constSize is not null && elementName is not null)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(marshalUsingData, nameof(SR.ConstantAndElementCountInfoDisallowed));
            }
            else if (constSize is not null)
            {
                return new ConstSizeCountInfo(constSize.Value);
            }
            else if (elementName is not null)
            {
                if (inspectedElements.Contains(elementName))
                {
                    throw new CyclicalCountElementInfoException(inspectedElements, elementName);
                }

                try
                {
                    TypePositionInfo? elementInfo = CreateForElementName(elementName, inspectedElements.Add(elementName));
                    if (elementInfo is null)
                    {
                        _diagnostics.ReportConfigurationNotSupported(marshalUsingData, ManualTypeMarshallingHelper.MarshalUsingProperties.CountElementName, elementName);
                        return NoCountInfo.Instance;
                    }
                    return new CountElementCountInfo(elementInfo);
                }
                // Specifically catch the exception when we're trying to inspect the element that started the cycle.
                // This ensures that we've unwound the whole cycle so when we return NoCountInfo.Instance, there will be no cycles in the count info.
                catch (CyclicalCountElementInfoException ex) when (ex.StartOfCycle == elementName)
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(marshalUsingData, nameof(SR.CyclicalCountInfo), elementName);
                    return NoCountInfo.Instance;
                }
            }

            return NoCountInfo.Instance;
        }

        private TypePositionInfo? CreateForParamIndex(AttributeData attrData, int paramIndex, ImmutableHashSet<string> inspectedElements)
        {
            if (!(_contextSymbol is IMethodSymbol method && 0 <= paramIndex && paramIndex < method.Parameters.Length))
            {
                return null;
            }
            IParameterSymbol param = method.Parameters[paramIndex];

            if (inspectedElements.Contains(param.Name))
            {
                throw new CyclicalCountElementInfoException(inspectedElements, param.Name);
            }

            try
            {
                return TypePositionInfo.CreateForParameter(
                    param,
                    ParseMarshallingInfo(param.Type, param.GetAttributes(), inspectedElements.Add(param.Name)), _compilation) with
                { ManagedIndex = paramIndex };
            }
            // Specifically catch the exception when we're trying to inspect the element that started the cycle.
            // This ensures that we've unwound the whole cycle so when we return, there will be no cycles in the count info.
            catch (CyclicalCountElementInfoException ex) when (ex.StartOfCycle == param.Name)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.CyclicalCountInfo), param.Name);
                return SizeAndParamIndexInfo.UnspecifiedParam;
            }
        }

        private TypePositionInfo? CreateForElementName(string elementName, ImmutableHashSet<string> inspectedElements)
        {
            if (_contextSymbol is IMethodSymbol method)
            {
                if (elementName == CountElementCountInfo.ReturnValueElementName)
                {
                    return new TypePositionInfo(
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(method.ReturnType),
                        ParseMarshallingInfo(method.ReturnType, method.GetReturnTypeAttributes(), inspectedElements)) with
                    {
                        ManagedIndex = TypePositionInfo.ReturnIndex
                    };
                }

                for (int i = 0; i < method.Parameters.Length; i++)
                {
                    IParameterSymbol param = method.Parameters[i];
                    if (param.Name == elementName)
                    {
                        return TypePositionInfo.CreateForParameter(param, ParseMarshallingInfo(param.Type, param.GetAttributes(), inspectedElements), _compilation) with { ManagedIndex = i };
                    }
                }
            }
            else if (_contextSymbol is INamedTypeSymbol)
            {
                // TODO: Handle when we create a struct marshalling generator
                // Do we want to support CountElementName pointing to only fields, or properties as well?
                // If only fields, how do we handle properties with generated backing fields?
            }

            return null;
        }

        private MarshallingInfo CreateInfoFromMarshalAs(
            ITypeSymbol type,
            AttributeData attrData,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionDepthUsed)
        {
            object unmanagedTypeObj = attrData.ConstructorArguments[0].Value!;
            UnmanagedType unmanagedType = unmanagedTypeObj is short unmanagedTypeAsShort
                ? (UnmanagedType)unmanagedTypeAsShort
                : (UnmanagedType)unmanagedTypeObj;
            if (!Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.SafeArray)
            {
                _diagnostics.ReportConfigurationNotSupported(attrData, nameof(UnmanagedType), unmanagedType.ToString());
            }

            bool isArrayType = unmanagedType == UnmanagedType.LPArray || unmanagedType == UnmanagedType.ByValArray;
            UnmanagedType elementUnmanagedType = (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize;
            SizeAndParamIndexInfo arraySizeInfo = SizeAndParamIndexInfo.Unspecified;

            // All other data on attribute is defined as NamedArguments.
            foreach (KeyValuePair<string, TypedConstant> namedArg in attrData.NamedArguments)
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
                        _diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        break;
                    case nameof(MarshalAsAttribute.ArraySubType):
                        if (!isArrayType)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        }
                        elementUnmanagedType = (UnmanagedType)namedArg.Value.Value!;
                        break;
                    case nameof(MarshalAsAttribute.SizeConst):
                        if (!isArrayType)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        }
                        arraySizeInfo = arraySizeInfo with { ConstSize = (int)namedArg.Value.Value! };
                        break;
                    case nameof(MarshalAsAttribute.SizeParamIndex):
                        if (!isArrayType)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attrData, $"{attrData.AttributeClass!.Name}{Type.Delimiter}{namedArg.Key}");
                        }
                        TypePositionInfo? paramIndexInfo = CreateForParamIndex(attrData, (short)namedArg.Value.Value!, inspectedElements);

                        if (paramIndexInfo is null)
                        {
                            _diagnostics.ReportConfigurationNotSupported(attrData, nameof(MarshalAsAttribute.SizeParamIndex), namedArg.Value.Value.ToString());
                        }
                        arraySizeInfo = arraySizeInfo with { ParamAtIndex = paramIndexInfo };
                        break;
                }
            }

            if (isArrayType)
            {
                if (type is not IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
                {
                    _diagnostics.ReportConfigurationNotSupported(attrData, nameof(UnmanagedType), unmanagedType.ToString());
                    return NoMarshallingInfo.Instance;
                }

                MarshallingInfo elementMarshallingInfo = NoMarshallingInfo.Instance;
                if (elementUnmanagedType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
                {
                    elementMarshallingInfo = elementType.SpecialType == SpecialType.System_String
                        ? CreateStringMarshallingInfo(elementType, elementUnmanagedType)
                        : new MarshalAsInfo(elementUnmanagedType, _defaultInfo.CharEncoding);
                }
                else
                {
                    maxIndirectionDepthUsed = 1;
                    elementMarshallingInfo = GetMarshallingInfo(elementType, new Dictionary<int, AttributeData>(), 1, ImmutableHashSet<string>.Empty, ref maxIndirectionDepthUsed);
                }

                return CreateArrayMarshallingInfo(type, elementType, arraySizeInfo, elementMarshallingInfo);
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return CreateStringMarshallingInfo(type, unmanagedType);
            }

            return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);
        }

        private MarshallingInfo CreateNativeMarshallingInfo(
            ITypeSymbol type,
            INamedTypeSymbol entryPointType,
            AttributeData attrData,
            bool isMarshalUsingAttribute,
            int indirectionLevel,
            CountInfo parsedCountInfo,
            Dictionary<int, AttributeData> useSiteAttributes,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionDepthUsed)
        {
            if (!ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(entryPointType))
            {
                return NoMarshallingInfo.Instance;
            }

            if (!(entryPointType.IsStatic && entryPointType.TypeKind == TypeKind.Class)
                && entryPointType.TypeKind != TypeKind.Struct)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerTypeMustBeStaticClassOrStruct), entryPointType.ToDisplayString(), type.ToDisplayString());
                return NoMarshallingInfo.Instance;
            }

            ManagedTypeInfo entryPointTypeInfo = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType);

            bool isLinearCollectionMarshalling = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointType);
            if (isLinearCollectionMarshalling)
            {
                // Update the entry point type with the type arguments based on the managed type
                if (type is IArrayTypeSymbol arrayManagedType)
                {
                    if (entryPointType.Arity != 2)
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = entryPointType.ConstructedFrom.Construct(
                        arrayManagedType.ElementType,
                        entryPointType.TypeArguments.Last());
                }
                else if (type is INamedTypeSymbol namedManagedCollectionType && entryPointType.IsUnboundGenericType)
                {
                    if (!ManualTypeMarshallingHelper.TryResolveEntryPointType(
                        namedManagedCollectionType,
                        entryPointType,
                        isLinearCollectionMarshalling,
                        (type, entryPointType) => _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString()),
                        out ITypeSymbol resolvedEntryPointType))
                    {
                        return NoMarshallingInfo.Instance;
                    }

                    entryPointType = (INamedTypeSymbol)resolvedEntryPointType;
                }
                else
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }

                int maxIndirectionDepthUsedLocal = maxIndirectionDepthUsed;
                Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement = (ITypeSymbol elementType) => GetMarshallingInfo(elementType, new Dictionary<int, AttributeData>(), 1, ImmutableHashSet<string>.Empty, ref maxIndirectionDepthUsedLocal);
                if (ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(entryPointType, type, _compilation, getMarshallingInfoForElement, out CustomTypeMarshallers? collectionMarshallers))
                {
                    maxIndirectionDepthUsed = maxIndirectionDepthUsedLocal;
                    return new NativeLinearCollectionMarshallingInfo(
                        entryPointTypeInfo,
                        collectionMarshallers.Value,
                        parsedCountInfo,
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType.TypeParameters.Last()));
                }
                return NoMarshallingInfo.Instance;
            }

            if (type is INamedTypeSymbol namedManagedType && entryPointType.IsUnboundGenericType)
            {
                if (!ManualTypeMarshallingHelper.TryResolveEntryPointType(
                    namedManagedType,
                    entryPointType,
                    isLinearCollectionMarshalling,
                    (type, entryPointType) => _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(SR.MarshallerEntryPointTypeMustMatchArity), entryPointType.ToDisplayString(), type.ToDisplayString()),
                    out ITypeSymbol resolvedEntryPointType))
                {
                    return NoMarshallingInfo.Instance;
                }

                entryPointType = (INamedTypeSymbol)resolvedEntryPointType;
            }

            if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(entryPointType, type, _compilation, out CustomTypeMarshallers? marshallers))
            {
                return new NativeMarshallingAttributeInfo(entryPointTypeInfo, marshallers.Value);
            }
            return NoMarshallingInfo.Instance;
        }

        private bool TryCreateTypeBasedMarshallingInfo(
            ITypeSymbol type,
            CountInfo parsedCountInfo,
            int indirectionLevel,
            Dictionary<int, AttributeData> useSiteAttributes,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionDepthUsed,
            out MarshallingInfo marshallingInfo)
        {
            // Check for an implicit SafeHandle conversion.
            // The SafeHandle type might not be defined if we're using one of the test CoreLib implementations used for NativeAOT.
            ITypeSymbol? safeHandleType = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_SafeHandle);
            if (safeHandleType is not null)
            {
                CodeAnalysis.Operations.CommonConversion conversion = _compilation.ClassifyCommonConversion(type, safeHandleType);
                if (conversion.Exists
                    && conversion.IsImplicit
                    && (conversion.IsReference || conversion.IsIdentity))
                {
                    bool hasAccessibleDefaultConstructor = false;
                    if (type is INamedTypeSymbol named && !named.IsAbstract && named.InstanceConstructors.Length > 0)
                    {
                        foreach (IMethodSymbol ctor in named.InstanceConstructors)
                        {
                            if (ctor.Parameters.Length == 0)
                            {
                                hasAccessibleDefaultConstructor = _compilation.IsSymbolAccessibleWithin(ctor, _contextSymbol.ContainingType);
                                break;
                            }
                        }
                    }
                    marshallingInfo = new SafeHandleMarshallingInfo(hasAccessibleDefaultConstructor, type.IsAbstract);
                    return true;
                }
            }

            if (type is IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
            {
                MarshallingInfo elementMarshallingInfo = GetMarshallingInfo(elementType, useSiteAttributes, indirectionLevel + 1, inspectedElements, ref maxIndirectionDepthUsed);
                marshallingInfo = CreateArrayMarshallingInfo(type, elementType, parsedCountInfo, elementMarshallingInfo);
                return true;
            }

            // No marshalling info was computed, but a character encoding was provided.
            // If the type is a character or string then pass on these details.
            if (type.SpecialType == SpecialType.System_Char && _defaultInfo.CharEncoding != CharEncoding.Undefined)
            {
                marshallingInfo = new MarshallingInfoStringSupport(_defaultInfo.CharEncoding);
                return true;
            }

            if (type.SpecialType == SpecialType.System_String && _defaultInfo.CharEncoding != CharEncoding.Undefined)
            {
                if (_defaultInfo.CharEncoding == CharEncoding.Custom)
                {
                    if (_defaultInfo.StringMarshallingCustomType is not null)
                    {
                        AttributeData attrData = _contextSymbol is IMethodSymbol
                            ? _contextSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == TypeNames.LibraryImportAttribute)
                            : default;
                        marshallingInfo = CreateNativeMarshallingInfo(type, _defaultInfo.StringMarshallingCustomType, attrData, true, indirectionLevel, parsedCountInfo, useSiteAttributes, inspectedElements, ref maxIndirectionDepthUsed);
                        return true;
                    }
                }
                else
                {
                    marshallingInfo = _defaultInfo.CharEncoding switch
                    {
                        CharEncoding.Utf16 => CreateStringMarshallingInfo(type, TypeNames.Utf16StringMarshaller),
                        CharEncoding.Utf8 => CreateStringMarshallingInfo(type, TypeNames.Utf8StringMarshaller),
                        _ => throw new InvalidOperationException()
                    };

                    return true;
                }

                marshallingInfo = new MarshallingInfoStringSupport(_defaultInfo.CharEncoding);
                return true;
            }


            if (type.SpecialType == SpecialType.System_Boolean)
            {
                // We explicitly don't support marshalling bool without any marshalling info
                // as treating bool as a non-normalized 1-byte value is generally not a good default.
                // Additionally, that default is different than the runtime marshalling, so by explicitly
                // blocking bool marshalling without additional info, we make it a little easier
                // to transition by explicitly notifying people of changing behavior.
                marshallingInfo = NoMarshallingInfo.Instance;
                return false;
            }

            if (type is INamedTypeSymbol { IsUnmanagedType: true } unmanagedType
                && unmanagedType.IsConsideredBlittable())
            {
                marshallingInfo = GetBlittableMarshallingInfo(type);
                return true;
            }

            marshallingInfo = NoMarshallingInfo.Instance;
            return false;
        }

        private MarshallingInfo CreateArrayMarshallingInfo(
            ITypeSymbol managedType,
            ITypeSymbol elementType,
            CountInfo countInfo,
            MarshallingInfo elementMarshallingInfo)
        {
            ITypeSymbol typeArgumentToInsert = elementType;
            INamedTypeSymbol? arrayMarshaller;
            if (elementType is IPointerTypeSymbol { PointedAtType: ITypeSymbol pointedAt })
            {
                arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_PointerArrayMarshaller_Metadata);
                typeArgumentToInsert = pointedAt;
            }
            else
            {
                arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ArrayMarshaller_Metadata);
            }

            if (arrayMarshaller is null)
            {
                // If the array marshaler type is not available, then we cannot marshal arrays but indicate it is missing.
                return new MissingSupportCollectionMarshallingInfo(countInfo, elementMarshallingInfo);
            }

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(arrayMarshaller)
                && ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(arrayMarshaller))
            {
                arrayMarshaller = arrayMarshaller.Construct(
                    typeArgumentToInsert,
                    arrayMarshaller.TypeArguments.Last());

                Func<ITypeSymbol, MarshallingInfo> getMarshallingInfoForElement = (ITypeSymbol elementType) => elementMarshallingInfo;
                if (ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(arrayMarshaller, managedType, _compilation, getMarshallingInfoForElement, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeLinearCollectionMarshallingInfo(
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(arrayMarshaller),
                        marshallers.Value,
                        countInfo,
                        ManagedTypeInfo.CreateTypeInfoForTypeSymbol(arrayMarshaller.TypeParameters.Last()));
                }
            }

            Debug.WriteLine("Default marshallers for arrays should be a valid shape.");
            return NoMarshallingInfo.Instance;
        }

        private MarshallingInfo CreateStringMarshallingInfo(
            ITypeSymbol type,
            UnmanagedType unmanagedType)
        {
            string? marshallerName = unmanagedType switch
            {
                UnmanagedType.BStr => TypeNames.BStrStringMarshaller,
                UnmanagedType.LPStr => TypeNames.AnsiStringMarshaller,
                UnmanagedType.LPTStr or UnmanagedType.LPWStr => TypeNames.Utf16StringMarshaller,
                MarshalAsInfo.UnmanagedType_LPUTF8Str => TypeNames.Utf8StringMarshaller,
                _ => null
            };

            if (marshallerName is null)
                return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);

            return CreateStringMarshallingInfo(type, marshallerName);
        }

        private MarshallingInfo CreateStringMarshallingInfo(
            ITypeSymbol type,
            string marshallerName)
        {
            INamedTypeSymbol? stringMarshaller = _compilation.GetTypeByMetadataName(marshallerName);
            if (stringMarshaller is null)
                return new MissingSupportMarshallingInfo();

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(stringMarshaller))
            {
                if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(stringMarshaller, type, _compilation, out CustomTypeMarshallers? marshallers))
                {
                    return new NativeMarshallingAttributeInfo(
                        EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(stringMarshaller),
                        Marshallers: marshallers.Value);
                }
            }

            return new MissingSupportMarshallingInfo();
        }

        private MarshallingInfo GetBlittableMarshallingInfo(ITypeSymbol type)
        {
            if (type.TypeKind is TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer
                || type.SpecialType.IsAlwaysBlittable())
            {
                // Treat primitive types and enums as having no marshalling info.
                // They are supported in configurations where runtime marshalling is enabled.
                return NoMarshallingInfo.Instance;
            }
            else if (_compilation.GetTypeByMetadataName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute) is null)
            {
                // If runtime marshalling cannot be disabled, then treat this as a "missing support" scenario so we can gracefully fall back to using the forwarder downlevel.
                return new MissingSupportMarshallingInfo();
            }
            else
            {
                return new UnmanagedBlittableMarshallingInfo(type.IsStrictlyBlittable());
            }
        }

        private bool TryGetAttributeIndirectionLevel(AttributeData attrData, out int indirectionLevel)
        {
            if (SymbolEqualityComparer.Default.Equals(attrData.AttributeClass, _marshalAsAttribute))
            {
                indirectionLevel = 0;
                return true;
            }

            if (!SymbolEqualityComparer.Default.Equals(attrData.AttributeClass, _marshalUsingAttribute))
            {
                indirectionLevel = 0;
                return false;
            }

            foreach (KeyValuePair<string, TypedConstant> arg in attrData.NamedArguments)
            {
                if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.ElementIndirectionDepth)
                {
                    indirectionLevel = (int)arg.Value.Value!;
                    return true;
                }
            }
            indirectionLevel = 0;
            return true;
        }

        private sealed class CyclicalCountElementInfoException : Exception
        {
            public CyclicalCountElementInfoException(ImmutableHashSet<string> elementsInCycle, string startOfCycle)
            {
                ElementsInCycle = elementsInCycle;
                StartOfCycle = startOfCycle;
            }

            public ImmutableHashSet<string> ElementsInCycle { get; }

            public string StartOfCycle { get; }
        }
    }
}
