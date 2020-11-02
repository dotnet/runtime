using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
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
                StackallocConstructorMustHaveStackBufferSizeConstantRule);

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
            var spanOfByte = context.Compilation.GetTypeByMetadataName(TypeNames.System_Span)!.Construct(context.Compilation.GetSpecialType(SpecialType.System_Byte));

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
                    context.ReportDiagnostic(Diagnostic.Create(CannotHaveMultipleMarshallingAttributesRule, blittableTypeAttributeData!.ApplicationSyntaxReference!.GetSyntax().GetLocation(), type.ToDisplayString()));
                }
                else if (blittableTypeAttributeData is not null && (!type.HasOnlyBlittableFields() || type.IsAutoLayout(StructLayoutAttribute)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(BlittableTypeMustBeBlittableRule, blittableTypeAttributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), type.ToDisplayString()));
                }
                else if (nativeMarshallingAttributeData is not null)
                {
                    AnalyzeNativeMarshalerType(context, type, nativeMarshallingAttributeData, validateGetPinnableReference: true, validateAllScenarioSupport: true);
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

            private void AnalyzeNativeMarshalerType(SymbolAnalysisContext context, ITypeSymbol type, AttributeData nativeMarshalerAttributeData, bool validateGetPinnableReference, bool validateAllScenarioSupport)
            {
                if (nativeMarshalerAttributeData.ConstructorArguments[0].IsNull)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustBeNonNullRule, nativeMarshalerAttributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), type.ToDisplayString()));
                    return;
                }

                ITypeSymbol nativeType = (ITypeSymbol)nativeMarshalerAttributeData.ConstructorArguments[0].Value!;

                if (!nativeType.IsValueType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustHaveRequiredShapeRule, GetSyntaxReferenceForDiagnostic(nativeType).GetSyntax().GetLocation(), nativeType.ToDisplayString(), type.ToDisplayString()));
                    return;
                }

                if (nativeType is not INamedTypeSymbol marshalerType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustHaveRequiredShapeRule, nativeMarshalerAttributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), nativeType.ToDisplayString(), type.ToDisplayString()));
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

                    hasConstructor = hasConstructor || ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type);

                    if (!hasStackallocConstructor && ManualTypeMarshallingHelper.IsStackallocConstructor(ctor, type, SpanOfByte))
                    {
                        hasStackallocConstructor = true;
                        IFieldSymbol stackAllocSizeField = nativeType.GetMembers("StackBufferSize").OfType<IFieldSymbol>().FirstOrDefault();
                        if (stackAllocSizeField is null or { DeclaredAccessibility: not Accessibility.Public } or { IsConst: false } or { Type: not { SpecialType: SpecialType.System_Int32 } })
                        {
                            context.ReportDiagnostic(Diagnostic.Create(StackallocConstructorMustHaveStackBufferSizeConstantRule, ctor.DeclaringSyntaxReferences[0].GetSyntax()!.GetLocation(), nativeType.ToDisplayString()));
                        }
                    }
                }

                bool hasToManaged = ManualTypeMarshallingHelper.HasToManagedMethod(marshalerType, type);

                // Validate that the native type has at least one marshalling method (either managed to native or native to managed)
                if (!hasConstructor && !hasStackallocConstructor && !hasToManaged)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustHaveRequiredShapeRule, GetSyntaxReferenceForDiagnostic(marshalerType).GetSyntax().GetLocation(), marshalerType.ToDisplayString(), type.ToDisplayString()));
                }

                // Validate that this type can support marshalling when stackalloc is not usable.
                if (validateAllScenarioSupport && hasStackallocConstructor && !hasConstructor)
                {
                    context.ReportDiagnostic(Diagnostic.Create(StackallocMarshallingShouldSupportAllocatingMarshallingFallbackRule, GetSyntaxReferenceForDiagnostic(marshalerType).GetSyntax().GetLocation(), marshalerType.ToDisplayString()));
                }

                IPropertySymbol? valueProperty = ManualTypeMarshallingHelper.FindValueProperty(nativeType);
                if (valueProperty is not null)
                {
                    nativeType = valueProperty.Type;

                    // Validate that we don't have partial implementations.
                    // We error if either of the conditions below are partially met but not fully met:
                    //  - a constructor and a Value property getter
                    //  - a ToManaged method and a Value property setter
                    if ((hasConstructor || hasStackallocConstructor) && valueProperty.GetMethod is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ValuePropertyMustHaveGetterRule, GetSyntaxReferenceForDiagnostic(valueProperty).GetSyntax().GetLocation(), marshalerType.ToDisplayString()));
                    }
                    if (hasToManaged && valueProperty.SetMethod is null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ValuePropertyMustHaveSetterRule, GetSyntaxReferenceForDiagnostic(valueProperty).GetSyntax().GetLocation(), marshalerType.ToDisplayString()));
                    }
                }
                
                if (!nativeType.IsConsideredBlittable())
                {
                    context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustBeBlittableRule,
                        valueProperty is not null
                        ? GetSyntaxReferenceForDiagnostic(valueProperty).GetSyntax().GetLocation()
                        : GetSyntaxReferenceForDiagnostic(nativeType).GetSyntax().GetLocation(),
                        nativeType.ToDisplayString(),
                        type.ToDisplayString()));
                }

                IMethodSymbol? getPinnableReferenceMethod = type.GetMembers("GetPinnableReference")
                                                                .OfType<IMethodSymbol>()
                                                                .FirstOrDefault(m => m is { Parameters: { Length: 0 } } and ({ ReturnsByRef: true } or { ReturnsByRefReadonly: true }));
                if (validateGetPinnableReference && getPinnableReferenceMethod is not null)
                {
                    if (!getPinnableReferenceMethod.ReturnType.IsConsideredBlittable())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GetPinnableReferenceReturnTypeBlittableRule, getPinnableReferenceMethod.DeclaringSyntaxReferences[0].GetSyntax().GetLocation()));
                    }
                    // Validate that the Value property is a pointer-sized primitive type.
                    if (valueProperty is null ||
                        valueProperty.Type is not (
                            IPointerTypeSymbol _ or
                            { SpecialType: SpecialType.System_IntPtr } or
                            { SpecialType: SpecialType.System_UIntPtr }))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NativeTypeMustBePointerSizedRule,
                            valueProperty is not null
                            ? GetSyntaxReferenceForDiagnostic(valueProperty).GetSyntax().GetLocation()
                            : GetSyntaxReferenceForDiagnostic(nativeType).GetSyntax().GetLocation(),
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                    }

                    // Validate that our marshaler supports scenarios where GetPinnableReference cannot be used.
                    if (validateAllScenarioSupport && (!hasConstructor || valueProperty is { GetMethod: null }))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule, nativeMarshalerAttributeData.ApplicationSyntaxReference!.GetSyntax().GetLocation(), type.ToDisplayString()));
                    }
                }

                SyntaxReference GetSyntaxReferenceForDiagnostic(ISymbol targetSymbol)
                {
                    if (targetSymbol.DeclaringSyntaxReferences.IsEmpty)
                    {
                        return nativeMarshalerAttributeData.ApplicationSyntaxReference!;
                    }
                    else
                    {
                        return targetSymbol.DeclaringSyntaxReferences[0];
                    }
                }
            }
        }
    }
}
