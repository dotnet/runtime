// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                Ids.InvalidMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidCustomMarshallerAttributeUsageTitle)),
                GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeDescription)));

        public static readonly DiagnosticDescriptor MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.EntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.EntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeDescription)));

        public static readonly DiagnosticDescriptor TypeMustBeUnmanagedOrStrictlyBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.TypeMustBeUnmanagedOrStrictlyBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeMustBeUnmanagedOrStrictlyBlittableDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceReturnTypeBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableDescription)));

        public static readonly DiagnosticDescriptor TypeMustHaveExplicitCastFromVoidPointerRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.TypeMustHaveExplicitCastFromVoidPointerMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeMustHaveExplicitCastFromVoidPointerDescription)));

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

        public static readonly DiagnosticDescriptor GenericMarshallerTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.GenericMarshallerTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.GenericMarshallerTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor ToFromUnmanagedTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.ToFromUnmanagedTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ToFromUnmanagedTypesMustMatchDescription)));

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
                MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                TypeMustBeUnmanagedOrStrictlyBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                TypeMustHaveExplicitCastFromVoidPointerRule,
                ValueInRequiresOneParameterConstructorRule,
                LinearCollectionInRequiresTwoParameterConstructorRule,
                OutRequiresToManagedRule,
                LinearCollectionInRequiresCollectionMethodsRule,
                LinearCollectionOutRequiresCollectionMethodsRule,
                CallerAllocConstructorMustHaveBufferSizeRule,
                GenericMarshallerTypeMustBeClosedOrMatchArityRule,
                ToFromUnmanagedTypesMustMatchRule,
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
                var perCompilationAnalyzer = new PerCompilationAnalyzer(context.Compilation, spanOfT, spanOfByte, readOnlySpanOfT, readOnlySpanOfByte);

                // TODO: Change this from a SyntaxNode action to an operation attribute once attribute application is represented in the
                // IOperation tree by Roslyn.
                context.RegisterSyntaxNodeAction(perCompilationAnalyzer.AnalyzeAttribute, SyntaxKind.Attribute);
            }
        }

        private sealed class PerCompilationAnalyzer
        {
            private readonly Compilation _compilation;
            private readonly INamedTypeSymbol _spanOfT;
            private readonly INamedTypeSymbol _readOnlySpanOfT;
            private readonly INamedTypeSymbol _spanOfByte;
            private readonly INamedTypeSymbol _readOnlySpanOfByte;

            public PerCompilationAnalyzer(Compilation compilation, INamedTypeSymbol spanOfT, INamedTypeSymbol? spanOfByte, INamedTypeSymbol readOnlySpanOfT, INamedTypeSymbol? readOnlySpanOfByte)
            {
                _compilation = compilation;
                _spanOfT = spanOfT;
                _spanOfByte = spanOfByte;
                _readOnlySpanOfT = readOnlySpanOfT;
                _readOnlySpanOfByte = readOnlySpanOfByte;
            }

            public void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
            {
                AttributeSyntax syntax = (AttributeSyntax)context.Node;
                ISymbol attributedSymbol = context.ContainingSymbol!;

                AttributeData attr = GetAttributeData(syntax, attributedSymbol);

                if (attr.AttributeClass?.ToDisplayString() == TypeNames.MarshalUsingAttribute
                    && attr.AttributeConstructor is not null
                    && attr.ConstructorArguments.Length > 0)
                {
                    INamedTypeSymbol? entryType = (INamedTypeSymbol?)attr.ConstructorArguments[0].Value;
                    AnalyzeManagedTypeMarshallingInfo(context.ReportDiagnostic, GetSymbolType(attributedSymbol), new DiagnosticFactory(attr.CreateDiagnostic), entryType);
                }
                if (attr.AttributeClass?.ToDisplayString() == TypeNames.NativeMarshallingAttribute
                    && attr.AttributeConstructor is not null)
                {
                    INamedTypeSymbol? entryType = (INamedTypeSymbol?)attr.ConstructorArguments[0].Value;
                    AnalyzeManagedTypeMarshallingInfo(context.ReportDiagnostic, GetSymbolType(attributedSymbol), new DiagnosticFactory(attr.CreateDiagnostic), entryType);
                }

                if (attr.AttributeClass?.ToDisplayString() == TypeNames.CustomMarshallerAttribute
                    && attr.AttributeConstructor is not null)
                {
                    INamedTypeSymbol entryType = (INamedTypeSymbol)attributedSymbol;
                    if (!TryConstructManagedTypeFromEntryPointTypeInformation(
                        context.ReportDiagnostic,
                        DiagnosticFactory.CreateForLocation(FindTypeExpressionLocation(FindArgumentWithArityOrName(syntax, 0, "managedType"))),
                        (ITypeSymbol?)attr.ConstructorArguments[0].Value,
                        entryType,
                        out ITypeSymbol managedType))
                    {
                        return;
                    }
                    if (!ManualTypeMarshallingHelper.TryResolveMarshallerType(entryType, (ITypeSymbol?)attr.ConstructorArguments[2].Value, context.ReportDiagnostic, out ITypeSymbol marshallerType))
                    {
                        return;
                    }

                    AnalyzeMarshallerType(
                        context.ReportDiagnostic,
                        DiagnosticFactory.CreateForLocation(FindTypeExpressionLocation(FindArgumentWithArityOrName(syntax, 2, "marshallerType"))),
                        (INamedTypeSymbol?)managedType,
                        (MarshalMode)attr.ConstructorArguments[1].Value,
                        (INamedTypeSymbol?)marshallerType,
                        ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryType));
                }
            }

            private void AnalyzeManagedTypeMarshallingInfo(
                Action<Diagnostic> reportDiagnostic,
                ITypeSymbol managedType,
                DiagnosticFactory diagnosticFactory,
                INamedTypeSymbol? entryType)
            {
                if (entryType is null)
                {
                    reportDiagnostic(
                        diagnosticFactory.CreateDiagnostic(
                            MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                            managedType.ToDisplayString()));
                    return;
                }

                if (!ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(entryType))
                {
                    reportDiagnostic(diagnosticFactory.CreateDiagnostic(
                        MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                        entryType.ToDisplayString(),
                        managedType.ToDisplayString()));
                    return;
                }

                bool isLinearCollectionMarshaller = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryType);
                if (entryType.IsUnboundGenericType)
                {
                    if (managedType is not INamedTypeSymbol namedManagedType)
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(
                                GenericMarshallerTypeMustBeClosedOrMatchArityRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                        return;
                    }
                    INamedTypeSymbol resolvedEntryType = entryType.ResolveUnboundConstructedTypeToConstructedType(namedManagedType, out int numOriginalTypeArgumentsSubstituted, out int extraTypeArgumentsInTemplate);
                    if (extraTypeArgumentsInTemplate > 0)
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(
                                GenericMarshallerTypeMustBeClosedOrMatchArityRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                        return;
                    }
                    if (!isLinearCollectionMarshaller && numOriginalTypeArgumentsSubstituted != 0)
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(
                                GenericMarshallerTypeMustBeClosedOrMatchArityRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                        return;
                    }
                    else if (isLinearCollectionMarshaller && numOriginalTypeArgumentsSubstituted != 1)
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(
                                GenericMarshallerTypeMustBeClosedOrMatchArityRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                        return;
                    }
                    entryType = resolvedEntryType;
                }

                if (isLinearCollectionMarshaller)
                {
                    if (!ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(entryType, managedType, _compilation, _ => NoMarshallingInfo.Instance, out _))
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                    }
                }
                else
                {
                    if (!ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(entryType, managedType, _compilation, out _))
                    {
                        reportDiagnostic(
                            diagnosticFactory.CreateDiagnostic(MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                                entryType.ToDisplayString(),
                                managedType.ToDisplayString()));
                    }
                }
            }

