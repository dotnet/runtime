// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComInterfaceGeneratorDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                // Interface-level diagnostics
                GeneratorDiagnostics.RequiresAllowUnsafeBlocks,
                GeneratorDiagnostics.InvalidAttributedInterfaceGenericNotSupported,
                GeneratorDiagnostics.InvalidAttributedInterfaceMissingPartialModifiers,
                GeneratorDiagnostics.InvalidAttributedInterfaceNotAccessible,
                GeneratorDiagnostics.InvalidAttributedInterfaceMissingGuidAttribute,
                GeneratorDiagnostics.InvalidStringMarshallingMismatchBetweenBaseAndDerived,
                GeneratorDiagnostics.InvalidOptionsOnInterface,
                GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface,
                GeneratorDiagnostics.InvalidExceptionToUnmanagedMarshallerType,
                GeneratorDiagnostics.StringMarshallingCustomTypeNotAccessibleByGeneratedCode,
                GeneratorDiagnostics.ExceptionToUnmanagedMarshallerNotAccessibleByGeneratedCode,
                GeneratorDiagnostics.MultipleComInterfaceBaseTypes,
                GeneratorDiagnostics.BaseInterfaceIsNotGenerated,
                GeneratorDiagnostics.BaseInterfaceDefinedInOtherAssembly,
                // Method-level diagnostics
                GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface,
                GeneratorDiagnostics.InstancePropertyDeclaredInInterface,
                GeneratorDiagnostics.InstanceEventDeclaredInInterface,
                GeneratorDiagnostics.CannotAnalyzeMethodPattern,
                GeneratorDiagnostics.CannotAnalyzeInterfacePattern,
                // Stub-level diagnostics
                GeneratorDiagnostics.ConfigurationNotSupported,
                GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnMethod,
                GeneratorDiagnostics.ParameterTypeNotSupported,
                GeneratorDiagnostics.ReturnTypeNotSupported,
                GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                GeneratorDiagnostics.ParameterConfigurationNotSupported,
                GeneratorDiagnostics.ReturnConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsParameterConfigurationNotSupported,
                GeneratorDiagnostics.MarshalAsReturnConfigurationNotSupported,
                GeneratorDiagnostics.ConfigurationValueNotSupported,
                GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo,
                GeneratorDiagnostics.UnnecessaryReturnMarshallingInfo,
                GeneratorDiagnostics.ComMethodManagedReturnWillBeOutVariable,
                GeneratorDiagnostics.HResultTypeWillBeTreatedAsStruct,
                GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallOutParam,
                GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallReturnValue,
                GeneratorDiagnostics.InvalidExceptionMarshallingConfiguration,
                GeneratorDiagnostics.GeneratedComInterfaceUsageDoesNotFollowBestPractices);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? generatedComInterfaceAttrType = compilationContext.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);
                if (generatedComInterfaceAttrType is null)
                    return;

                StubEnvironment env = new StubEnvironment(
                    compilationContext.Compilation,
                    compilationContext.Compilation.GetEnvironmentFlags());

                var interfaceSymbols = new ConcurrentBag<(InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)>();

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    INamedTypeSymbol typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;
                    if (typeSymbol.TypeKind != TypeKind.Interface)
                        return;

                    foreach (AttributeData attr in typeSymbol.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedComInterfaceAttrType))
                        {
                            // Use the syntax reference that contains the attribute application
                            // (important for partial types where the attribute is on a specific partial declaration)
                            var attrSyntaxRef = attr.ApplicationSyntaxReference;
                            if (attrSyntaxRef is not null)
                            {
                                var attrSyntax = attrSyntaxRef.GetSyntax(symbolContext.CancellationToken);
                                if (attrSyntax.Parent?.Parent is InterfaceDeclarationSyntax ifaceSyntax)
                                {
                                    interfaceSymbols.Add((ifaceSyntax, typeSymbol));
                                }
                            }
                            else
                            {
                                foreach (SyntaxReference syntaxRef in typeSymbol.DeclaringSyntaxReferences)
                                {
                                    if (syntaxRef.GetSyntax(symbolContext.CancellationToken) is InterfaceDeclarationSyntax ifaceSyntax)
                                    {
                                        interfaceSymbols.Add((ifaceSyntax, typeSymbol));
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }, SymbolKind.NamedType);

                compilationContext.RegisterCompilationEndAction(endContext =>
                {
                    if (interfaceSymbols.IsEmpty)
                        return;

                    AnalyzeInterfaces(endContext, env, interfaceSymbols.ToImmutableArray());
                });
            });
        }

        private static void AnalyzeInterfaces(
            CompilationAnalysisContext context,
            StubEnvironment env,
            ImmutableArray<(InterfaceDeclarationSyntax Syntax, INamedTypeSymbol Symbol)> attributedInterfaces)
        {
            CancellationToken ct = context.CancellationToken;

            // This mirrors the analysis phase of ComInterfaceGenerator.Initialize.
            List<(ComInterfaceInfo, INamedTypeSymbol)> interfaceInfos = new();
            HashSet<(ComInterfaceInfo, INamedTypeSymbol)> externalIfaces = new(ComInterfaceInfo.EqualityComparerForExternalIfaces.Instance);
            List<DiagnosticInfo> diags = new();

            foreach (var (syntax, symbol) in attributedInterfaces)
            {
                var cii = ComInterfaceInfo.From(symbol, syntax, env, ct);
                if (cii.HasDiagnostic)
                {
                    foreach (var diag in cii.Diagnostics)
                        diags.Add(diag);
                }
                if (cii.HasValue)
                    interfaceInfos.Add(cii.Value);

                var externalBase = ComInterfaceInfo.CreateInterfaceInfoForBaseInterfacesInOtherCompilations(symbol);
                if (!externalBase.IsDefaultOrEmpty)
                {
                    foreach (var b in externalBase)
                        externalIfaces.Add(b);
                }
            }

            interfaceInfos.AddRange(externalIfaces);

            var comInterfaceContexts = ComInterfaceContext.GetContexts(interfaceInfos.Select(i => i.Item1).ToImmutableArray(), ct);

            Dictionary<ComMethodInfo, IMethodSymbol> methodSymbols = new();
            List<List<ComMethodInfo>> methods = new();

            foreach (var cii in interfaceInfos)
            {
                var cmi = ComMethodInfo.GetMethodsFromInterface(cii, ct);
                var inner = new List<ComMethodInfo>();
                foreach (var m in cmi)
                {
                    if (m.HasDiagnostic)
                    {
                        foreach (var diag in m.Diagnostics)
                            diags.Add(diag);
                    }
                    if (m.HasValue)
                    {
                        inner.Add(m.Value.ComMethod);
                        methodSymbols.Add(m.Value.ComMethod, m.Value.Symbol);
                    }
                }
                methods.Add(inner);
            }

            List<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>)> ifaceCtxs = new();
            for (int i = 0; i < interfaceInfos.Count; i++)
            {
                var cic = comInterfaceContexts[i];
                if (cic.HasDiagnostic)
                {
                    foreach (var diag in cic.Diagnostics)
                        diags.Add(diag);
                }
                if (cic.HasValue)
                {
                    ifaceCtxs.Add((cic.Value, methods[i].ToSequenceEqualImmutableArray()));
                }
            }

            // Report interface-level and method-level diagnostics
            foreach (var diag in diags)
                context.ReportDiagnostic(diag.ToDiagnostic());

            var result = ComMethodContext.CalculateAllMethods(ifaceCtxs, ct);

            List<ComMethodContext> methodContexts = new();
            foreach (var data in result)
            {
                methodContexts.Add(new ComMethodContext(
                    data.Method,
                    data.OwningInterface,
                    ComInterfaceGenerator.CalculateStubInformation(
                        data.Method.MethodInfo.Syntax,
                        methodSymbols[data.Method.MethodInfo],
                        data.Method.Index,
                        env,
                        data.OwningInterface.Info,
                        ct)));
            }

            // Group method contexts by owning interface to match the generator's GroupComContextsForInterfaceGeneration
            // and only report diagnostics for declared (non-inherited) methods.
            var groupedByOwningInterface = methodContexts
                .GroupBy(m => m.OwningInterface);

            foreach (var group in groupedByOwningInterface)
            {
                var declaredMethods = group.Where(static m => !m.IsInheritedMethod).ToList();

                // Report diagnostics for managed-to-unmanaged and unmanaged-to-managed stubs,
                // deduplicating diagnostics that are reported for both (matching the generator behavior).
                var allStubDiags = declaredMethods
                    .SelectMany(m => m.ManagedToUnmanagedStub.Diagnostics.Array)
                    .Union(declaredMethods.SelectMany(m => m.UnmanagedToManagedStub.Diagnostics.Array));

                foreach (var diag in allStubDiags)
                    context.ReportDiagnostic(diag.ToDiagnostic());
            }
        }
    }
}
