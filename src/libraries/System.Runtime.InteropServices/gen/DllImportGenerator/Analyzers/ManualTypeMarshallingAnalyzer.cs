// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ManualTypeMarshallingAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static readonly DiagnosticDescriptor BlittableTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.BlittableTypeMustBeBlittable,
                "BlittableTypeMustBeBlittable",
                GetResourceString(nameof(Resources.BlittableTypeMustBeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.BlittableTypeMustBeBlittableDescription)));

        public static readonly DiagnosticDescriptor CannotHaveMultipleMarshallingAttributesRule =
            new DiagnosticDescriptor(
                Ids.CannotHaveMultipleMarshallingAttributes,
                "CannotHaveMultipleMarshallingAttributes",
                GetResourceString(nameof(Resources.CannotHaveMultipleMarshallingAttributesMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CannotHaveMultipleMarshallingAttributesDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBeNonNullRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBeNonNull,
                "NativeTypeMustBeNonNull",
                GetResourceString(nameof(Resources.NativeTypeMustBeNonNullMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBeNonNullDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBeBlittable,
                "NativeTypeMustBeBlittable",
                GetResourceString(nameof(Resources.NativeTypeMustBeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBeBlittableDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceReturnTypeBlittableRule =
            new DiagnosticDescriptor(
                Ids.GetPinnableReferenceReturnTypeBlittable,
                "GetPinnableReferenceReturnTypeBlittable",
                GetResourceString(nameof(Resources.GetPinnableReferenceReturnTypeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GetPinnableReferenceReturnTypeBlittableDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBePointerSizedRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBePointerSized,
                "NativeTypeMustBePointerSized",
                GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustHaveRequiredShapeRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustHaveRequiredShape,
                "NativeTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.NativeTypeMustHaveRequiredShapeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustHaveRequiredShapeDescription)));

        public static readonly DiagnosticDescriptor CollectionNativeTypeMustHaveRequiredShapeRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustHaveRequiredShape,
                "NativeTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.CollectionNativeTypeMustHaveRequiredShapeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CollectionNativeTypeMustHaveRequiredShapeDescription)));

        public static readonly DiagnosticDescriptor ValuePropertyMustHaveSetterRule =
            new DiagnosticDescriptor(
                Ids.ValuePropertyMustHaveSetter,
                "ValuePropertyMustHaveSetter",
                GetResourceString(nameof(Resources.ValuePropertyMustHaveSetterMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValuePropertyMustHaveSetterDescription)));

        public static readonly DiagnosticDescriptor ValuePropertyMustHaveGetterRule =
            new DiagnosticDescriptor(
                Ids.ValuePropertyMustHaveGetter,
                "ValuePropertyMustHaveGetter",
                GetResourceString(nameof(Resources.ValuePropertyMustHaveGetterMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValuePropertyMustHaveGetterDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.GetPinnableReferenceShouldSupportAllocatingMarshallingFallback,
                "GetPinnableReferenceShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackDescription)));

        public static readonly DiagnosticDescriptor CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocMarshallingShouldSupportAllocatingMarshallingFallback,
                "CallerAllocMarshallingShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackDescription)));

        public static readonly DiagnosticDescriptor CallerAllocConstructorMustHaveBufferSizeConstantRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocConstructorMustHaveStackBufferSizeConstant,
                "CallerAllocConstructorMustHaveBufferSizeConstant",
                GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeConstantMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeConstantDescription)));

        public static readonly DiagnosticDescriptor RefValuePropertyUnsupportedRule =
            new DiagnosticDescriptor(
                Ids.RefValuePropertyUnsupported,
                "RefValuePropertyUnsupported",
                GetResourceString(nameof(Resources.RefValuePropertyUnsupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.RefValuePropertyUnsupportedDescription)));

        public static readonly DiagnosticDescriptor NativeGenericTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.NativeGenericTypeMustBeClosedOrMatchArity,
                "NativeGenericTypeMustBeClosedOrMatchArity",
                GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor MarshallerGetPinnableReferenceRequiresValuePropertyRule =
            new DiagnosticDescriptor(
                Ids.MarshallerGetPinnableReferenceRequiresValueProperty,
                "MarshallerGetPinnableReferenceRequiresValueProperty",
                GetResourceString(nameof(Resources.MarshallerGetPinnableReferenceRequiresValuePropertyMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerGetPinnableReferenceRequiresValuePropertyDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                BlittableTypeMustBeBlittableRule,
                CannotHaveMultipleMarshallingAttributesRule,
                NativeTypeMustBeNonNullRule,
                NativeTypeMustBeBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                NativeTypeMustBePointerSizedRule,
                NativeTypeMustHaveRequiredShapeRule,
                CollectionNativeTypeMustHaveRequiredShapeRule,
                ValuePropertyMustHaveSetterRule,
                ValuePropertyMustHaveGetterRule,
                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocConstructorMustHaveBufferSizeConstantRule,
                RefValuePropertyUnsupportedRule,
                NativeGenericTypeMustBeClosedOrMatchArityRule,
                MarshallerGetPinnableReferenceRequiresValuePropertyRule);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(PrepareForAnalysis);
        }

        private void PrepareForAnalysis(CompilationStartAnalysisContext context)
        {
            INamedTypeSymbol? generatedMarshallingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.GeneratedMarshallingAttribute);
            INamedTypeSymbol? blittableTypeAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.BlittableTypeAttribute);
            INamedTypeSymbol? nativeMarshallingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.NativeMarshallingAttribute);
            INamedTypeSymbol? marshalUsingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute);
            INamedTypeSymbol? genericContiguousCollectionMarshallerAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.GenericContiguousCollectionMarshallerAttribute);
            INamedTypeSymbol? spanOfByte = context.Compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!.Construct(context.Compilation.GetSpecialType(SpecialType.System_Byte));

            if (generatedMarshallingAttribute is not null
                && blittableTypeAttribute is not null
                && nativeMarshallingAttribute is not null
                && marshalUsingAttribute is not null
                && genericContiguousCollectionMarshallerAttribute is not null
                && spanOfByte is not null)
            {
                var perCompilationAnalyzer = new PerCompilationAnalyzer(
                    generatedMarshallingAttribute,
                    blittableTypeAttribute,
                    nativeMarshallingAttribute,
                    marshalUsingAttribute,
                    genericContiguousCollectionMarshallerAttribute,
                    spanOfByte,
                    context.Compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_StructLayoutAttribute)!);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeTypeDefinition(context), SymbolKind.NamedType);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeElement(context), SymbolKind.Parameter, SymbolKind.Field);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeReturnType(context), SymbolKind.Method);
            }
        }

        private class PerCompilationAnalyzer
        {
            private readonly INamedTypeSymbol _generatedMarshallingAttribute;
            private readonly INamedTypeSymbol _blittableTypeAttribute;
            private readonly INamedTypeSymbol _nativeMarshallingAttribute;
            private readonly INamedTypeSymbol _marshalUsingAttribute;
            private readonly INamedTypeSymbol _genericContiguousCollectionMarshallerAttribute;
            private readonly INamedTypeSymbol _spanOfByte;
            private readonly INamedTypeSymbol _structLayoutAttribute;

            public PerCompilationAnalyzer(INamedTypeSymbol generatedMarshallingAttribute,
                                          INamedTypeSymbol blittableTypeAttribute,
                                          INamedTypeSymbol nativeMarshallingAttribute,
                                          INamedTypeSymbol marshalUsingAttribute,
                                          INamedTypeSymbol genericContiguousCollectionMarshallerAttribute,
                                          INamedTypeSymbol spanOfByte,
                                          INamedTypeSymbol structLayoutAttribute)
            {
                _generatedMarshallingAttribute = generatedMarshallingAttribute;
                _blittableTypeAttribute = blittableTypeAttribute;
                _nativeMarshallingAttribute = nativeMarshallingAttribute;
                _marshalUsingAttribute = marshalUsingAttribute;
                _genericContiguousCollectionMarshallerAttribute = genericContiguousCollectionMarshallerAttribute;
                _spanOfByte = spanOfByte;
                _structLayoutAttribute = structLayoutAttribute;
            }

            public void AnalyzeTypeDefinition(SymbolAnalysisContext context)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

                AttributeData? blittableTypeAttributeData = null;
                AttributeData? nativeMarshallingAttributeData = null;
                foreach (AttributeData attr in type.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _generatedMarshallingAttribute))
                    {
                        // If the type has the GeneratedMarshallingAttribute,
                        // we let the source generator handle error checking.
                        return;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _blittableTypeAttribute))
                    {
                        blittableTypeAttributeData = attr;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _nativeMarshallingAttribute))
                    {
                        nativeMarshallingAttributeData = attr;
                    }
                }

                if (HasMultipleMarshallingAttributes(blittableTypeAttributeData, nativeMarshallingAttributeData))
                {
                    context.ReportDiagnostic(
                        blittableTypeAttributeData!.CreateDiagnostic(
                            CannotHaveMultipleMarshallingAttributesRule,
                            type.ToDisplayString()));
                }
                else if (blittableTypeAttributeData is not null && (!type.HasOnlyBlittableFields() || type.IsAutoLayout(_structLayoutAttribute)))
                {
                    context.ReportDiagnostic(
                        blittableTypeAttributeData.CreateDiagnostic(
                            BlittableTypeMustBeBlittableRule,
                            type.ToDisplayString()));
                }
                else if (nativeMarshallingAttributeData is not null)
                {
                    AnalyzeNativeMarshalerType(context, type, nativeMarshallingAttributeData, isNativeMarshallingAttribute: true);
                }
            }

            private bool HasMultipleMarshallingAttributes(AttributeData? blittableTypeAttributeData, AttributeData? nativeMarshallingAttributeData)
            {
                return (blittableTypeAttributeData, nativeMarshallingAttributeData) switch
                {
                    (null, null) => false,
                    (not null, null) => false,
                    (null, not null) => false,
                    _ => true
                };
            }

            public void AnalyzeElement(SymbolAnalysisContext context)
            {
                AttributeData? attrData = context.Symbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(_marshalUsingAttribute, attr.AttributeClass));
                if (attrData is not null)
                {
                    if (context.Symbol is IParameterSymbol param)
                    {
                        AnalyzeNativeMarshalerType(context, param.Type, attrData, isNativeMarshallingAttribute: false);
                    }
                    else if (context.Symbol is IFieldSymbol field)
                    {
                        AnalyzeNativeMarshalerType(context, field.Type, attrData, isNativeMarshallingAttribute: false);
                    }
                }
            }

            public void AnalyzeReturnType(SymbolAnalysisContext context)
            {
                var method = (IMethodSymbol)context.Symbol;
                AttributeData? attrData = method.GetReturnTypeAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(_marshalUsingAttribute, attr.AttributeClass));
                if (attrData is not null)
                {
                    AnalyzeNativeMarshalerType(context, method.ReturnType, attrData, isNativeMarshallingAttribute: false);
                }
            }

            private void AnalyzeNativeMarshalerType(SymbolAnalysisContext context, ITypeSymbol type, AttributeData nativeMarshalerAttributeData, bool isNativeMarshallingAttribute)
            {
                if (nativeMarshalerAttributeData.ConstructorArguments.Length == 0)
                {
                    // This is a MarshalUsing with just count information.
                    return;
                }

                if (nativeMarshalerAttributeData.ConstructorArguments[0].IsNull)
                {
                    context.ReportDiagnostic(
                        nativeMarshalerAttributeData.CreateDiagnostic(
                            NativeTypeMustBeNonNullRule,
                            type.ToDisplayString()));
                    return;
                }

                ITypeSymbol nativeType = (ITypeSymbol)nativeMarshalerAttributeData.ConstructorArguments[0].Value!;
                ISymbol nativeTypeDiagnosticsTargetSymbol = nativeType;

                if (nativeType is not INamedTypeSymbol marshalerType)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeType, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustHaveRequiredShapeRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    return;
                }

                DiagnosticDescriptor requiredShapeRule = NativeTypeMustHaveRequiredShapeRule;

                ManualTypeMarshallingHelper.NativeTypeMarshallingVariant variant = ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.Standard;
                if (marshalerType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(_genericContiguousCollectionMarshallerAttribute, a.AttributeClass)))
                {
                    variant = ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.ContiguousCollection;
                    requiredShapeRule = CollectionNativeTypeMustHaveRequiredShapeRule;
                    if (!ManualTypeMarshallingHelper.TryGetManagedValuesProperty(marshalerType, out _)
                        || !ManualTypeMarshallingHelper.HasNativeValueStorageProperty(marshalerType, _spanOfByte))
                    {
                        context.ReportDiagnostic(
                            GetDiagnosticLocations(context, marshalerType, nativeMarshalerAttributeData).CreateDiagnostic(
                                requiredShapeRule,
                                nativeType.ToDisplayString(),
                                type.ToDisplayString()));
                        return;
                    }
                }

                if (!nativeType.IsValueType)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeType, nativeMarshalerAttributeData).CreateDiagnostic(
                            requiredShapeRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    return;
                }

                if (marshalerType.IsUnboundGenericType)
                {
                    if (!isNativeMarshallingAttribute)
                    {
                        context.ReportDiagnostic(
                            nativeMarshalerAttributeData.CreateDiagnostic(
                                NativeGenericTypeMustBeClosedOrMatchArityRule,
                                nativeType.ToDisplayString(),
                                type.ToDisplayString()));
                        return;
                    }
                    if (type is not INamedTypeSymbol namedType || marshalerType.TypeArguments.Length != namedType.TypeArguments.Length)
                    {
                        context.ReportDiagnostic(
                            nativeMarshalerAttributeData.CreateDiagnostic(
                                NativeGenericTypeMustBeClosedOrMatchArityRule,
                                nativeType.ToDisplayString(),
                                type.ToDisplayString()));
                        return;
                    }
                    // Construct the marshaler type around the same type arguments as the managed type.
                    nativeType = marshalerType = marshalerType.ConstructedFrom.Construct(namedType.TypeArguments, namedType.TypeArgumentNullableAnnotations);
                }

                bool hasConstructor = false;
                bool hasCallerAllocSpanConstructor = false;
                foreach (IMethodSymbol ctor in marshalerType.Constructors)
                {
                    if (ctor.IsStatic)
                    {
                        continue;
                    }

                    hasConstructor = hasConstructor || ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type, variant);

                    if (!hasCallerAllocSpanConstructor && ManualTypeMarshallingHelper.IsCallerAllocatedSpanConstructor(ctor, type, _spanOfByte, variant))
                    {
                        hasCallerAllocSpanConstructor = true;
                        IFieldSymbol bufferSizeField = nativeType.GetMembers(ManualTypeMarshallingHelper.BufferSizeFieldName).OfType<IFieldSymbol>().FirstOrDefault();
                        if (bufferSizeField is null or { DeclaredAccessibility: not Accessibility.Public } or { IsConst: false } or { Type: not { SpecialType: SpecialType.System_Int32 } })
                        {
                            context.ReportDiagnostic(
                                GetDiagnosticLocations(context, ctor, nativeMarshalerAttributeData).CreateDiagnostic(
                                    CallerAllocConstructorMustHaveBufferSizeConstantRule,
                                    nativeType.ToDisplayString()));
                        }
                    }
                }

                bool hasToManaged = ManualTypeMarshallingHelper.HasToManagedMethod(marshalerType, type);

                // Validate that the native type has at least one marshalling method (either managed to native or native to managed)
                if (!hasConstructor && !hasCallerAllocSpanConstructor && !hasToManaged)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, marshalerType, nativeMarshalerAttributeData).CreateDiagnostic(
                            requiredShapeRule,
                            marshalerType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                // Validate that this type can support marshalling when stackalloc is not usable.
                if (isNativeMarshallingAttribute && hasCallerAllocSpanConstructor && !hasConstructor)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, marshalerType, nativeMarshalerAttributeData).CreateDiagnostic(
                            CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                            marshalerType.ToDisplayString()));
                }

                IPropertySymbol? valueProperty = ManualTypeMarshallingHelper.FindValueProperty(nativeType);
                bool valuePropertyIsRefReturn = valueProperty is { ReturnsByRef: true } or { ReturnsByRefReadonly: true };

                if (valueProperty is not null)
                {
                    if (valuePropertyIsRefReturn)
                    {
                        context.ReportDiagnostic(
                            GetDiagnosticLocations(context, valueProperty, nativeMarshalerAttributeData).CreateDiagnostic(
                                RefValuePropertyUnsupportedRule,
                                marshalerType.ToDisplayString()));
                    }

                    nativeType = valueProperty.Type;
                    nativeTypeDiagnosticsTargetSymbol = valueProperty;

                    // Validate that we don't have partial implementations.
                    // We error if either of the conditions below are partially met but not fully met:
                    //  - a constructor and a Value property getter
                    //  - a ToManaged method and a Value property setter
                    if ((hasConstructor || hasCallerAllocSpanConstructor) && valueProperty.GetMethod is null)
                    {
                        context.ReportDiagnostic(
                            GetDiagnosticLocations(context, valueProperty, nativeMarshalerAttributeData).CreateDiagnostic(
                                ValuePropertyMustHaveGetterRule,
                                marshalerType.ToDisplayString()));
                    }
                    if (hasToManaged && valueProperty.SetMethod is null)
                    {
                        context.ReportDiagnostic(
                            GetDiagnosticLocations(context, valueProperty, nativeMarshalerAttributeData).CreateDiagnostic(
                                ValuePropertyMustHaveSetterRule,
                                marshalerType.ToDisplayString()));
                    }
                }
                else if (ManualTypeMarshallingHelper.FindGetPinnableReference(marshalerType) is IMethodSymbol marshallerGetPinnableReferenceMethod)
                {
                    // If we don't have a Value property, then we disallow a GetPinnableReference on the marshaler type.
                    // We do this since there is no valid use case that we can think of for a GetPinnableReference on a blittable type
                    // being a requirement to calculate the value of the fields of the same blittable instance,
                    // so we're pre-emptively blocking this until a use case is discovered.
                    context.ReportDiagnostic(
                        marshallerGetPinnableReferenceMethod.CreateDiagnostic(
                            MarshallerGetPinnableReferenceRequiresValuePropertyRule,
                            nativeType.ToDisplayString()));
                }

                if (!nativeType.IsConsideredBlittable())
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeTypeDiagnosticsTargetSymbol, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustBeBlittableRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                if (isNativeMarshallingAttribute && ManualTypeMarshallingHelper.FindGetPinnableReference(type) is IMethodSymbol managedGetPinnableReferenceMethod)
                {
                    if (!managedGetPinnableReferenceMethod.ReturnType.IsConsideredBlittable())
                    {
                        context.ReportDiagnostic(managedGetPinnableReferenceMethod.CreateDiagnostic(GetPinnableReferenceReturnTypeBlittableRule));
                    }
                    // Validate that our marshaler supports scenarios where GetPinnableReference cannot be used.
                    if (isNativeMarshallingAttribute && (!hasConstructor || valueProperty is { GetMethod: null }))
                    {
                        context.ReportDiagnostic(
                            nativeMarshalerAttributeData.CreateDiagnostic(
                                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                                type.ToDisplayString()));
                    }

                    // If the managed type has a GetPinnableReference method, make sure that the Value getter is also a pointer-sized primitive.
                    // This ensures that marshalling via pinning the managed value and marshalling via the default marshaller will have the same ABI.
                    if (!valuePropertyIsRefReturn // Ref returns are already reported above as invalid, so don't issue another warning here about them
                        && nativeType is not (
                        IPointerTypeSymbol _ or
                        { SpecialType: SpecialType.System_IntPtr } or
                        { SpecialType: SpecialType.System_UIntPtr }))
                    {
                        IMethodSymbol getPinnableReferenceMethodToMention = managedGetPinnableReferenceMethod;

                        context.ReportDiagnostic(
                            GetDiagnosticLocations(context, nativeTypeDiagnosticsTargetSymbol, nativeMarshalerAttributeData).CreateDiagnostic(
                                NativeTypeMustBePointerSizedRule,
                                nativeType.ToDisplayString(),
                                getPinnableReferenceMethodToMention.ContainingType.ToDisplayString()));
                    }
                }
            }

            private ImmutableArray<Location> GetDiagnosticLocations(SymbolAnalysisContext context, ISymbol targetSymbol, AttributeData marshallingAttribute)
            {
                // If we're using a compilation that references another compilation, the symbol locations can be in source in the wrong compilation,
                // which can cause exceptions when reporting diagnostics. Make sure the symbol is defined in the current Compilation's source module before using its locations.
                // If the symbol is not defined in the current Compilation's source module, report the diagnostic at the marshalling attribute's location.
                if (SymbolEqualityComparer.Default.Equals(context.Compilation.SourceModule, targetSymbol.ContainingModule))
                {
                    return targetSymbol.Locations;
                }
                return ImmutableArray.Create(marshallingAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None);
            }
        }
    }
}
