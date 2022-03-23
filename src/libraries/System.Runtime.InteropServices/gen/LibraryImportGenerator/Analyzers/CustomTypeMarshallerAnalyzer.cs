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
    public class CustomTypeMarshallerAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public const string MissingFeaturesKey = nameof(MissingFeaturesKey);

        public static class MissingMemberNames
        {
            public const string Key = nameof(MissingMemberNames);
            public const char Delimiter = ' ';

            public const string ValueManagedToNativeConstructor = nameof(ValueManagedToNativeConstructor);
            public const string ValueCallerAllocatedBufferConstructor = nameof(ValueCallerAllocatedBufferConstructor);
            public const string CollectionManagedToNativeConstructor = nameof(CollectionManagedToNativeConstructor);
            public const string CollectionCallerAllocatedBufferConstructor = nameof(CollectionCallerAllocatedBufferConstructor);
            public const string CollectionNativeElementSizeConstructor = nameof(CollectionNativeElementSizeConstructor);
        }

        public static readonly DiagnosticDescriptor MarshallerTypeMustSpecifyManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.MarshallerTypeMustSpecifyManagedType,
                "MarshallerTypeMustSpecifyManagedType",
                GetResourceString(nameof(Resources.MarshallerTypeMustSpecifyManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerTypeMustSpecifyManagedTypeDescription)));

        public static readonly DiagnosticDescriptor CustomTypeMarshallerAttributeMustBeValidRule =
            new DiagnosticDescriptor(
                Ids.CustomTypeMarshallerAttributeMustBeValid,
                "CustomTypeMarshallerAttributeMustBeValid",
                GetResourceString(nameof(Resources.CustomTypeMarshallerAttributeMustBeValidMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CustomTypeMarshallerAttributeMustBeValidDescription)));

        public static readonly DiagnosticDescriptor MarshallerKindMustBeValidRule =
            new DiagnosticDescriptor(
                Ids.CustomTypeMarshallerAttributeMustBeValid,
                "CustomTypeMarshallerAttributeMustBeValid",
                GetResourceString(nameof(Resources.MarshallerKindMustBeValidMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerKindMustBeValidDescription)));

        public static readonly DiagnosticDescriptor MarshallerDirectionMustBeValidRule =
            new DiagnosticDescriptor(
                Ids.CustomTypeMarshallerAttributeMustBeValid,
                "CustomTypeMarshallerAttributeMustBeValid",
                GetResourceString(nameof(Resources.MarshallerDirectionMustBeValidMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerDirectionMustBeValidDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustHaveCustomTypeMarshallerAttributeRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                "InvalidNativeType",
                GetResourceString(nameof(Resources.NativeTypeMustHaveCustomTypeMarshallerAttributeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustHaveCustomTypeMarshallerAttributeDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                "InvalidNativeType",
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
                Ids.InvalidNativeType,
                "InvalidNativeType",
                GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeTypeMustBePointerSizedDescription)));

        public static readonly DiagnosticDescriptor CustomMarshallerTypeMustSupportDirectionRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustSupportDirection,
                "CustomMarshallerTypeMustSupportDirection",
                GetResourceString(nameof(Resources.CustomMarshallerTypeMustSupportDirectionMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CustomMarshallerTypeMustSupportDirectionDescription)));

        public static readonly DiagnosticDescriptor ValueInRequiresOneParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.ValueInRequiresOneParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValueInRequiresOneParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresTwoParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.LinearCollectionInRequiresTwoParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionInRequiresTwoParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor ValueInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.ValueInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ValueInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor OutRequiresToManagedRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.OutRequiresToManagedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.OutRequiresToManagedDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.LinearCollectionInRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionInRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionOutRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.LinearCollectionOutRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionOutRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionOutRequiresIntConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.LinearCollectionOutRequiresIntConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionOutRequiresIntConstructorDescription)));

        public static readonly DiagnosticDescriptor UnmanagedResourcesRequiresFreeNativeRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.UnmanagedResourcesRequiresFreeNativeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.UnmanagedResourcesRequiresFreeNativeDescription)));

        public static readonly DiagnosticDescriptor OutTwoStageMarshallingRequiresFromNativeValueRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.OutTwoStageMarshallingRequiresFromNativeValueMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.OutTwoStageMarshallingRequiresFromNativeValueDescription)));

        public static readonly DiagnosticDescriptor InTwoStageMarshallingRequiresToNativeValueRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                "CustomMarshallerTypeMustHaveRequiredShape",
                GetResourceString(nameof(Resources.InTwoStageMarshallingRequiresToNativeValueMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.InTwoStageMarshallingRequiresToNativeValueDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.MissingAllocatingMarshallingFallback,
                "GetPinnableReferenceShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackDescription)));

        public static readonly DiagnosticDescriptor CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule =
            new DiagnosticDescriptor(
                Ids.MissingAllocatingMarshallingFallback,
                "CallerAllocMarshallingShouldSupportAllocatingMarshallingFallback",
                GetResourceString(nameof(Resources.CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackDescription)));

        public static readonly DiagnosticDescriptor CallerAllocConstructorMustHaveBufferSizeRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocConstructorMustHaveBufferSize,
                "CallerAllocConstructorMustHaveBufferSize",
                GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocConstructorMustHaveBufferSizeDescription)));

        public static readonly DiagnosticDescriptor RefNativeValueUnsupportedRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                "InvalidSignaturesInMarshallerShape",
                GetResourceString(nameof(Resources.RefNativeValueUnsupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.RefNativeValueUnsupportedDescription)));

        public static readonly DiagnosticDescriptor NativeGenericTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                "NativeGenericTypeMustBeClosedOrMatchArity",
                GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.NativeGenericTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor MarshallerGetPinnableReferenceRequiresTwoStageMarshallingRule =
            new DiagnosticDescriptor(
                Ids.MarshallerGetPinnableReferenceRequiresTwoStageMarshalling,
                "MarshallerGetPinnableReferenceRequiresTwoStageMarshalling",
                GetResourceString(nameof(Resources.MarshallerGetPinnableReferenceRequiresTwoStageMarshallingMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.MarshallerGetPinnableReferenceRequiresTwoStageMarshallingDescription)));

        public static readonly DiagnosticDescriptor FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureRule =
            new DiagnosticDescriptor(
                Ids.ProvidedMethodsNotSpecifiedInShape,
                "ProvidedMethodsNotSpecifiedInShape",
                GetResourceString(nameof(Resources.FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureDescription)));

        public static readonly DiagnosticDescriptor ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule =
            new DiagnosticDescriptor(
                Ids.ProvidedMethodsNotSpecifiedInShape,
                "ProvidedMethodsNotSpecifiedInShape",
                GetResourceString(nameof(Resources.ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureDescription)));

        public static readonly DiagnosticDescriptor FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule =
            new DiagnosticDescriptor(
                Ids.ProvidedMethodsNotSpecifiedInShape,
                "ProvidedMethodsNotSpecifiedInShape",
                GetResourceString(nameof(Resources.FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureDescription)));

        public static readonly DiagnosticDescriptor CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureRule =
            new DiagnosticDescriptor(
                Ids.ProvidedMethodsNotSpecifiedInShape,
                "ProvidedMethodsNotSpecifiedInShape",
                GetResourceString(nameof(Resources.CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureDescription)));

        public static readonly DiagnosticDescriptor TwoStageMarshallingNativeTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                "InvalidSignaturesInMarshallerShape",
                GetResourceString(nameof(Resources.TwoStageMarshallingNativeTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TwoStageMarshallingNativeTypesMustMatchDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionElementTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                "InvalidSignaturesInMarshallerShape",
                GetResourceString(nameof(Resources.LinearCollectionElementTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.LinearCollectionElementTypesMustMatchDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                MarshallerTypeMustSpecifyManagedTypeRule,
                CustomTypeMarshallerAttributeMustBeValidRule,
                MarshallerKindMustBeValidRule,
                MarshallerDirectionMustBeValidRule,
                NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                NativeTypeMustBeBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                NativeTypeMustBePointerSizedRule,
                ValueInRequiresOneParameterConstructorRule,
                LinearCollectionInRequiresTwoParameterConstructorRule,
                OutRequiresToManagedRule,
                LinearCollectionInRequiresCollectionMethodsRule,
                LinearCollectionOutRequiresCollectionMethodsRule,
                LinearCollectionOutRequiresIntConstructorRule,
                CustomMarshallerTypeMustSupportDirectionRule,
                OutTwoStageMarshallingRequiresFromNativeValueRule,
                InTwoStageMarshallingRequiresToNativeValueRule,
                GetPinnableReferenceShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                CallerAllocConstructorMustHaveBufferSizeRule,
                RefNativeValueUnsupportedRule,
                NativeGenericTypeMustBeClosedOrMatchArityRule,
                MarshallerGetPinnableReferenceRequiresTwoStageMarshallingRule,
                FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureRule,
                ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule,
                FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule,
                CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureRule,
                TwoStageMarshallingNativeTypesMustMatchRule,
                LinearCollectionElementTypesMustMatchRule);

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
                (bool hasCustomTypeMarshallerAttribute, ITypeSymbol? type, CustomTypeMarshallerData? marshallerDataMaybe) = ManualTypeMarshallingHelper.GetMarshallerShapeInfo(marshallerType);
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

                if (marshallerDataMaybe is not { } marshallerData)
                {
                    context.ReportDiagnostic(marshallerType.CreateDiagnostic(CustomTypeMarshallerAttributeMustBeValidRule, marshallerType.ToDisplayString()));
                    return;
                }

                if (!Enum.IsDefined(typeof(CustomTypeMarshallerKind), marshallerData.Kind))
                {
                    context.ReportDiagnostic(marshallerType.CreateDiagnostic(MarshallerKindMustBeValidRule, marshallerType.ToDisplayString()));
                    return;
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

                IMethodSymbol? inConstructor = null;
                IMethodSymbol? callerAllocatedSpanConstructor = null;
                IMethodSymbol collectionOutConstructor = null;
                foreach (IMethodSymbol ctor in marshallerType.Constructors)
                {
                    if (ctor.IsStatic)
                    {
                        continue;
                    }

                    if (inConstructor is null && ManualTypeMarshallingHelper.IsManagedToNativeConstructor(ctor, type, marshallerData.Kind))
                    {
                        inConstructor = ctor;
                    }

                    if (callerAllocatedSpanConstructor is null && ManualTypeMarshallingHelper.IsCallerAllocatedSpanConstructor(ctor, type, _spanOfByte, marshallerData.Kind))
                    {
                        callerAllocatedSpanConstructor = ctor;
                    }
                    if (collectionOutConstructor is null && ctor.Parameters.Length == 1 && ctor.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
                    {
                        collectionOutConstructor = ctor;
                    }
                }

                if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.In))
                {
                    if (inConstructor is null)
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                GetInConstructorShapeRule(marshallerData.Kind),
                                ImmutableDictionary<string, string>.Empty.Add(
                                    MissingMemberNames.Key,
                                    GetInConstructorMissingMemberName(marshallerData.Kind)),
                                marshallerType.ToDisplayString(),
                                type.ToDisplayString()));
                    }
                    if (marshallerData.Features.HasFlag(CustomTypeMarshallerFeatures.CallerAllocatedBuffer))
                    {
                        if (callerAllocatedSpanConstructor is null)
                        {
                            context.ReportDiagnostic(
                                marshallerType.CreateDiagnostic(
                                    GetCallerAllocatedBufferConstructorShapeRule(marshallerData.Kind),
                                    ImmutableDictionary<string, string>.Empty.Add(
                                        MissingMemberNames.Key,
                                        GetCallerAllocatedBufferConstructorMissingMemberName(marshallerData.Kind)),
                                    marshallerType.ToDisplayString(),
                                    type.ToDisplayString()));
                        }
                        if (marshallerData.BufferSize == null)
                        {
                            context.ReportDiagnostic(
                                (callerAllocatedSpanConstructor ?? (ISymbol)marshallerType).CreateDiagnostic(
                                    CallerAllocConstructorMustHaveBufferSizeRule,
                                    marshallerType.ToDisplayString()));
                        }
                    }
                    else if (callerAllocatedSpanConstructor is not null)
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                CallerAllocatedBufferConstructorProvidedShouldSpecifyFeatureRule,
                                ImmutableDictionary<string, string>.Empty.Add(
                                    MissingFeaturesKey,
                                    nameof(CustomTypeMarshallerFeatures.CallerAllocatedBuffer)),
                            marshallerType.ToDisplayString()));
                    }

                    // Validate that this type can support marshalling when stackalloc is not usable.
                    if (callerAllocatedSpanConstructor is not null && inConstructor is null)
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                CallerAllocMarshallingShouldSupportAllocatingMarshallingFallbackRule,
                                marshallerType.ToDisplayString()));
                    }
                }

                if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.Out) && !ManualTypeMarshallingHelper.HasToManagedMethod(marshallerType, type))
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            OutRequiresToManagedRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingMemberNames.Key,
                                ShapeMemberNames.Value.ToManaged),
                            marshallerType.ToDisplayString()));
                }

                if (marshallerData.Kind == CustomTypeMarshallerKind.LinearCollection)
                {
                    IMethodSymbol? getManagedValuesSourceMethod = ManualTypeMarshallingHelper.FindGetManagedValuesSourceMethod(marshallerType, _readOnlySpanOfT);
                    IMethodSymbol? getManagedValuesDestinationMethod = ManualTypeMarshallingHelper.FindGetManagedValuesDestinationMethod(marshallerType, _spanOfT);
                    IMethodSymbol? getNativeValuesSourceMethod = ManualTypeMarshallingHelper.FindGetNativeValuesSourceMethod(marshallerType, _readOnlySpanOfByte);
                    IMethodSymbol? getNativeValuesDestinationMethod = ManualTypeMarshallingHelper.FindGetNativeValuesDestinationMethod(marshallerType, _spanOfByte);
                    if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.In) && (getManagedValuesSourceMethod is null || getNativeValuesDestinationMethod is null))
                    {
                        var missingMembers = (getManagedValuesSourceMethod, getNativeValuesDestinationMethod) switch
                        {
                            (null, not null) => ShapeMemberNames.LinearCollection.GetManagedValuesSource,
                            (not null, null) => ShapeMemberNames.LinearCollection.GetNativeValuesDestination,
                            (null, null) => $"{ShapeMemberNames.LinearCollection.GetManagedValuesSource}{MissingMemberNames.Delimiter}{ShapeMemberNames.LinearCollection.GetNativeValuesDestination}",
                            (not null, not null) => string.Empty
                        };
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                LinearCollectionInRequiresCollectionMethodsRule,
                                ImmutableDictionary<string, string>.Empty.Add(
                                    MissingMemberNames.Key,
                                    missingMembers),
                                marshallerType.ToDisplayString()));
                    }

                    if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.Out) && (getNativeValuesSourceMethod is null || getManagedValuesDestinationMethod is null))
                    {
                        var missingMembers = (getNativeValuesSourceMethod, getManagedValuesDestinationMethod) switch
                        {
                            (not null, null) => ShapeMemberNames.LinearCollection.GetNativeValuesSource,
                            (null, not null) => ShapeMemberNames.LinearCollection.GetManagedValuesDestination,
                            (null, null) => $"{ShapeMemberNames.LinearCollection.GetNativeValuesSource}{MissingMemberNames.Delimiter}{ShapeMemberNames.LinearCollection.GetManagedValuesDestination}",
                            (not null, not null) => string.Empty
                        };
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                LinearCollectionOutRequiresCollectionMethodsRule,
                                ImmutableDictionary<string, string>.Empty.Add(
                                    MissingMemberNames.Key,
                                    missingMembers),
                                marshallerType.ToDisplayString()));
                    }

                    if (getManagedValuesSourceMethod is not null
                        && getManagedValuesDestinationMethod is not null
                        && !SymbolEqualityComparer.Default.Equals(
                            ((INamedTypeSymbol)getManagedValuesSourceMethod.ReturnType).TypeArguments[0],
                            ((INamedTypeSymbol)getManagedValuesDestinationMethod.ReturnType).TypeArguments[0]))
                    {
                        context.ReportDiagnostic(getManagedValuesSourceMethod.CreateDiagnostic(LinearCollectionElementTypesMustMatchRule));
                    }
                    if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.Out) && collectionOutConstructor is null)
                    {
                        context.ReportDiagnostic(
                            marshallerType.CreateDiagnostic(
                                LinearCollectionOutRequiresIntConstructorRule,
                                ImmutableDictionary<string, string>.Empty.Add(
                                    MissingMemberNames.Key,
                                    MissingMemberNames.CollectionNativeElementSizeConstructor),
                                marshallerType.ToDisplayString()));
                    }
                }


                // Validate that the native type has at least one marshalling direction (either managed to native or native to managed)
                if ((marshallerData.Direction & CustomTypeMarshallerDirection.Ref) == CustomTypeMarshallerDirection.None)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            CustomMarshallerTypeMustSupportDirectionRule,
                            marshallerType.ToDisplayString()));
                }

                if (marshallerData.Features.HasFlag(CustomTypeMarshallerFeatures.UnmanagedResources) && !ManualTypeMarshallingHelper.HasFreeNativeMethod(marshallerType))
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            UnmanagedResourcesRequiresFreeNativeRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingMemberNames.Key,
                                ShapeMemberNames.Value.FreeNative),
                            marshallerType.ToDisplayString(),
                            type.ToDisplayString()));
                }
                else if (!marshallerData.Features.HasFlag(CustomTypeMarshallerFeatures.UnmanagedResources) && ManualTypeMarshallingHelper.HasFreeNativeMethod(marshallerType))
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            FreeNativeMethodProvidedShouldSpecifyUnmanagedResourcesFeatureRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingFeaturesKey,
                                nameof(CustomTypeMarshallerFeatures.UnmanagedResources)),
                            marshallerType.ToDisplayString()));
                }

                IMethodSymbol? toNativeValueMethod = ManualTypeMarshallingHelper.FindToNativeValueMethod(marshallerType);
                IMethodSymbol? fromNativeValueMethod = ManualTypeMarshallingHelper.FindFromNativeValueMethod(marshallerType);
                bool toNativeValueMethodIsRefReturn = toNativeValueMethod is { ReturnsByRef: true } or { ReturnsByRefReadonly: true };
                ITypeSymbol nativeType = marshallerType;

                if (marshallerData.Features.HasFlag(CustomTypeMarshallerFeatures.TwoStageMarshalling))
                {
                    if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.In) && toNativeValueMethod is null)
                    {
                        context.ReportDiagnostic(marshallerType.CreateDiagnostic(
                            InTwoStageMarshallingRequiresToNativeValueRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingMemberNames.Key,
                                ShapeMemberNames.Value.ToNativeValue),
                            marshallerType.ToDisplayString()));
                    }
                    if (marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.Out) && fromNativeValueMethod is null)
                    {
                        context.ReportDiagnostic(marshallerType.CreateDiagnostic(
                            OutTwoStageMarshallingRequiresFromNativeValueRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingMemberNames.Key,
                                ShapeMemberNames.Value.FromNativeValue),
                            marshallerType.ToDisplayString()));
                    }

                    // ToNativeValue and FromNativeValue must be provided with the same type.
                    if (toNativeValueMethod is not null
                        && fromNativeValueMethod is not null
                        && !SymbolEqualityComparer.Default.Equals(toNativeValueMethod.ReturnType, fromNativeValueMethod.Parameters[0].Type))
                    {
                        context.ReportDiagnostic(toNativeValueMethod.CreateDiagnostic(TwoStageMarshallingNativeTypesMustMatchRule));
                    }
                }
                else if (fromNativeValueMethod is not null)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            FromNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingFeaturesKey,
                                nameof(CustomTypeMarshallerFeatures.TwoStageMarshalling)),
                            marshallerType.ToDisplayString()));
                }
                else if (toNativeValueMethod is not null)
                {
                    context.ReportDiagnostic(
                        marshallerType.CreateDiagnostic(
                            ToNativeValueMethodProvidedShouldSpecifyTwoStageMarshallingFeatureRule,
                            ImmutableDictionary<string, string>.Empty.Add(
                                MissingFeaturesKey,
                                nameof(CustomTypeMarshallerFeatures.TwoStageMarshalling)),
                            marshallerType.ToDisplayString()));
                }

                if (toNativeValueMethod is not null)
                {
                    if (toNativeValueMethodIsRefReturn)
                    {
                        context.ReportDiagnostic(
                            toNativeValueMethod.CreateDiagnostic(
                                RefNativeValueUnsupportedRule,
                                marshallerType.ToDisplayString()));
                    }

                    nativeType = toNativeValueMethod.ReturnType;
                }
                else if (ManualTypeMarshallingHelper.FindGetPinnableReference(marshallerType) is IMethodSymbol marshallerGetPinnableReferenceMethod)
                {
                    // If we don't have a ToNativeValue method, then we disallow a GetPinnableReference on the marshaler type.
                    // We do this since there is no valid use case that we can think of for a GetPinnableReference on a blittable type
                    // being a requirement to calculate the value of the fields of the same blittable instance,
                    // so we're pre-emptively blocking this until a use case is discovered.
                    context.ReportDiagnostic(
                        marshallerGetPinnableReferenceMethod.CreateDiagnostic(
                            MarshallerGetPinnableReferenceRequiresTwoStageMarshallingRule,
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
                    if (!marshallerData.Direction.HasFlag(CustomTypeMarshallerDirection.In))
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
            }

            private DiagnosticDescriptor GetInConstructorShapeRule(CustomTypeMarshallerKind kind) => kind switch
            {
                CustomTypeMarshallerKind.Value => ValueInRequiresOneParameterConstructorRule,
                CustomTypeMarshallerKind.LinearCollection => LinearCollectionInRequiresTwoParameterConstructorRule,
                _ => throw new UnreachableException()
            };
            private string GetInConstructorMissingMemberName(CustomTypeMarshallerKind kind) => kind switch
            {
                CustomTypeMarshallerKind.Value => MissingMemberNames.ValueManagedToNativeConstructor,
                CustomTypeMarshallerKind.LinearCollection => MissingMemberNames.CollectionManagedToNativeConstructor,
                _ => throw new UnreachableException()
            };
            private DiagnosticDescriptor GetCallerAllocatedBufferConstructorShapeRule(CustomTypeMarshallerKind kind) => kind switch
            {
                CustomTypeMarshallerKind.Value => ValueInCallerAllocatedBufferRequiresSpanConstructorRule,
                CustomTypeMarshallerKind.LinearCollection => LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorRule,
                _ => throw new UnreachableException()
            };
            private string GetCallerAllocatedBufferConstructorMissingMemberName(CustomTypeMarshallerKind kind) => kind switch
            {
                CustomTypeMarshallerKind.Value => MissingMemberNames.ValueCallerAllocatedBufferConstructor,
                CustomTypeMarshallerKind.LinearCollection => MissingMemberNames.CollectionCallerAllocatedBufferConstructor,
                _ => throw new UnreachableException()
            };
        }
    }
}
