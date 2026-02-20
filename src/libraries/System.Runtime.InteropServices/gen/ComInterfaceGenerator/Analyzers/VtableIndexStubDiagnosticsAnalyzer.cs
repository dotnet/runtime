// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VtableIndexStubDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                GeneratorDiagnostics.InvalidAttributedMethodSignature,
                GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers,
                GeneratorDiagnostics.ReturnConfigurationNotSupported,
                GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute,
                GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnMethod,
                GeneratorDiagnostics.InvalidExceptionMarshallingConfiguration,
                GeneratorDiagnostics.ConfigurationNotSupported,
                GeneratorDiagnostics.ParameterTypeNotSupported,
                GeneratorDiagnostics.ReturnTypeNotSupported,
                GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ParameterConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsParameterConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsReturnConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationValueNotSupported,
                GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo,
                GeneratorDiagnostics.UnnecessaryReturnMarshallingInfo,
                GeneratorDiagnostics.GeneratedComInterfaceUsageDoesNotFollowBestPractices);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? virtualMethodIndexAttrType = compilationContext.Compilation.GetBestTypeByMetadataName(TypeNames.VirtualMethodIndexAttribute);
                if (virtualMethodIndexAttrType is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    compilationContext.Compilation,
                    compilationContext.Compilation.GetEnvironmentFlags());

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    IMethodSymbol method = (IMethodSymbol)symbolContext.Symbol;
                    AttributeData? virtualMethodIndexAttr = null;
                    foreach (AttributeData attr in method.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, virtualMethodIndexAttrType))
                        {
                            virtualMethodIndexAttr = attr;
                            break;
                        }
                    }

                    if (virtualMethodIndexAttr is null)
                        return;

                    foreach (SyntaxReference syntaxRef in method.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(symbolContext.CancellationToken) is MethodDeclarationSyntax methodSyntax)
                        {
                            AnalyzeMethod(symbolContext, methodSyntax, method, env);
                            break;
                        }
                    }
                }, SymbolKind.Method);
            });
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context, MethodDeclarationSyntax methodSyntax, IMethodSymbol method, StubEnvironment env)
        {
            DiagnosticInfo? invalidMethodDiagnostic = GetDiagnosticIfInvalidMethodForGeneration(methodSyntax, method);
            if (invalidMethodDiagnostic is not null)
            {
                context.ReportDiagnostic(invalidMethodDiagnostic.ToDiagnostic());
                return;
            }

            SourceAvailableIncrementalMethodStubGenerationContext stubContext = VtableIndexStubGenerator.CalculateStubInformation(methodSyntax, method, env, context.CancellationToken);

            if (stubContext.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
            {
                var (_, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(stubContext, VtableIndexStubGeneratorHelpers.GetGeneratorResolver);
                foreach (DiagnosticInfo diag in diagnostics)
                    context.ReportDiagnostic(diag.ToDiagnostic());
            }

            if (stubContext.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
            {
                var (_, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(stubContext, VtableIndexStubGeneratorHelpers.GetGeneratorResolver);
                foreach (DiagnosticInfo diag in diagnostics)
                    context.ReportDiagnostic(diag.ToDiagnostic());
            }
        }

        internal static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || methodSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            // Verify there is an [UnmanagedObjectUnwrapperAttribute<TMapper>]
            if (!method.ContainingType.GetAttributes().Any(att => att.AttributeClass.IsOfType(TypeNames.UnmanagedObjectUnwrapperAttribute)))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            return null;
        }
    }
}
