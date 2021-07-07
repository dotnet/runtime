using System;
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

        public readonly static DiagnosticDescriptor BlittableTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.BlittableTypeMustBeBlittable,
                "BlittableTypeMustBeBlittable",
                GetResourceString(nameof(Resources.BlittableTypeMustBeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.BlittableTypeMustBeBlittableDescription)));

        public readonly static DiagnosticDescriptor CannotHaveMultipleMarshallingAttributesRule =
            new DiagnosticDescriptor(
                Ids.CannotHaveMultipleMarshallingAttributes,
                "CannotHaveMultipleMarshallingAttributes",
                GetResourceString(nameof(Resources.CannotHaveMultipleMarshallingAttributesMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CannotHaveMultipleMarshallingAttributesDescription)));

        public readonly static DiagnosticDescriptor NativeTypeMustBeNonNullRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBeNonNull,
                "NativeTypeMustBeNonNull",
                GetResourceString(nameof(Resources.NativeTypeMustBeNonNullMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBeNonNullDescription)));

        public readonly static DiagnosticDescriptor NativeTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBeBlittable,
                "NativeTypeMustBeBlittable",
                GetResourceString(nameof(Resources.NativeTypeMustBeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.BlittableTypeMustBeBlittableDescription)));

        public readonly static DiagnosticDescriptor GetPinnableReferenceReturnTypeBlittableRule =
            new DiagnosticDescriptor(
                Ids.GetPinnableReferenceReturnTypeBlittable,
                "GetPinnableReferenceReturnTypeBlittable",
                GetResourceString(nameof(Resources.GetPinnableReferenceReturnTypeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GetPinnableReferenceReturnTypeBlittableDescription)));
    
        public readonly static DiagnosticDescriptor NativeTypeMustBePointerSizedRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustBePointerSized,
                "NativeTypeMustBePointerSized",
                GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedDescription)));

        public readonly static DiagnosticDescriptor NativeTypeMustHaveRequiredShapeRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustHaveRequiredShape,
                "NativeTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.NativeTypeMustHaveRequiredShapeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustHaveRequiredShapeDescription)));

        public readonly static DiagnosticDescriptor ValuePropertyMustHaveSetterRule =
            new DiagnosticDescriptor(
                Ids.ValuePropertyMustHaveSetter,
                "ValuePropertyMustHaveSetter",
                GetResourceString(nameof(Resources.ValuePropertyMustHaveSetterMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValuePropertyMustHaveSetterDescription)));

        public readonly static DiagnosticDescriptor ValuePropertyMustHaveGetterRule =
            new DiagnosticDescriptor(
                Ids.ValuePropertyMustHaveGetter,
                "ValuePropertyMustHaveGetter",
                GetResourceString(nameof(Resources.ValuePropertyMustHaveGetterMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValuePropertyMustHaveGetterDescription)));

        public readonly static DiagnosticDescriptor GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.GetPinnableReferenceShouldSupportAllocatingMarshallingFallback,
                "GetPinnableReferenceShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackDescription)));

        public readonly static DiagnosticDescriptor StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.StackallocMarshallingShouldSupportAllocatingMarshallingFallback,
                "StackallocMarshallingShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.StackallocMarshallingShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.StackallocMarshallingShouldSupportAllocatingMarshallingFallbackDescription)));

        public readonly static DiagnosticDescriptor StackallocConstructorMustHaveStackBufferSizeConstantRule =
            new DiagnosticDescriptor(
                Ids.StackallocConstructorMustHaveStackBufferSizeConstant,
                "StackallocConstructorMustHaveStackBufferSizeConstant",
                GetResourceString(nameof(Resources.StackallocConstructorMustHaveStackBufferSizeConstantMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.StackallocConstructorMustHaveStackBufferSizeConstantDescription)));

        public readonly static DiagnosticDescriptor RefValuePropertyUnsupportedRule =
            new DiagnosticDescriptor(
                Ids.RefValuePropertyUnsupported,
                "RefValuePropertyUnsupported",
                GetResourceString(nameof(Resources.RefValuePropertyUnsupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.RefValuePropertyUnsupportedDescription)));

        public readonly static DiagnosticDescriptor NativeGenericTypeMustBeClosedRule =
            new DiagnosticDescriptor(
                Ids.NativeGenericTypeMustBeClosed,
                "GenericTypeMustBeClosed",
                GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(
                BlittableTypeMustBeBlittableRule,
                CannotHaveMultipleMarshallingAttributesRule,
                NativeTypeMustBeNonNullRule,
                NativeTypeMustBeBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                NativeTypeMustBePointerSizedRule,
                NativeTypeMustHaveRequiredShapeRule,
                ValuePropertyMustHaveSetterRule,
                ValuePropertyMustHaveGetterRule,
                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                StackallocConstructorMustHaveStackBufferSizeConstantRule,
                RefValuePropertyUnsupportedRule,
                NativeGenericTypeMustBeClosedRule);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(PrepareForAnalysis);
        }

        private void PrepareForAnalysis(CompilationStartAnalysisContext context)
        {
            var generatedMarshallingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.GeneratedMarshallingAttribute);
            var blittableTypeAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.BlittableTypeAttribute);
            var nativeMarshallingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.NativeMarshallingAttribute);
            var marshalUsingAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.MarshalUsingAttribute);
            var spanOfByte = context.Compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!.Construct(context.Compilation.GetSpecialType(SpecialType.System_Byte));

            if (generatedMarshallingAttribute is not null
                && blittableTypeAttribute is not null
                && nativeMarshallingAttribute is not null
                && marshalUsingAttribute is not null
                && spanOfByte is not null)
            {
                var perCompilationAnalyzer = new PerCompilationAnalyzer(
                    generatedMarshallingAttribute,
                    blittableTypeAttribute,
                    nativeMarshallingAttribute,
                    marshalUsingAttribute,
                    spanOfByte,
                    context.Compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_StructLayoutAttribute)!);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeTypeDefinition(context), SymbolKind.NamedType);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeElement(context), SymbolKind.Parameter, SymbolKind.Field);
                context.RegisterSymbolAction(context => perCompilationAnalyzer.AnalyzeReturnType(context), SymbolKind.Method);
            }
        }

        class PerCompilationAnalyzer
        {
            private readonly INamedTypeSymbol GeneratedMarshallingAttribute;
            private readonly INamedTypeSymbol BlittableTypeAttribute;
            private readonly INamedTypeSymbol NativeMarshallingAttribute;
            private readonly INamedTypeSymbol MarshalUsingAttribute;
            private readonly INamedTypeSymbol SpanOfByte;
            private readonly INamedTypeSymbol StructLayoutAttribute;

            public PerCompilationAnalyzer(INamedTypeSymbol generatedMarshallingAttribute,
                                          INamedTypeSymbol blittableTypeAttribute,
                                          INamedTypeSymbol nativeMarshallingAttribute,
                                          INamedTypeSymbol marshalUsingAttribute,
                                          INamedTypeSymbol spanOfByte,
                                          INamedTypeSymbol structLayoutAttribute)
            {
                GeneratedMarshallingAttribute = generatedMarshallingAttribute;
                BlittableTypeAttribute = blittableTypeAttribute;
                NativeMarshallingAttribute = nativeMarshallingAttribute;
                MarshalUsingAttribute = marshalUsingAttribute;
                SpanOfByte = spanOfByte;
                StructLayoutAttribute = structLayoutAttribute;
            }

            public void AnalyzeTypeDefinition(SymbolAnalysisContext context)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

                AttributeData? blittableTypeAttributeData = null;
                AttributeData? nativeMarshallingAttributeData = null;
                foreach (var attr in type.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, GeneratedMarshallingAttribute))
                    {
                        // If the type has the GeneratedMarshallingAttribute,
                        // we let the source generator handle error checking.
                        return;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, BlittableTypeAttribute))
                    {
                        blittableTypeAttributeData = attr;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, NativeMarshallingAttribute))
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
                else if (blittableTypeAttributeData is not null && (!type.HasOnlyBlittableFields() || type.IsAutoLayout(StructLayoutAttribute)))
                {
                    context.ReportDiagnostic(
                        blittableTypeAttributeData.CreateDiagnostic(
                            BlittableTypeMustBeBlittableRule,
                            type.ToDisplayString()));
                }
                else if (nativeMarshallingAttributeData is not null)
                {
                    AnalyzeNativeMarshalerType(context, type, nativeMarshallingAttributeData, validateManagedGetPinnableReference: true, validateAllScenarioSupport: true);
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
                AttributeData? attrData = context.Symbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(MarshalUsingAttribute, attr.AttributeClass));
                if (attrData is not null)
                {
                    if (context.Symbol is IParameterSymbol param)
                    {
                        AnalyzeNativeMarshalerType(context, param.Type, attrData, false, false);
                    }
                    else if (context.Symbol is IFieldSymbol field)
                    {
                        AnalyzeNativeMarshalerType(context, field.Type, attrData, false, false);
                    }
                }
            }

            public void AnalyzeReturnType(SymbolAnalysisContext context)
            {
                var method = (IMethodSymbol)context.Symbol;
                AttributeData? attrData = method.GetReturnTypeAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(MarshalUsingAttribute, attr.AttributeClass));
                if (attrData is not null)
                {
                    AnalyzeNativeMarshalerType(context, method.ReturnType, attrData, false, false);
                }
            }

            private void AnalyzeNativeMarshalerType(SymbolAnalysisContext context, ITypeSymbol type, AttributeData nativeMarshalerAttributeData, bool validateManagedGetPinnableReference, bool validateAllScenarioSupport)
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

                if (!nativeType.IsValueType)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeType, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustHaveRequiredShapeRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    return;
                }

                if (nativeType is not INamedTypeSymbol marshalerType)
                {
                    context.ReportDiagnostic(
                        nativeMarshalerAttributeData.CreateDiagnostic(
                            NativeTypeMustHaveRequiredShapeRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    return;
                }

                if (marshalerType.IsUnboundGenericType)
                {
                    context.ReportDiagnostic(
                        nativeMarshalerAttributeData.CreateDiagnostic(
                            NativeGenericTypeMustBeClosedRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    return;
                }

                bool hasConstructor = false;
                bool hasStackallocConstructor = false;
                foreach (var ctor in marshalerType.Constructors)
                {
                    if (ctor.IsStatic)
                    {
                        continue;
                    }

                    hasConstructor = hasConstructor || ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type, ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.Standard);

                    if (!hasStackallocConstructor && ManualTypeMarshallingHelper.IsStackallocConstructor(ctor, type, SpanOfByte, ManualTypeMarshallingHelper.NativeTypeMarshallingVariant.Standard))
                    {
                        hasStackallocConstructor = true;
                        IFieldSymbol stackAllocSizeField = nativeType.GetMembers("StackBufferSize").OfType<IFieldSymbol>().FirstOrDefault();
                        if (stackAllocSizeField is null or { DeclaredAccessibility: not Accessibility.Public } or { IsConst: false } or { Type: not { SpecialType: SpecialType.System_Int32 } })
                        {
                            context.ReportDiagnostic(
                                GetDiagnosticLocations(context, ctor, nativeMarshalerAttributeData).CreateDiagnostic(
                                    StackallocConstructorMustHaveStackBufferSizeConstantRule,
                                    nativeType.ToDisplayString()));
                        }
                    }
                }

                bool hasToManaged = ManualTypeMarshallingHelper.HasToManagedMethod(marshalerType, type);

                // Validate that the native type has at least one marshalling method (either managed to native or native to managed)
                if (!hasConstructor && !hasStackallocConstructor && !hasToManaged)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, marshalerType, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustHaveRequiredShapeRule,
                            marshalerType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                // Validate that this type can support marshalling when stackalloc is not usable.
                if (validateAllScenarioSupport && hasStackallocConstructor && !hasConstructor)
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, marshalerType, nativeMarshalerAttributeData).CreateDiagnostic(
                            StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
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
                    if ((hasConstructor || hasStackallocConstructor) && valueProperty.GetMethod is null)
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

                if (!nativeType.IsConsideredBlittable())
                {
                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeTypeDiagnosticsTargetSymbol, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustBeBlittableRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                // Use a tuple here instead of an anonymous type so we can do the reassignment and pattern matching below.
                var getPinnableReferenceMethods = new
                {
                    Managed = validateManagedGetPinnableReference? ManualTypeMarshallingHelper.FindGetPinnableReference(type) : null,
                    Marshaler = ManualTypeMarshallingHelper.FindGetPinnableReference(marshalerType)
                };

                if (getPinnableReferenceMethods.Managed is not null)
                {
                    if (!getPinnableReferenceMethods.Managed.ReturnType.IsConsideredBlittable())
                    {
                        context.ReportDiagnostic(getPinnableReferenceMethods.Managed.CreateDiagnostic(GetPinnableReferenceReturnTypeBlittableRule));
                    }
                    // Validate that our marshaler supports scenarios where GetPinnableReference cannot be used.
                    if (validateAllScenarioSupport && (!hasConstructor || valueProperty is { GetMethod: null }))
                    {
                        context.ReportDiagnostic(
                            nativeMarshalerAttributeData.CreateDiagnostic(
                                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                                type.ToDisplayString()));
                    }
                }

                if ((getPinnableReferenceMethods.Managed is not null
                      || getPinnableReferenceMethods.Marshaler is not null)
                    && !valuePropertyIsRefReturn // Ref returns are already reported above as invalid, so don't issue another warning here about them
                    && nativeType is not (
                        IPointerTypeSymbol _ or
                        { SpecialType: SpecialType.System_IntPtr } or
                        { SpecialType: SpecialType.System_UIntPtr }))
                {
                    IMethodSymbol getPinnableReferenceMethodToMention = getPinnableReferenceMethods.Managed ?? getPinnableReferenceMethods.Marshaler!;

                    context.ReportDiagnostic(
                        GetDiagnosticLocations(context, nativeTypeDiagnosticsTargetSymbol, nativeMarshalerAttributeData).CreateDiagnostic(
                            NativeTypeMustBePointerSizedRule,
                            nativeType.ToDisplayString(),
                            getPinnableReferenceMethodToMention.ContainingType.ToDisplayString()));
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