#pragma warning disable CA1822 // Mark members as static
            private void AnalyzeMarshallerType(Action<Diagnostic> reportDiagnostic, DiagnosticFactory diagnosticFactory, INamedTypeSymbol? managedType, MarshalMode mode, INamedTypeSymbol? marshallerType, bool isLinearCollectionMarshaller)
#pragma warning restore CA1822 // Mark members as static
            {
                // TODO: Implement for the V2 shapes
            }

            private static AttributeArgumentSyntax FindArgumentWithArityOrName(AttributeSyntax attribute, int arity, string name)
            {
                return attribute.ArgumentList.Arguments.FirstOrDefault(arg => arg.NameColon.Name.ToString() == name) ?? attribute.ArgumentList.Arguments[arity];
            }

            private static Location FindTypeExpressionLocation(AttributeArgumentSyntax attributeArgumentSyntax)
            {
                var walker = new FindTypeLocationWalker();
                walker.Visit(attributeArgumentSyntax);
                return walker.TypeExpressionLocation;
            }

            private class FindTypeLocationWalker : CSharpSyntaxWalker
            {
                public Location? TypeExpressionLocation { get; private set; }

                public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
                {
                    TypeExpressionLocation = node.Type.GetLocation();
                }
            }

            private static bool TryConstructManagedTypeFromEntryPointTypeInformation(
                Action<Diagnostic> reportDiagnostic,
                DiagnosticFactory diagnosticFactory,
                ITypeSymbol? typeFromAttribute,
                INamedTypeSymbol entryPointType,
                [NotNullWhen(true)] out ITypeSymbol? managedType)
            {
                managedType = null;
                if (typeFromAttribute is null)
                {
                    reportDiagnostic(diagnosticFactory.CreateDiagnostic(MarshallerTypeMustSpecifyManagedTypeRule, entryPointType.ToDisplayString()));
                    return false;
                }
                if (typeFromAttribute is not INamedTypeSymbol { TypeParameters.Length: > 0, IsUnboundGenericType: true } genericManagedType)
                {
                    managedType = typeFromAttribute;
                    return true;
                }
                int expectedArity = genericManagedType.TypeArguments.Length;
                bool isLinearCollectionEntryPoint = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointType);
                if (isLinearCollectionEntryPoint)
                {
                    expectedArity++;
                }
                if (entryPointType.TypeParameters.Length != expectedArity)
                {
                    reportDiagnostic(diagnosticFactory.CreateDiagnostic(GenericMarshallerTypeMustBeClosedOrMatchArityRule, entryPointType.ToDisplayString(), managedType.ToDisplayString()));
                    return false;
                }
                ImmutableArray<ITypeSymbol> genericArguments = entryPointType.TypeArguments;
                ImmutableArray<NullableAnnotation> genericArgumentNullableAnnotations = entryPointType.TypeArgumentNullableAnnotations;
                if (isLinearCollectionEntryPoint)
                {
                    genericArguments = genericArguments.RemoveAt(genericArguments.Length - 1);
                    genericArgumentNullableAnnotations = genericArgumentNullableAnnotations.RemoveAt(genericArgumentNullableAnnotations.Length - 1);
                }
                managedType = genericManagedType.Construct(genericArguments, genericArgumentNullableAnnotations);
                return true;
            }

            private static AttributeData GetAttributeData(AttributeSyntax syntax, ISymbol symbol)
            {
                if (syntax.FirstAncestorOrSelf<AttributeListSyntax>().Target?.Identifier.IsKind(SyntaxKind.ReturnKeyword) == true)
                {
                    return ((IMethodSymbol)symbol).GetReturnTypeAttributes().First(attributeSyntaxLocationMatches);
                }
                return symbol.GetAttributes().First(attributeSyntaxLocationMatches);

                bool attributeSyntaxLocationMatches(AttributeData attrData)
                {
                    return attrData.ApplicationSyntaxReference!.SyntaxTree == syntax.SyntaxTree && attrData.ApplicationSyntaxReference.Span == syntax.Span;
                }
            }

            private static ITypeSymbol GetSymbolType(ISymbol symbol)
            {
                return symbol switch
                {
                    IMethodSymbol method => method.ReturnType,
                    IParameterSymbol param => param.Type,
                    IFieldSymbol field => field.Type,
                    _ => throw new InvalidOperationException()
                };
            }

            private struct DiagnosticFactory
            {
                private readonly Func<DiagnosticDescriptor, ImmutableDictionary<string, string>, object[], Diagnostic> _diagnosticFactory;

                public DiagnosticFactory(Func<DiagnosticDescriptor, ImmutableDictionary<string, string>, object[], Diagnostic> diagnosticFactory)
                {
                    _diagnosticFactory = diagnosticFactory;
                }

                public static DiagnosticFactory CreateForLocation(Location location) => new(location.CreateDiagnostic);

                public Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs) => _diagnosticFactory(descriptor, ImmutableDictionary<string, string>.Empty, messageArgs);
            }
        }
    }
}
