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
    internal sealed record DefaultMarshallingInfo(
        CharEncoding CharEncoding
    );

    // The following types are modeled to fit with the current prospective spec
    // for C# vNext discriminated unions. Once discriminated unions are released,
    // these should be updated to be implemented as a discriminated union.

    internal abstract record MarshallingInfo
    {
    }

    internal sealed record NoMarshallingInfo : MarshallingInfo
    {
        public static readonly MarshallingInfo Instance = new NoMarshallingInfo();

        private NoMarshallingInfo() { }
    }

    /// <summary>
    /// Character encoding enumeration.
    /// </summary>
    internal enum CharEncoding
    {
        Undefined,
        Utf8,
        Utf16,
        Ansi,
        PlatformDefined
    }

    /// <summary>
    /// Details that are required when scenario supports strings.
    /// </summary>
    internal record MarshallingInfoStringSupport(
        CharEncoding CharEncoding
    ) : MarshallingInfo;

    /// <summary>
    /// Simple User-application of System.Runtime.InteropServices.MarshalAsAttribute
    /// </summary>
    internal sealed record MarshalAsInfo(
        UnmanagedType UnmanagedType,
        CharEncoding CharEncoding) : MarshallingInfoStringSupport(CharEncoding)
    {
    }

    /// <summary>
    /// User-applied System.Runtime.InteropServices.BlittableTypeAttribute
    /// or System.Runtime.InteropServices.GeneratedMarshallingAttribute on a blittable type
    /// in source in this compilation.
    /// </summary>
    internal sealed record BlittableTypeAttributeInfo : MarshallingInfo;

    [Flags]
    internal enum SupportedMarshallingMethods
    {
        None = 0,
        ManagedToNative = 0x1,
        NativeToManaged = 0x2,
        ManagedToNativeStackalloc = 0x4,
        Pinning = 0x8,
        All = -1
    }

    internal abstract record CountInfo;

    internal sealed record NoCountInfo : CountInfo
    {
        public static readonly NoCountInfo Instance = new NoCountInfo();

        private NoCountInfo() { }
    }

    internal sealed record ConstSizeCountInfo(int Size) : CountInfo;

    internal sealed record CountElementCountInfo(TypePositionInfo ElementInfo) : CountInfo
    {
        public const string ReturnValueElementName = "return-value";
    }

    internal sealed record SizeAndParamIndexInfo(int ConstSize, TypePositionInfo? ParamAtIndex) : CountInfo
    {
        public const int UnspecifiedConstSize = -1;

        public const TypePositionInfo UnspecifiedParam = null;

        public static readonly SizeAndParamIndexInfo Unspecified = new(UnspecifiedConstSize, UnspecifiedParam);
    }

    /// <summary>
    /// User-applied System.Runtime.InteropServices.NativeMarshallingAttribute
    /// </summary>
    internal record NativeMarshallingAttributeInfo(
        ITypeSymbol NativeMarshallingType,
        ITypeSymbol? ValuePropertyType,
        SupportedMarshallingMethods MarshallingMethods,
        bool NativeTypePinnable,
        bool UseDefaultMarshalling) : MarshallingInfo;

    /// <summary>
    /// User-applied System.Runtime.InteropServices.GeneratedMarshallingAttribute
    /// on a non-blittable type in source in this compilation.
    /// </summary>
    internal sealed record GeneratedNativeMarshallingAttributeInfo(
        string NativeMarshallingFullyQualifiedTypeName) : MarshallingInfo;

    /// <summary>
    /// The type of the element is a SafeHandle-derived type with no marshalling attributes.
    /// </summary>
    internal sealed record SafeHandleMarshallingInfo(bool AccessibleDefaultConstructor) : MarshallingInfo;

    /// <summary>
    /// User-applied System.Runtime.InteropServices.NativeMarshallingAttribute
    /// with a contiguous collection marshaller
    internal sealed record NativeContiguousCollectionMarshallingInfo(
        ITypeSymbol NativeMarshallingType,
        ITypeSymbol? ValuePropertyType,
        SupportedMarshallingMethods MarshallingMethods,
        bool NativeTypePinnable,
        bool UseDefaultMarshalling,
        CountInfo ElementCountInfo,
        ITypeSymbol ElementType,
        MarshallingInfo ElementMarshallingInfo) : NativeMarshallingAttributeInfo(
            NativeMarshallingType,
            ValuePropertyType,
            MarshallingMethods,
            NativeTypePinnable,
            UseDefaultMarshalling
        );

    internal class MarshallingAttributeInfoParser
    {
        private readonly Compilation _compilation;
        private readonly GeneratorDiagnostics _diagnostics;
        private readonly DefaultMarshallingInfo _defaultInfo;
        private readonly ISymbol _contextSymbol;
        private readonly ITypeSymbol _marshalAsAttribute;
        private readonly ITypeSymbol _marshalUsingAttribute;

        public MarshallingAttributeInfoParser(
            Compilation compilation,
            GeneratorDiagnostics diagnostics,
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
            Dictionary<int, AttributeData> marshallingAttributesByIndirectionLevel = new();
            int maxIndirectionLevelDataProvided = 0;
            foreach (AttributeData attribute in useSiteAttributes)
            {
                if (TryGetAttributeIndirectionLevel(attribute, out int indirectionLevel))
                {
                    if (marshallingAttributesByIndirectionLevel.ContainsKey(indirectionLevel))
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(attribute, nameof(Resources.DuplicateMarshallingInfo), indirectionLevel.ToString());
                        return NoMarshallingInfo.Instance;
                    }
                    marshallingAttributesByIndirectionLevel.Add(indirectionLevel, attribute);
                    maxIndirectionLevelDataProvided = Math.Max(maxIndirectionLevelDataProvided, indirectionLevel);
                }
            }

            int maxIndirectionLevelUsed = 0;
            MarshallingInfo info = GetMarshallingInfo(
                managedType,
                marshallingAttributesByIndirectionLevel,
                indirectionLevel: 0,
                inspectedElements,
                ref maxIndirectionLevelUsed);
            if (maxIndirectionLevelUsed < maxIndirectionLevelDataProvided)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(
                    marshallingAttributesByIndirectionLevel[maxIndirectionLevelDataProvided],
                    nameof(Resources.ExtraneousMarshallingInfo),
                    maxIndirectionLevelDataProvided.ToString(),
                    maxIndirectionLevelUsed.ToString());
            }
            return info;
        }

        private MarshallingInfo GetMarshallingInfo(
            ITypeSymbol type,
            Dictionary<int, AttributeData> useSiteAttributes,
            int indirectionLevel,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionLevelUsed)
        {
            maxIndirectionLevelUsed = Math.Max(indirectionLevel, maxIndirectionLevelUsed);
            CountInfo parsedCountInfo = NoCountInfo.Instance;

            if (useSiteAttributes.TryGetValue(indirectionLevel, out AttributeData useSiteAttribute))
            {
                INamedTypeSymbol attributeClass = useSiteAttribute.AttributeClass!;

                if (indirectionLevel == 0
                    && SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute), attributeClass))
                {
                    // https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute
                    return CreateInfoFromMarshalAs(type, useSiteAttribute, inspectedElements, ref maxIndirectionLevelUsed);
                }
                else if (SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute), attributeClass))
                {
                    if (parsedCountInfo != NoCountInfo.Instance)
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(useSiteAttribute, nameof(Resources.DuplicateCountInfo));
                        return NoMarshallingInfo.Instance;
                    }
                    parsedCountInfo = CreateCountInfo(useSiteAttribute, inspectedElements);
                    if (useSiteAttribute.ConstructorArguments.Length != 0)
                    {
                        return CreateNativeMarshallingInfo(
                            type,
                            useSiteAttribute,
                            isMarshalUsingAttribute: true,
                            indirectionLevel,
                            parsedCountInfo,
                            useSiteAttributes,
                            inspectedElements,
                            ref maxIndirectionLevelUsed);
                    }
                }
            }

            // If we aren't overriding the marshalling at usage time,
            // then fall back to the information on the element type itself.
            foreach (var typeAttribute in type.GetAttributes())
            {
                INamedTypeSymbol attributeClass = typeAttribute.AttributeClass!;

                if (SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.BlittableTypeAttribute), attributeClass))
                {
                    // If type is generic, then we need to re-evaluate that it is blittable at usage time.
                    if (type is INamedTypeSymbol { IsGenericType: false } || type.HasOnlyBlittableFields())
                    {
                        return new BlittableTypeAttributeInfo();
                    }
                    break;
                }
                else if (SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.NativeMarshallingAttribute), attributeClass))
                {
                    return CreateNativeMarshallingInfo(
                        type,
                        typeAttribute,
                        isMarshalUsingAttribute: false,
                        indirectionLevel,
                        parsedCountInfo,
                        useSiteAttributes,
                        inspectedElements,
                        ref maxIndirectionLevelUsed);
                }
                else if (SymbolEqualityComparer.Default.Equals(_compilation.GetTypeByMetadataName(TypeNames.GeneratedMarshallingAttribute), attributeClass))
                {
                    return type.IsConsideredBlittable() ? new BlittableTypeAttributeInfo() : new GeneratedNativeMarshallingAttributeInfo(null! /* TODO: determine naming convention */);
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
                ref maxIndirectionLevelUsed,
                out MarshallingInfo infoMaybe))
            {
                return infoMaybe;
            }

            // No marshalling info was computed, but a character encoding was provided.
            // If the type is a character or string then pass on these details.
            if (_defaultInfo.CharEncoding != CharEncoding.Undefined
                && (type.SpecialType == SpecialType.System_Char
                    || type.SpecialType == SpecialType.System_String))
            {
                return new MarshallingInfoStringSupport(_defaultInfo.CharEncoding);
            }

            return NoMarshallingInfo.Instance;
        }

        CountInfo CreateCountInfo(AttributeData marshalUsingData, ImmutableHashSet<string> inspectedElements)
        {
            int? constSize = null;
            string? elementName = null;
            foreach (var arg in marshalUsingData.NamedArguments)
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
                _diagnostics.ReportInvalidMarshallingAttributeInfo(marshalUsingData, nameof(Resources.ConstantAndElementCountInfoDisallowed));
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
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(marshalUsingData, nameof(Resources.CyclicalCountInfo), elementName);
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
                _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.CyclicalCountInfo), param.Name);
                return SizeAndParamIndexInfo.UnspecifiedParam;
            }
        }

        private TypePositionInfo? CreateForElementName(string elementName, ImmutableHashSet<string> inspectedElements)
        {
            if (_contextSymbol is IMethodSymbol method)
            {
                if (elementName == CountElementCountInfo.ReturnValueElementName)
                {
                    return TypePositionInfo.CreateForType(
                        method.ReturnType,
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
            else if (_contextSymbol is INamedTypeSymbol _)
            {
                // TODO: Handle when we create a struct marshalling generator
                // Do we want to support CountElementName pointing to only fields, or properties as well?
                // If only fields, how do we handle properties with generated backing fields?
            }

            return null;
        }

        MarshallingInfo CreateInfoFromMarshalAs(
            ITypeSymbol type,
            AttributeData attrData,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionLevelUsed)
        {
            object unmanagedTypeObj = attrData.ConstructorArguments[0].Value!;
            UnmanagedType unmanagedType = unmanagedTypeObj is short
                ? (UnmanagedType)(short)unmanagedTypeObj
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

            if (!isArrayType)
            {
                return new MarshalAsInfo(unmanagedType, _defaultInfo.CharEncoding);
            }

            if (type is not IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
            {
                _diagnostics.ReportConfigurationNotSupported(attrData, nameof(UnmanagedType), unmanagedType.ToString());
                return NoMarshallingInfo.Instance;
            }

            MarshallingInfo elementMarshallingInfo = NoMarshallingInfo.Instance;
            if (elementUnmanagedType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
            {
                elementMarshallingInfo = new MarshalAsInfo(elementUnmanagedType, _defaultInfo.CharEncoding);
            }
            else
            {
                maxIndirectionLevelUsed = 1;
                elementMarshallingInfo = GetMarshallingInfo(elementType, new Dictionary<int, AttributeData>(), 1, ImmutableHashSet<string>.Empty, ref maxIndirectionLevelUsed);
            }

            INamedTypeSymbol? arrayMarshaller;

            if (elementType is IPointerTypeSymbol { PointedAtType: ITypeSymbol pointedAt })
            {
                arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_GeneratedMarshalling_PtrArrayMarshaller_Metadata)?.Construct(pointedAt);
            }
            else
            {
                arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_GeneratedMarshalling_ArrayMarshaller_Metadata)?.Construct(elementType);
            }

            if (arrayMarshaller is null)
            {
                // If the array marshaler type is not available, then we cannot marshal arrays.
                return NoMarshallingInfo.Instance;
            }

            return new NativeContiguousCollectionMarshallingInfo(
                NativeMarshallingType: arrayMarshaller,
                ValuePropertyType: ManualTypeMarshallingHelper.FindValueProperty(arrayMarshaller)?.Type,
                MarshallingMethods: ~SupportedMarshallingMethods.Pinning,
                NativeTypePinnable: true,
                UseDefaultMarshalling: true,
                ElementCountInfo: arraySizeInfo,
                ElementType: elementType,
                ElementMarshallingInfo: elementMarshallingInfo);
        }

        MarshallingInfo CreateNativeMarshallingInfo(
            ITypeSymbol type,
            AttributeData attrData,
            bool isMarshalUsingAttribute,
            int indirectionLevel,
            CountInfo parsedCountInfo,
            Dictionary<int, AttributeData> useSiteAttributes,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionLevelUsed)
        {
            SupportedMarshallingMethods methods = SupportedMarshallingMethods.None;

            if (!isMarshalUsingAttribute && ManualTypeMarshallingHelper.FindGetPinnableReference(type) is not null)
            {
                methods |= SupportedMarshallingMethods.Pinning;
            }

            ITypeSymbol spanOfByte = _compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!.Construct(_compilation.GetSpecialType(SpecialType.System_Byte));

            INamedTypeSymbol nativeType = (INamedTypeSymbol)attrData.ConstructorArguments[0].Value!;

            if (nativeType.IsUnboundGenericType)
            {
                if (isMarshalUsingAttribute)
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityMessage), nativeType.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    if (namedType.Arity != nativeType.Arity)
                    {
                        _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityMessage), nativeType.ToDisplayString());
                        return NoMarshallingInfo.Instance;
                    }
                    else
                    {
                        nativeType = nativeType.ConstructedFrom.Construct(namedType.TypeArguments.ToArray());
                    }
                }
                else
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityMessage), nativeType.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }
            }

            ITypeSymbol contiguousCollectionMarshalerAttribute = _compilation.GetTypeByMetadataName(TypeNames.GenericContiguousCollectionMarshallerAttribute)!;

            bool isContiguousCollectionMarshaller = nativeType.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, contiguousCollectionMarshalerAttribute));
            IPropertySymbol? valueProperty = ManualTypeMarshallingHelper.FindValueProperty(nativeType);

            var marshallingVariant = isContiguousCollectionMarshaller
                ? ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.ContiguousCollection
                : ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.Standard;

            bool hasInt32Constructor = false;
            foreach (var ctor in nativeType.Constructors)
            {
                if (ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type, marshallingVariant) && (valueProperty is null or { GetMethod: not null }))
                {
                    methods |= SupportedMarshallingMethods.ManagedToNative;
                }
                else if (ManualTypeMarshallingHelper.IsStackallocConstructor(ctor, type, spanOfByte, marshallingVariant)
                    && (valueProperty is null or { GetMethod: not null }))
                {
                    methods |= SupportedMarshallingMethods.ManagedToNativeStackalloc;
                }
                else if (ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
                {
                    hasInt32Constructor = true;
                }
            }

            // The constructor that takes only the native element size is required for collection marshallers
            // in the native-to-managed scenario.
            if ((!isContiguousCollectionMarshaller
                    || (hasInt32Constructor && ManualTypeMarshallingHelper.HasSetUnmarshalledCollectionLengthMethod(nativeType)))
                && ManualTypeMarshallingHelper.HasToManagedMethod(nativeType, type)
                && (valueProperty is null or { SetMethod: not null }))
            {
                methods |= SupportedMarshallingMethods.NativeToManaged;
            }

            if (methods == SupportedMarshallingMethods.None)
            {
                _diagnostics.ReportInvalidMarshallingAttributeInfo(
                    attrData,
                    isContiguousCollectionMarshaller
                        ? nameof(Resources.CollectionNativeTypeMustHaveRequiredShapeMessage)
                        : nameof(Resources.NativeTypeMustHaveRequiredShapeMessage),
                    nativeType.ToDisplayString());
                return NoMarshallingInfo.Instance;
            }

            if (isContiguousCollectionMarshaller)
            {
                if (!ManualTypeMarshallingHelper.HasNativeValueStorageProperty(nativeType, spanOfByte))
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.CollectionNativeTypeMustHaveRequiredShapeMessage), nativeType.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }

                if (!ManualTypeMarshallingHelper.TryGetElementTypeFromContiguousCollectionMarshaller(nativeType, out ITypeSymbol elementType))
                {
                    _diagnostics.ReportInvalidMarshallingAttributeInfo(attrData, nameof(Resources.CollectionNativeTypeMustHaveRequiredShapeMessage), nativeType.ToDisplayString());
                    return NoMarshallingInfo.Instance;
                }

                return new NativeContiguousCollectionMarshallingInfo(
                    nativeType,
                    valueProperty?.Type,
                    methods,
                    NativeTypePinnable: ManualTypeMarshallingHelper.FindGetPinnableReference(nativeType) is not null,
                    UseDefaultMarshalling: !isMarshalUsingAttribute,
                    parsedCountInfo,
                    elementType,
                    GetMarshallingInfo(elementType, useSiteAttributes, indirectionLevel + 1, inspectedElements, ref maxIndirectionLevelUsed));
            }

            return new NativeMarshallingAttributeInfo(
                nativeType,
                valueProperty?.Type,
                methods,
                NativeTypePinnable: ManualTypeMarshallingHelper.FindGetPinnableReference(nativeType) is not null,
                UseDefaultMarshalling: !isMarshalUsingAttribute);
        }

        bool TryCreateTypeBasedMarshallingInfo(
            ITypeSymbol type,
            CountInfo parsedCountInfo,
            int indirectionLevel,
            Dictionary<int, AttributeData> useSiteAttributes,
            ImmutableHashSet<string> inspectedElements,
            ref int maxIndirectionLevelUsed,
            out MarshallingInfo marshallingInfo)
        {
            // Check for an implicit SafeHandle conversion.
            var conversion = _compilation.ClassifyCommonConversion(type, _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_SafeHandle)!);
            if (conversion.Exists
                && conversion.IsImplicit
                && (conversion.IsReference || conversion.IsIdentity))
            {
                bool hasAccessibleDefaultConstructor = false;
                if (type is INamedTypeSymbol named && !named.IsAbstract && named.InstanceConstructors.Length > 0)
                {
                    foreach (var ctor in named.InstanceConstructors)
                    {
                        if (ctor.Parameters.Length == 0)
                        {
                            hasAccessibleDefaultConstructor = _compilation.IsSymbolAccessibleWithin(ctor, _contextSymbol.ContainingType);
                            break;
                        }
                    }
                }
                marshallingInfo = new SafeHandleMarshallingInfo(hasAccessibleDefaultConstructor);
                return true;
            }

            if (type is IArrayTypeSymbol { ElementType: ITypeSymbol elementType })
            {
                INamedTypeSymbol? arrayMarshaller;

                if (elementType is IPointerTypeSymbol { PointedAtType: ITypeSymbol pointedAt })
                {
                    arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_GeneratedMarshalling_PtrArrayMarshaller_Metadata)?.Construct(pointedAt);
                }
                else
                {
                    arrayMarshaller = _compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_GeneratedMarshalling_ArrayMarshaller_Metadata)?.Construct(elementType);
                }

                if (arrayMarshaller is null)
                {
                    // If the array marshaler type is not available, then we cannot marshal arrays.
                    marshallingInfo = NoMarshallingInfo.Instance;
                    return false;
                }

                marshallingInfo = new NativeContiguousCollectionMarshallingInfo(
                    NativeMarshallingType: arrayMarshaller,
                    ValuePropertyType: ManualTypeMarshallingHelper.FindValueProperty(arrayMarshaller)?.Type,
                    MarshallingMethods: ~SupportedMarshallingMethods.Pinning,
                    NativeTypePinnable: true,
                    UseDefaultMarshalling: true,
                    ElementCountInfo: parsedCountInfo,
                    ElementType: elementType,
                    ElementMarshallingInfo: GetMarshallingInfo(elementType, useSiteAttributes, indirectionLevel + 1, inspectedElements, ref maxIndirectionLevelUsed));
                return true;
            }

            if (type is INamedTypeSymbol { IsValueType: true } valueType
                && !valueType.IsExposedOutsideOfCurrentCompilation()
                && valueType.IsConsideredBlittable())
            {
                // Allow implicit [BlittableType] on internal value types.
                marshallingInfo = new BlittableTypeAttributeInfo();
                return true;
            }

            marshallingInfo = NoMarshallingInfo.Instance;
            return false;
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

            foreach (var arg in attrData.NamedArguments)
            {
                if (arg.Key == ManualTypeMarshallingHelper.MarshalUsingProperties.ElementIndirectionLevel)
                {
                    indirectionLevel = (int)arg.Value.Value!;
                    return true;
                }
            }
            indirectionLevel = 0;
            return true;
        }

        private class CyclicalCountElementInfoException : Exception
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
