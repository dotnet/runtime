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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CustomTypeMarshallerAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static class MissingMemberNames
        {
            public const string Key = nameof(MissingMemberNames);
            public const char Delimiter = ' ';
        }

        public static readonly DiagnosticDescriptor MarshallerTypeMustSpecifyManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomTypeMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidCustomTypeMarshallerAttributeUsageTitle)),
                GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustHaveCustomTypeMarshallerAttributeRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidNativeTypeTitle)),
                GetResourceString(nameof(SR.NativeTypeMustHaveCustomTypeMarshallerAttributeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.NativeTypeMustHaveCustomTypeMarshallerAttributeDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBeBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidNativeTypeTitle)),
                GetResourceString(nameof(SR.NativeTypeMustBeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.NativeTypeMustBeBlittableDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceReturnTypeBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableDescription)));

        public static readonly DiagnosticDescriptor NativeTypeMustBePointerSizedRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidNativeTypeTitle)),
                GetResourceString(nameof(SR.NativeTypeMustBePointerSizedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.NativeTypeMustBePointerSizedDescription)));

        public static readonly DiagnosticDescriptor ValueInRequiresOneParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.ValueInRequiresOneParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ValueInRequiresOneParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresTwoParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInRequiresTwoParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInRequiresTwoParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor ValueInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.ValueInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ValueInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor OutRequiresToManagedRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.OutRequiresToManagedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.OutRequiresToManagedDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionOutRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionOutRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionOutRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor CallerAllocConstructorMustHaveBufferSizeRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocConstructorMustHaveBufferSize,
                GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeTitle)),
                GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeDescription)));

        public static readonly DiagnosticDescriptor RefNativeValueUnsupportedRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.RefNativeValueUnsupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RefNativeValueUnsupportedDescription)));

        public static readonly DiagnosticDescriptor NativeGenericTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidNativeTypeTitle)),
                GetResourceString(nameof(SR.NativeGenericTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.NativeGenericTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor TwoStageMarshallingNativeTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.TwoStageMarshallingNativeTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TwoStageMarshallingNativeTypesMustMatchDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionElementTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionElementTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionElementTypesMustMatchDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                MarshallerTypeMustSpecifyManagedTypeRule,
                NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                NativeTypeMustBeBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                NativeTypeMustBePointerSizedRule,
                ValueInRequiresOneParameterConstructorRule,
                LinearCollectionInRequiresTwoParameterConstructorRule,
                OutRequiresToManagedRule,
                LinearCollectionInRequiresCollectionMethodsRule,
                LinearCollectionOutRequiresCollectionMethodsRule,
                CallerAllocConstructorMustHaveBufferSizeRule,
                RefNativeValueUnsupportedRule,
                NativeGenericTypeMustBeClosedOrMatchArityRule,
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
                context.RegisterSymbolAction(PerCompilationAnalyzer.AnalyzeTypeDefinition, SymbolKind.NamedType);
                context.RegisterSymbolAction(PerCompilationAnalyzer.AnalyzeParameterOrField, SymbolKind.Parameter, SymbolKind.Field);
                context.RegisterSymbolAction(PerCompilationAnalyzer.AnalyzeReturnType, SymbolKind.Method);

                // Analyze marshaller type to validate shape.
                context.RegisterSymbolAction(perCompilationAnalyzer.AnalyzeMarshallerType, SymbolKind.NamedType);
            }
        }

        private sealed class PerCompilationAnalyzer
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

            public static void AnalyzeTypeDefinition(SymbolAnalysisContext context)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
                (AttributeData? attributeData, INamedTypeSymbol? entryType) = ManualTypeMarshallingHelper.GetDefaultMarshallerEntryType(type);
                if (attributeData is null)
                {
                    return;
                }

                AnalyzeManagedTypeMarshallingInfo(context, type, attributeData, entryType);
            }

            public static void AnalyzeParameterOrField(SymbolAnalysisContext context)
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

            public static void AnalyzeReturnType(SymbolAnalysisContext context)
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

            private static void AnalyzeManagedTypeMarshallingInfo(
                SymbolAnalysisContext context,
                ITypeSymbol managedType,
                AttributeData attributeData,
                INamedTypeSymbol? entryType)
            {
                if (entryType is null)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeTypeMustHaveCustomTypeMarshallerAttributeRule,
                            managedType.ToDisplayString()));
                    return;
                }

                if (entryType.IsUnboundGenericType)
                {
                    context.ReportDiagnostic(
                        attributeData.CreateDiagnostic(
                            NativeGenericTypeMustBeClosedOrMatchArityRule,
                            entryType.ToDisplayString(),
                            managedType.ToDisplayString()));
                }


                if (!ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(entryType))
                {
                    // TODO: Report that the marshaller needs to be an entry-point marshaller type.
                    return;
                }
            }

#pragma warning disable CA1822 // Mark members as static
            public void AnalyzeMarshallerType(SymbolAnalysisContext context)
#pragma warning restore CA1822 // Mark members as static
            {
                // TODO: Implement for the V2 shapes
            }
        }
    }
}
