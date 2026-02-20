// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.JavaScript
{
    /// <summary>
    /// Abstract base analyzer for JSImport and JSExport diagnostics.
    /// Derived classes specialize for each attribute type.
    /// </summary>
    public abstract class JSInteropDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The metadata name of the attribute this analyzer handles.</summary>
        protected abstract string AttributeMetadataName { get; }

        /// <summary>Descriptor for an invalid method signature for this attribute type.</summary>
        protected abstract DiagnosticDescriptor InvalidSignatureDescriptor { get; }

        /// <summary>Descriptor when the containing type is missing partial modifiers.</summary>
        protected abstract DiagnosticDescriptor ContainingTypeMissingModifiersDescriptor { get; }

        /// <summary>Descriptor reported when AllowUnsafeBlocks is not enabled.</summary>
        protected abstract DiagnosticDescriptor RequiresAllowUnsafeBlocksDescriptor { get; }

        /// <summary>
        /// When <see langword="true"/> (JSExport), the method must have a body and must not be partial.
        /// When <see langword="false"/> (JSImport), the method must not have a body and must be partial.
        /// </summary>
        protected abstract bool RequiresImplementation { get; }

        /// <summary>
        /// Calculate marshalling-related diagnostics for a validated method.
        /// </summary>
        protected abstract ImmutableArray<DiagnosticInfo> CalculateDiagnostics(
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol method,
            AttributeData attr,
            StubEnvironment environment,
            System.Threading.CancellationToken ct);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? attrType = context.Compilation.GetTypeByMetadataName(AttributeMetadataName);
                if (attrType is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    context.Compilation,
                    context.Compilation.GetEnvironmentFlags());

                int foundMethod = 0;
                bool unsafeEnabled = context.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true };

                context.RegisterSymbolAction(symbolContext =>
                {
                    IMethodSymbol method = (IMethodSymbol)symbolContext.Symbol;
                    foreach (AttributeData attr in method.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
                        {
                            if (AnalyzeMethod(symbolContext, method, attr, env))
                            {
                                Interlocked.Exchange(ref foundMethod, 1);
                            }
                            break;
                        }
                    }
                }, SymbolKind.Method);

                context.RegisterCompilationEndAction(endContext =>
                {
                    if (!unsafeEnabled && Volatile.Read(ref foundMethod) != 0)
                    {
                        endContext.ReportDiagnostic(DiagnosticInfo.Create(RequiresAllowUnsafeBlocksDescriptor, null).ToDiagnostic());
                    }
                });
            });
        }

        private bool AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol method, AttributeData attr, StubEnvironment env)
        {
            foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                {
                    DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidMethodForGeneration(
                        methodSyntax, method,
                        InvalidSignatureDescriptor,
                        ContainingTypeMissingModifiersDescriptor,
                        RequiresImplementation);
                    if (invalidMethodDiagnostic is not null)
                    {
                        context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                        return true;
                    }

                    foreach (DiagnosticInfo diagnostic in CalculateDiagnostics(methodSyntax, method, attr, env, context.CancellationToken))
                    {
                        context.ReportDiagnostic(diagnostic.ToDiagnostic());
                    }
                    break;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if a JSImport or JSExport method is invalid for generation and returns a diagnostic if so.
        /// </summary>
        /// <param name="invalidSignatureDescriptor">Descriptor for an invalid method signature.</param>
        /// <param name="containingTypeMissingModifiersDescriptor">Descriptor for a containing type missing modifiers.</param>
        /// <param name="requiresImplementation">
        /// When <see langword="true"/> (JSExport), the method must have a body and must not be partial.
        /// When <see langword="false"/> (JSImport), the method must not have a body and must be partial.
        /// </param>
        /// <returns>A diagnostic if the method is invalid, null otherwise.</returns>
        internal static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol method,
            DiagnosticDescriptor invalidSignatureDescriptor,
            DiagnosticDescriptor containingTypeMissingModifiersDescriptor,
            bool requiresImplementation)
        {
            bool hasImplementation = methodSyntax.Body is not null || methodSyntax.ExpressionBody is not null;
            bool isPartial = methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

            // requiresImplementation=false (JSImport): must have no body, must be partial.
            // requiresImplementation=true (JSExport): must have a body, must not be partial.
            if (methodSyntax.TypeParameterList is not null
                || hasImplementation != requiresImplementation
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || isPartial == requiresImplementation)
            {
                return DiagnosticInfo.Create(invalidSignatureDescriptor, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return DiagnosticInfo.Create(containingTypeMissingModifiersDescriptor, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }
    }
}
