// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
    {
        public const string RequiresUnreferencedCodeAttribute = nameof(RequiresUnreferencedCodeAttribute);
        public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

        private static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnreferencedCode);
        private static readonly DiagnosticDescriptor s_requiresUnreferencedCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnreferencedCodeAttributeMismatch);
        private static readonly DiagnosticDescriptor s_makeGenericTypeRule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.MakeGenericType);
        private static readonly DiagnosticDescriptor s_makeGenericMethodRule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.MakeGenericMethod);
        private static readonly DiagnosticDescriptor s_requiresUnreferencedCodeOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnreferencedCodeOnStaticConstructor);
        private static readonly DiagnosticDescriptor s_requiresUnreferencedCodeOnEntryPoint = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnreferencedCodeOnEntryPoint);
        private static readonly DiagnosticDescriptor s_debuggerDisplayReferencesRequiresUnreferencedCodeMember = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.DebuggerDisplayReferencesRequiresUnreferencedCodeMember);

        private static readonly DiagnosticDescriptor s_referenceNotMarkedIsTrimmableRule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.ReferenceNotMarkedIsTrimmable);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(s_makeGenericMethodRule, s_makeGenericTypeRule, s_requiresUnreferencedCodeRule, s_requiresUnreferencedCodeAttributeMismatch, s_requiresUnreferencedCodeOnStaticCtor, s_requiresUnreferencedCodeOnEntryPoint, s_debuggerDisplayReferencesRequiresUnreferencedCodeMember, s_referenceNotMarkedIsTrimmableRule);

        private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

        internal override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

        private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

        private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

        private protected override DiagnosticId RequiresDiagnosticId => DiagnosticId.RequiresUnreferencedCode;

        private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnreferencedCodeAttributeMismatch;

        private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresUnreferencedCodeOnStaticCtor;

        private protected override DiagnosticDescriptor RequiresOnEntryPoint => s_requiresUnreferencedCodeOnEntryPoint;

        internal override bool IsAnalyzerEnabled(AnalyzerOptions options) =>
            options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableTrimAnalyzer);

        private protected override bool IsRequiresCheck(IPropertySymbol propertySymbol, Compilation compilation)
        {
            // "IsUnreferencedCodeSupported" is treated as a requires check for testing purposes only, and
            // is not officially-supported product behavior.
            var runtimeFeaturesType = compilation.GetTypeByMetadataName("ILLink.RoslynAnalyzer.TestFeatures");
            if (runtimeFeaturesType == null)
                return false;

            var isDynamicCodeSupportedProperty = runtimeFeaturesType.GetMembers("IsUnreferencedCodeSupported").OfType<IPropertySymbol>().FirstOrDefault();
            if (isDynamicCodeSupportedProperty == null)
                return false;

            return SymbolEqualityComparer.Default.Equals(propertySymbol, isDynamicCodeSupportedProperty);
        }

        protected override bool CreateSpecialIncompatibleMembersDiagnostic(
            ImmutableArray<ISymbol> specialIncompatibleMembers,
            ISymbol member,
            in DiagnosticContext diagnosticContext)
        {
            // Some RUC-annotated APIs are intrinsically handled by the trimmer
            if (member is IMethodSymbol method && Intrinsics.GetIntrinsicIdForMethod(new MethodProxy(method)) != IntrinsicId.None)
            {
                return true;
            }

            return false;
        }

        private protected override ImmutableArray<Action<CompilationAnalysisContext>> ExtraCompilationActions =>
            ImmutableArray.Create<Action<CompilationAnalysisContext>>((context) =>
            {
                CheckReferencedAssemblies(
                    context,
                    MSBuildPropertyOptionNames.VerifyReferenceTrimCompatibility,
                    "IsTrimmable",
                    s_referenceNotMarkedIsTrimmableRule);
            });

        private protected override ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)> ExtraSymbolActions =>
            ImmutableArray.Create<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)>(
                (AnalyzeTypeForDebuggerDisplay, new[] { SymbolKind.NamedType })
            );

        private void AnalyzeTypeForDebuggerDisplay(SymbolAnalysisContext context)
        {
            var typeSymbol = (INamedTypeSymbol)context.Symbol;

            // Check for DebuggerDisplay attributes on the type
            foreach (var attribute in typeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass is not INamedTypeSymbol attributeClass)
                    continue;

                if (attributeClass.Name != "DebuggerDisplayAttribute" ||
                    attributeClass.ContainingNamespace?.ToDisplayString() != "System.Diagnostics")
                    continue;

                AnalyzeDebuggerDisplayAttribute(context, typeSymbol, attribute);
            }
        }

        private void AnalyzeDebuggerDisplayAttribute(
            SymbolAnalysisContext context,
            INamedTypeSymbol typeSymbol,
            AttributeData attribute)
        {
            // Extract the format string from the attribute constructor argument
            string? formatString = null;
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorArg)
            {
                formatString = ctorArg;
            }

            AnalyzeDebuggerDisplayString(context, typeSymbol, attribute, formatString);

            // Also check the Name and Type properties
            foreach (var namedArg in attribute.NamedArguments)
            {
                if ((namedArg.Key is "Name" or "Type") && namedArg.Value.Value is string propertyValue)
                {
                    AnalyzeDebuggerDisplayString(context, typeSymbol, attribute, propertyValue);
                }
            }
        }

        private void AnalyzeDebuggerDisplayString(
            SymbolAnalysisContext context,
            INamedTypeSymbol typeSymbol,
            AttributeData attribute,
            string? displayString)
        {
            if (!DebuggerDisplayAttributeHelper.TryParseMemberReferences(displayString, out var memberNames))
            {
                // If we can't fully understand the DebuggerDisplay string, we don't warn.
                // This matches the ILLinker behavior to avoid false positives.
                return;
            }

            foreach (var memberName in memberNames)
            {
                // Try to find the member on the type
                ISymbol? member = null;

                // Check for method with no parameters (methods are referenced with "()" in DebuggerDisplay)
                var methods = typeSymbol.GetMembers(memberName).OfType<IMethodSymbol>()
                    .Where(m => m.Parameters.Length == 0 && m.MethodKind == MethodKind.Ordinary);
                member = methods.FirstOrDefault();

                // If not a method, check for field or property
                member ??= typeSymbol.GetMembers(memberName).FirstOrDefault(m =>
                        m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property);

                if (member == null)
                    continue;

                // Check if the member or its accessors have RequiresUnreferencedCode
                if (member.HasAttribute(RequiresAttributeName))
                {
                    ReportDebuggerDisplayReferencesRUCMember(context, typeSymbol, member, attribute);
                }
                else if (member is IPropertySymbol property)
                {
                    // Check property accessors
                    if (property.GetMethod?.HasAttribute(RequiresAttributeName) == true)
                    {
                        ReportDebuggerDisplayReferencesRUCMember(context, typeSymbol, property.GetMethod, attribute);
                    }
                    if (property.SetMethod?.HasAttribute(RequiresAttributeName) == true)
                    {
                        ReportDebuggerDisplayReferencesRUCMember(context, typeSymbol, property.SetMethod, attribute);
                    }
                }
            }
        }

        private void ReportDebuggerDisplayReferencesRUCMember(
            SymbolAnalysisContext context,
            INamedTypeSymbol typeSymbol,
            ISymbol referencedMember,
            AttributeData attribute)
        {
            if (!referencedMember.TryGetAttribute(RequiresAttributeName, out var attributeData))
                return;

            var message = GetMessageFromAttribute(attributeData);
            var url = GetUrlFromAttribute(attributeData);

            context.ReportDiagnostic(Diagnostic.Create(
                s_debuggerDisplayReferencesRequiresUnreferencedCodeMember,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.ToDisplayString(),
                referencedMember.ToDisplayString(),
                message,
                url));
        }

        protected override bool VerifyAttributeArguments(AttributeData attribute) =>
            RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments(attribute);

        protected override string GetMessageFromAttribute(AttributeData? requiresAttribute) =>
            RequiresUnreferencedCodeUtils.GetMessageFromAttribute(requiresAttribute);
    }
}
