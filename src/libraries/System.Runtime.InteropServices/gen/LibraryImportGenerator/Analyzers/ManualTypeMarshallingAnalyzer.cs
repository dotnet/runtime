// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ManualTypeMarshallingAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static readonly DiagnosticDescriptor MarshallerTypeMustSpecifyManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.MarshallerTypeMustSpecifyManagedType,
                "MarshallerTypeMustSpecifyManagedType",
                GetResourceString(nameof(Resources.MarshallerTypeMustSpecifyManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerTypeMustSpecifyManagedTypeDescription)));

        public static readonly DiagnosticDescriptor MarshallerKindMustBeValidRule =
            new DiagnosticDescriptor(
                Ids.MarshallerKindMustBeValid,
                "MarshallerKindMustBeValid",
                GetResourceString(nameof(Resources.MarshallerKindMustBeValidMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerKindMustBeValidDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustHaveCustomTypeMarshallerAttributeRule =
            new DiagnosticDescriptor(
                Ids.NativeTypeMustHaveCustomTypeMarshallerAttribute,
                "NativeTypeMustHaveCustomTypeMarshallerAttribute",
                GetResourceString(nameof(Resources.NativeTypeMustHaveCustomTypeMarshallerAttributeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustHaveCustomTypeMarshallerAttributeDescription)));

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

        public static readonly DiagnosticDescriptor CallerAllocConstructorMustHaveBufferSizeRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocConstructorMustHaveStackBufferSize,
                "CallerAllocConstructorMustHaveBufferSize",
                GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeDescription)));

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
                MarshallerTypeMustSpecifyManagedTypeRule,
                MarshallerKindMustBeValidRule,
                NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                NativeTypeMustBeBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                NativeTypeMustBePointerSizedRule,
                NativeTypeMustHaveRequiredShapeRule,
                CollectionNativeTypeMustHaveRequiredShapeRule,
                ValuePropertyMustHaveSetterRule,
                ValuePropertyMustHaveGetterRule,
                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocConstructorMustHaveBufferSizeRule,
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
            INamedTypeSymbol? spanOfT = context.Compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata);
            INamedTypeSymbol? spanOfByte = spanOfT?.Construct(context.Compilation.GetSpecialType(SpecialType.System_Byte));
            INamedTypeSymbol? readOnlySpanOfT = context.Compilation.GetTypeByMetadataName(TypeNames.System_ReadOnlySpan_Metadata);
            INamedTypeSymbol? readOnlySpanOfByte = readOnlySpanOfT?.Construct(context.Compilation.GetSpecialType(SpecialType.System_Byte));

            if (spanOfT is not null && readOnlySpanOfT is not null)
            {
                var perCompilationAnalyzer = new PerCompilationAnalyzer(spanOfT, spanOfByte, readOnlySpanOfT, readOnlySpanOfByte);

                // Analyze NativeMarshalling/MarshalUsing for correctness
                context.RegisterSymbolAction(perCompilationAnalyzer.AnalyzeTypeDefinition, SymbolKind.NamedType);
                context.RegisterSymbolAction(perCompilationAnalyzer.AnalyzeElement, SymbolKind.Parameter, SymbolKind.Field);
                context.RegisterSymbolAction(perCompilationAnalyzer.AnalyzeReturnType, SymbolKind.Method);

                // Analyze marshaller type to validate shape.
                context.RegisterSymbolAction(perCompilationAnalyzer.AnalyzeMarshallerType, SymbolKind.NamedType);
            }
        }

        private class PerCompilationAnalyzer
        {
            private readonly INamedTypeSymbol _spanOfT;
            private readonly INamedTypeSymbol _readOnlySpanOfT;
            private readonly INamedTypeSymbol _spanOfByte;
            private readonly INamedTypeSymbol _readOnlySpanOfByte;

            public PerCompilationAnalyzer(INamedTypeSymbol spanOfT, INamedTypeSymbol? spanOfByte, INamedTypeSymbol readOnlySpanOfT, INamedTypeSymbol? readOnlySpanOfByte)
            {
                _spanOfT = spanOfT;
                _spanOfByte = spanOfByte;
                _readOnlySpanOfT = readOnlySpanOfT;
                _readOnlySpanOfByte = readOnlySpanOfByte;
            }

            public void AnalyzeTypeDefinition(SymbolAnalysisContext context)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

                (AttributeData? attributeData, INamedTypeSymbol? marshallerType) = ManualTypeMarshallingHelper.GetDefaultMarshallerInfo(type);

                if (attributeData is null)
                {
                    return;
                }

                AnalyzeManagedTypeMarshallingInfo(context, type, attributeData, marshallerType);
            }

            public void AnalyzeElement(SymbolAnalysisContext context)
            {
                ITypeSymbol managedType = context.Symbol switch
                {
                    IParameterSymbol param => param.Type,
                    IFieldSymbol field => field.Type,
                    _ => throw new InvalidOperationException()
                };
                AttributeData? attributeData = context.Symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.MarshalUsingAttribute);
                if (attributeData is null || attributeData.ConstructorArguments.Length == 0)
                {
                    return;
                }
                AnalyzeManagedTypeMarshallingInfo(context, managedType, attributeData, attributeData.ConstructorArguments[0].Value as INamedTypeSymbol);
            }

            public void AnalyzeReturnType(SymbolAnalysisContext context)
            {
                IMethodSymbol method = (IMethodSymbol)context.Symbol;
                ITypeSymbol managedType = method.ReturnType;
                AttributeData? attributeData = method.GetReturnTypeAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.MarshalUsingAttribute);
                if (attributeData is null || attributeData.ConstructorArguments.Length == 0)
                {
                    return;
                }
                AnalyzeManagedTypeMarshallingInfo(context, managedType, attributeData, attributeData.ConstructorArguments[0].Value as INamedTypeSymbol);
            }

            private static void AnalyzeManagedTypeMarshallingInfo(SymbolAnalysisContext context, ITypeSymbol type, AttributeData attributeData, INamedTypeSymbol? marshallerType)
            {
                if (marshallerType is null)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                            type.ToDisplayString()));
                    return;
                }

                if (marshallerType.IsUnboundGenericType)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeGenericTypeMustBeClosedOrMatchArityRule,
                            marshallerType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                (bool hasCustomTypeMarshallerAttribute, ITypeSymbol? marshallerManagedType, _) = ManualTypeMarshallingHelper.GetMarshallerShapeInfo(marshallerType);

                marshallerManagedType = ManualTypeMarshallingHelper.ResolveManagedType(marshallerManagedType, marshallerType, context.Compilation);

                if (!hasCustomTypeMarshallerAttribute)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                            type.ToDisplayString()));
                    return;
                }

                if (marshallerManagedType is null)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                            type.ToDisplayString()));
                    return;
                }

                if (!TypeSymbolsConstructedFromEqualTypes(type, marshallerManagedType))
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                            type.ToDisplayString()));
                    return;
                }
            }

            private static bool TypeSymbolsConstructedFromEqualTypes(ITypeSymbol left, ITypeSymbol right)
            {
                return (left, right) switch
                {
                    (INamedTypeSymbol namedLeft, INamedTypeSymbol namedRight) => SymbolEqualityComparer.Default.Equals(namedLeft.ConstructedFrom, namedRight.ConstructedFrom),
                    _ => SymbolEqualityComparer.Default.Equals(left, right)
                };
            }

            public void AnalyzeMarshallerType(SymbolAnalysisContext context)
            {
                INamedTypeSymbol marshallerType = (INamedTypeSymbol)context.Symbol;
                (bool hasCustomTypeMarshallerAttribute, ITypeSymbol? type, CustomTypeMarshallerData? marshallerData) = ManualTypeMarshallingHelper.GetMarshallerShapeInfo(marshallerType);
                type = ManualTypeMarshallingHelper.ResolveManagedType(type, marshallerType, context.Compilation);

                if (!hasCustomTypeMarshallerAttribute)
                {
                    return;
                }
                if (type is null)
                {
                    context.ReportDiagnostic(marshallerType.CreateDiagnostic(MarshallerTypeMustSpecifyManagedTypeRule, marshallerType.ToDisplayString()));
                    return;
                }

                if (marshallerData == null || !Enum.IsDefined(typeof(CustomTypeMarshallerKind), marshallerData.Value.Kind))
                {
                    context.ReportDiagnostic(marshallerType.CreateDiagnostic(MarshallerKindMustBeValidRule, marshallerType.ToDisplayString()));
                }

                if (type is INamedTypeSymbol { IsUnboundGenericType: true } generic)
                {
                    if (generic.TypeArguments.Length != marshallerType.TypeArguments.Length)
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                NativeGenericTypeMustBeClosedOrMatchArityRule,
                                marshallerType.ToDisplayString(),
                                type.ToDisplayString()));
                        return;
                    }
                    type = generic.ConstructedFrom.Construct(marshallerType.TypeArguments, marshallerType.TypeArgumentNullableAnnotations);
                }

                DiagnosticDescriptor requiredShapeRule = marshallerData.Value.Kind switch
                {
                    CustomTypeMarshallerKind.Value => NativeTypeMustHaveRequiredShapeRule,
                    CustomTypeMarshallerKind.LinearCollection => CollectionNativeTypeMustHaveRequiredShapeRule,
                    _ => throw new InvalidOperationException()
                };

                if (!marshallerType.IsValueType)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            requiredShapeRule,
                            marshallerType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                bool hasConstructor = false;
                bool hasCallerAllocSpanConstructor = false;
                foreach (IMethodSymbol ctor in marshallerType.Constructors)
                {
                    if (ctor.IsStatic)
                    {
                        continue;
                    }

                    hasConstructor = hasConstructor || ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type, marshallerData.Value.Kind);

                    if (!hasCallerAllocSpanConstructor && ManualTypeMarshallingHelper.IsCallerAllocatedSpanConstructor(ctor, type, _spanOfByte, marshallerData.Value.Kind))
                    {
                        hasCallerAllocSpanConstructor = true;
                        if (marshallerData.Value.BufferSize == null)
                        {
                            context.ReportDiagnostic(
                                ctor.CreateDiagnostic(
                                    CallerAllocConstructorMustHaveBufferSizeRule,
                                    marshallerType.ToDisplayString()));
                        }
                    }
                }

                bool hasToManaged = ManualTypeMarshallingHelper.HasToManagedMethod(marshallerType, type);

                if (marshallerData.Value.Kind == CustomTypeMarshallerKind.LinearCollection)
                {
                    requiredShapeRule = CollectionNativeTypeMustHaveRequiredShapeRule;
                    IMethodSymbol? getManagedValuesSourceMethod = ManualTypeMarshallingHelper.FindGetManagedValuesSourceMethod(marshallerType, _readOnlySpanOfT);
                    IMethodSymbol? getManagedValuesDestinationMethod = ManualTypeMarshallingHelper.FindGetManagedValuesDestinationMethod(marshallerType, _spanOfT);
                    IMethodSymbol? getNativeValuesSourceMethod = ManualTypeMarshallingHelper.FindGetNativeValuesSourceMethod(marshallerType, _readOnlySpanOfByte);
                    IMethodSymbol? getNativeValuesDestinationMethod = ManualTypeMarshallingHelper.FindGetNativeValuesDestinationMethod(marshallerType, _spanOfByte);
                    if ((hasConstructor || hasCallerAllocSpanConstructor) && (getManagedValuesSourceMethod is null || getNativeValuesDestinationMethod is null))
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                requiredShapeRule,
                                marshallerType.ToDisplayString(),
                                type.ToDisplayString()));
                    }

                    if (hasToManaged && (getNativeValuesSourceMethod is null || getManagedValuesDestinationMethod is null))
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                requiredShapeRule,
                                marshallerType.ToDisplayString(),
                                type.ToDisplayString()));
                    }

                    if (getManagedValuesSourceMethod is not null
                        && getManagedValuesDestinationMethod is not null
                        && !SymbolEqualityComparer.Default.Equals(
                            ((INamedTypeSymbol)getManagedValuesSourceMethod.ReturnType).TypeArguments[0],
                            ((INamedTypeSymbol)getManagedValuesDestinationMethod.ReturnType).TypeArguments[0]))
                    {
                        // TODO: Diagnostic for mismatched element collection type
                    }
                }


                // Validate that the native type has at least one marshalling method (either managed to native or native to managed)
                if (!hasConstructor && !hasCallerAllocSpanConstructor && !hasToManaged)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            requiredShapeRule,
                            marshallerType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                // Validate that this type can support marshalling when stackalloc is not usable.
                if (hasCallerAllocSpanConstructor && !hasConstructor)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                            marshallerType.ToDisplayString()));
                }

                IMethodSymbol? toNativeValueMethod = ManualTypeMarshallingHelper.FindToNativeValueMethod(marshallerType);
                IMethodSymbol? fromNativeValueMethod = ManualTypeMarshallingHelper.FindFromNativeValueMethod(marshallerType);
                bool toNativeValueMethodIsRefReturn = toNativeValueMethod is { ReturnsByRef: true } or { ReturnsByRefReadonly: true };
                ITypeSymbol nativeType = marshallerType;

                // If either ToNativeValue or FromNativeValue is provided, validate the scenarios where they are required.
                ValidateTwoStageMarshalling();

                if (toNativeValueMethod is not null)
                {
                    if (toNativeValueMethodIsRefReturn)
                    {
                        context.ReportDiagnostic(
                            toNativeValueMethod.CreateDiagnostic(
                                RefValuePropertyUnsupportedRule,
                                marshallerType.ToDisplayString()));
                    }

                    nativeType = toNativeValueMethod.ReturnType;
                }
                else if (ManualTypeMarshallingHelper.FindGetPinnableReference(marshallerType) is IMethodSymbol marshallerGetPinnableReferenceMethod)
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
                        (toNativeValueMethod ?? (ISymbol)marshallerType).CreateDiagnostic(
                            NativeTypeMustBeBlittableRule,
                            nativeType.ToDisplayString(),
                            type.ToDisplayString()));
                }

                if (SymbolEqualityComparer.Default.Equals(ManualTypeMarshallingHelper.GetDefaultMarshallerInfo(type).marshallerType, marshallerType)
                    && ManualTypeMarshallingHelper.FindGetPinnableReference(type) is IMethodSymbol managedGetPinnableReferenceMethod)
                {
                    if (!managedGetPinnableReferenceMethod.ReturnType.IsConsideredBlittable())
                    {
                        context.ReportDiagnostic(managedGetPinnableReferenceMethod.CreateDiagnostic(GetPinnableReferenceReturnTypeBlittableRule));
                    }
                    // Validate that our marshaler supports scenarios where GetPinnableReference cannot be used.
                    if (!hasConstructor || toNativeValueMethod is null)
                    {
                        context.ReportDiagnostic(
                            type.CreateDiagnostic(
                                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                                type.ToDisplayString()));
                    }

                    // If the managed type has a GetPinnableReference method, make sure that the Value getter is also a pointer-sized primitive.
                    // This ensures that marshalling via pinning the managed value and marshalling via the default marshaller will have the same ABI.
                    if (toNativeValueMethod is not null
                        && !toNativeValueMethodIsRefReturn // Ref returns are already reported above as invalid, so don't issue another warning here about them
                        && nativeType is not (
                        IPointerTypeSymbol or
                        { SpecialType: SpecialType.System_IntPtr } or
                        { SpecialType: SpecialType.System_UIntPtr }))
                    {
                        context.ReportDiagnostic(
                            toNativeValueMethod.CreateDiagnostic(
                                NativeTypeMustBePointerSizedRule,
                                nativeType.ToDisplayString(),
                                managedGetPinnableReferenceMethod.ContainingType.ToDisplayString()));
                    }
                }

                void ValidateTwoStageMarshalling()
                {
                    bool hasTwoStageMarshalling = false;
                    if ((hasConstructor || hasCallerAllocSpanConstructor) && toNativeValueMethod is not null)
                    {
                        hasTwoStageMarshalling = true;
                    }

                    if (hasToManaged && fromNativeValueMethod is not null)
                    {
                        hasTwoStageMarshalling = true;
                    }

                    if (hasTwoStageMarshalling)
                    {
                        if ((hasConstructor || hasCallerAllocSpanConstructor) && toNativeValueMethod is null)
                        {
                            context.ReportDiagnostic(marshallerType.CreateDiagnostic(ValuePropertyMustHaveGetterRule, marshallerType.ToDisplayString()));
                        }
                        if (hasToManaged && fromNativeValueMethod is null)
                        {
                            context.ReportDiagnostic(marshallerType.CreateDiagnostic(ValuePropertyMustHaveSetterRule, marshallerType.ToDisplayString()));
                        }
                    }

                    // ToNativeValue and FromNativeValue must be provided with the same type.
                    if (toNativeValueMethod is not null
                        && fromNativeValueMethod is not null
                        && !SymbolEqualityComparer.Default.Equals(toNativeValueMethod.ReturnType, fromNativeValueMethod.Parameters[0].Type))
                    {
                        // TODO: Add diagnostic.
                    }
                }
            }
        }
    }
}
