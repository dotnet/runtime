// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        private const string RegexName = "System.Text.RegularExpressions.Regex";
        private const string RegexGeneratorAttributeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
            node is MethodDeclarationSyntax { AttributeLists: { Count: > 0 } };

        private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

            foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol &&
                        attributeSymbol.ContainingType.ToDisplayString() == RegexGeneratorAttributeName)
                    {
                        return methodDeclarationSyntax.Parent as TypeDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        private static IReadOnlyList<RegexType> GetRegexTypesToEmit(Compilation compilation, Action<Diagnostic> reportDiagnostic, IEnumerable<TypeDeclarationSyntax> classes, CancellationToken cancellationToken)
        {
            // TODO: Use https://github.com/dotnet/runtime/pull/59092
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexName);
            INamedTypeSymbol? regexGeneratorAttributeSymbol = compilation.GetTypeByMetadataName(RegexGeneratorAttributeName);
            if (regexSymbol is null || regexGeneratorAttributeSymbol is null)
            {
                // Required types aren't available
                return Array.Empty<RegexType>();
            }

            var results = new List<RegexType>();

            // Enumerate by SyntaxTree to minimize the need to instantiate semantic models (since they're expensive)
            foreach (var group in classes.GroupBy(x => x.SyntaxTree))
            {
                SemanticModel? sm = null;
                foreach (TypeDeclarationSyntax typeDec in group)
                {
                    foreach (MemberDeclarationSyntax member in typeDec.Members)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Scope to just methods
                        if (member is not MethodDeclarationSyntax methodSyntax)
                        {
                            continue;
                        }

                        sm ??= compilation.GetSemanticModel(typeDec.SyntaxTree);

                        IMethodSymbol regexMethodSymbol = sm.GetDeclaredSymbol(methodSyntax, cancellationToken) as IMethodSymbol;
                        if (regexMethodSymbol is null)
                        {
                            continue;
                        }

                        ImmutableArray<AttributeData>? boundAttributes = regexMethodSymbol.GetAttributes();
                        if (boundAttributes is null || boundAttributes.Value.Length == 0)
                        {
                            continue;
                        }

                        DiagnosticDescriptor? errorDescriptor = null;
                        RegexMethod? regexMethod = null;
                        foreach (AttributeData attributeData in boundAttributes)
                        {
                            // If we already encountered an error, stop looking at this method's attributes.
                            if (errorDescriptor is not null)
                            {
                                break;
                            }

                            // If this isn't 
                            if (!attributeData.AttributeClass.Equals(regexGeneratorAttributeSymbol))
                            {
                                continue;
                            }

                            if (attributeData.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
                            {
                                errorDescriptor = DiagnosticDescriptors.InvalidRegexGeneratorAttribute;
                                break;
                            }

                            ImmutableArray<TypedConstant> items = attributeData.ConstructorArguments;
                            if (items.Length is > 0 and <= 3 && items[0].Value is string pattern)
                            {
                                switch (items.Length)
                                {
                                    case 1:
                                        regexMethod = new RegexMethod { Pattern = pattern };
                                        break;

                                    case 2:
                                        regexMethod = new RegexMethod { Pattern = pattern, Options = items[1].Value as int?, };
                                        break;

                                    case 3:
                                        regexMethod = new RegexMethod { Pattern = pattern, Options = items[1].Value as int?, MatchTimeout = items[2].Value as int?, };
                                        break;
                                }
                            }
                            else
                            {
                                errorDescriptor = DiagnosticDescriptors.InvalidRegexGeneratorAttribute;
                            }
                        }

                        if (errorDescriptor is not null)
                        {
                            Diag(reportDiagnostic, errorDescriptor, methodSyntax.GetLocation());
                            continue;
                        }

                        if (regexMethod is null)
                        {
                            continue;
                        }

                        if (regexMethod.Pattern is null)
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "(null)");
                            continue;
                        }

                        if (!regexMethodSymbol.IsPartialDefinition ||
                            !regexMethodSymbol.IsStatic ||
                            regexMethodSymbol.Parameters.Length != 0 ||
                            regexMethodSymbol.Arity != 0 ||
                            !regexMethodSymbol.ReturnType.Equals(regexSymbol))
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.RegexMethodMustHaveValidSignature, methodSyntax.GetLocation());
                            continue;
                        }

                        if (typeDec.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: < LanguageVersion.CSharp10 })
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.InvalidLangVersion, methodSyntax.GetLocation());
                            continue;
                        }

                        regexMethod.MethodName = regexMethodSymbol.Name;
                        regexMethod.Modifiers = methodSyntax.Modifiers.ToString();
                        regexMethod.MatchTimeout ??= Timeout.Infinite;
                        RegexOptions options = regexMethod.Options.HasValue ? (RegexOptions)regexMethod.Options.Value : RegexOptions.None;
                        regexMethod.Options = (int)RegexOptions.Compiled | (int)options;

                        // TODO: This is going to include the culture that's current at the time of compilation.
                        // What should we do about that?  We could:
                        // - say not specifying CultureInvariant is invalid if anything about options or the expression will look at culture
                        // - fall back to not generating source if it's not specified
                        // - just use whatever culture is present at build time
                        // - devise a new way of not using the culture present at build time
                        // - ...
                        CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

                        // Validate the options
                        const RegexOptions SupportedOptions =
                            RegexOptions.IgnoreCase |
                            RegexOptions.Multiline |
                            RegexOptions.ExplicitCapture |
                            RegexOptions.Compiled |
                            RegexOptions.Singleline |
                            RegexOptions.IgnorePatternWhitespace |
                            RegexOptions.RightToLeft |
                            RegexOptions.ECMAScript |
                            RegexOptions.CultureInvariant;
                        if ((regexMethod.Options.Value & ~(int)SupportedOptions) != 0)
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "options");
                            continue;
                        }

                        // Validate the timeout
                        if (regexMethod.MatchTimeout.Value is 0 or < -1)
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "matchTimeout");
                            continue;
                        }

                        // Parse the input pattern
                        try
                        {
                            regexMethod.Tree = RegexParser.Parse(regexMethod.Pattern, (RegexOptions)regexMethod.Options, culture);
                            regexMethod.Code = RegexWriter.Write(regexMethod.Tree);
                        }
                        catch (Exception e)
                        {
                            Diag(reportDiagnostic, DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), e.Message);
                            continue;
                        }

                        // Determine the namespace the class is declared in, if any
                        string? ns = null;
                        SyntaxNode? potentialNamespaceParent = typeDec.Parent;
                        while (potentialNamespaceParent is not null &&
                               potentialNamespaceParent is not NamespaceDeclarationSyntax &&
                               potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
                        {
                            potentialNamespaceParent = potentialNamespaceParent.Parent;
                        }

                        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
                        {
                            ns = namespaceParent.Name.ToString();
                            while (true)
                            {
                                namespaceParent = namespaceParent.Parent as NamespaceDeclarationSyntax;
                                if (namespaceParent is null)
                                {
                                    break;
                                }

                                ns = $"{namespaceParent.Name}.{ns}";
                            }
                        }

                        var rc = new RegexType
                        {
                            Keyword = typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                            Namespace = ns,
                            Name = $"{typeDec.Identifier}{typeDec.TypeParameterList}",
                            Constraints = typeDec.ConstraintClauses.ToString(),
                            ParentClass = null,
                            Method = regexMethod,
                        };

                        RegexType current = rc;
                        var parent = typeDec.Parent as TypeDeclarationSyntax;

                        while (parent is not null && IsAllowedKind(parent.Kind()))
                        {
                            current.ParentClass = new RegexType
                            {
                                Keyword = parent is RecordDeclarationSyntax rds2 ? $"{parent.Keyword.ValueText} {rds2.ClassOrStructKeyword}" : parent.Keyword.ValueText,
                                Namespace = ns,
                                Name = $"{parent.Identifier}{parent.TypeParameterList}",
                                Constraints = parent.ConstraintClauses.ToString(),
                                ParentClass = null,
                            };

                            current = current.ParentClass;
                            parent = parent.Parent as TypeDeclarationSyntax;
                        }

                        results.Add(rc);

                        bool IsAllowedKind(SyntaxKind kind) =>
                            kind == SyntaxKind.ClassDeclaration ||
                            kind == SyntaxKind.StructDeclaration ||
                            kind == SyntaxKind.RecordDeclaration ||
                            kind == SyntaxKind.RecordStructDeclaration;
                    }
                }
            }

            return results;

            static void Diag(Action<Diagnostic> reportDiagnostic, DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs) =>
                reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
        }

        /// <summary>A type holding a regex method.</summary>
        internal sealed class RegexType
        {
            public RegexMethod Method;
            public string Keyword = string.Empty;
            public string Namespace = string.Empty;
            public string Name = string.Empty;
            public string Constraints = string.Empty;
            public RegexType? ParentClass;
        }

        /// <summary>A regex method.</summary>
        internal sealed class RegexMethod
        {
            public string MethodName = string.Empty;
            public string Pattern = string.Empty;
            public int? Options;
            public int? MatchTimeout;
            public string Modifiers = string.Empty;
            public RegexTree Tree;
            public RegexCode Code;
        }
    }
}
