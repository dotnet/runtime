// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

                // Cache ComInterfaceInfo per symbol for deduplication when multiple interfaces share the same base.
                // This avoids recomputing the same interface info when traversing the ancestor chain of different derived interfaces.
                var interfaceInfoCache = new ConcurrentDictionary<INamedTypeSymbol, DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)>>(SymbolEqualityComparer.Default);

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    INamedTypeSymbol typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;
                    if (typeSymbol.TypeKind != TypeKind.Interface)
                        return;

                    // Find the [GeneratedComInterface] attribute and the syntax node of the declaring partial interface
                    InterfaceDeclarationSyntax? ifaceSyntax = null;
                    foreach (AttributeData attr in typeSymbol.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedComInterfaceAttrType))
                        {
                            ifaceSyntax = FindInterfaceSyntaxWithAttribute(typeSymbol, generatedComInterfaceAttrType, symbolContext.CancellationToken);
                            break;
                        }
                    }

                    if (ifaceSyntax is null)
                        return;

                    AnalyzeInterface(symbolContext, typeSymbol, ifaceSyntax, env, generatedComInterfaceAttrType, interfaceInfoCache);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeInterface(
            SymbolAnalysisContext context,
            INamedTypeSymbol typeSymbol,
            InterfaceDeclarationSyntax ifaceSyntax,
            StubEnvironment env,
            INamedTypeSymbol generatedComInterfaceAttrType,
            ConcurrentDictionary<INamedTypeSymbol, DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)>> interfaceInfoCache)
        {
            CancellationToken ct = context.CancellationToken;

            // Get or compute ComInterfaceInfo for this interface (cached to avoid recomputing for shared base interfaces)
            DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)> ciiResult = interfaceInfoCache.GetOrAdd(
                typeSymbol, _ => ComInterfaceInfo.From(typeSymbol, ifaceSyntax, env, ct));

            // Report interface-level diagnostics
            if (ciiResult.HasDiagnostic)
            {
                foreach (DiagnosticInfo diag in ciiResult.Diagnostics)
                    context.ReportDiagnostic(diag.ToDiagnostic());
            }

            if (!ciiResult.HasValue)
                return;

            (ComInterfaceInfo cii, INamedTypeSymbol _) = ciiResult.Value;

            // Build the context chain for this interface (ancestors first, then this interface) to detect
            // BaseInterfaceIsNotGenerated. Note: vtable indices don't need to be correct here since we're
            // only reporting diagnostics, not emitting code.
            ImmutableArray<ComInterfaceInfo> contextChain = BuildContextChain(
                typeSymbol, cii, env, generatedComInterfaceAttrType, interfaceInfoCache, ct);

            ImmutableArray<DiagnosticOr<ComInterfaceContext>> contextResults = ComInterfaceContext.GetContexts(contextChain, ct);
            // BuildContextChain always appends cii as the last element, so contextResults is always non-empty.
            Debug.Assert(contextResults.Length > 0);
            // The last entry corresponds to this interface
            DiagnosticOr<ComInterfaceContext> thisContextResult = contextResults[contextResults.Length - 1];
            if (thisContextResult.HasDiagnostic)
            {
                foreach (DiagnosticInfo diag in thisContextResult.Diagnostics)
                    context.ReportDiagnostic(diag.ToDiagnostic());
                return;
            }

            // Process each method declared on this interface
            foreach (DiagnosticOr<(ComMethodInfo ComMethod, IMethodSymbol Symbol)> methodResult in
                ComMethodInfo.GetMethodsFromInterface((cii, typeSymbol), ct))
            {
                if (methodResult.HasDiagnostic)
                {
                    foreach (DiagnosticInfo diag in methodResult.Diagnostics)
                        context.ReportDiagnostic(diag.ToDiagnostic());
                }

                if (!methodResult.HasValue)
                    continue;

                (ComMethodInfo comMethod, IMethodSymbol methodSymbol) = methodResult.Value;

                if (comMethod.Syntax is null)
                    continue; // externally-defined method; no stub diagnostics to report

                // Note: the vtable index passed here (0) doesn't need to be the correct vtable slot since
                // we're only reporting diagnostics, not emitting code.
                IncrementalMethodStubGenerationContext stubContext = ComInterfaceGenerator.CalculateStubInformation(
                    comMethod.Syntax,
                    methodSymbol,
                    0,
                    env,
                    cii,
                    ct);

                if (stubContext is not SourceAvailableIncrementalMethodStubGenerationContext srcCtx)
                    continue;

                ImmutableArray<DiagnosticInfo> managedToNativeDiags = ImmutableArray<DiagnosticInfo>.Empty;
                ImmutableArray<DiagnosticInfo> nativeToManagedDiags = ImmutableArray<DiagnosticInfo>.Empty;

                if (srcCtx.VtableIndexData.Direction is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                {
                    (_, managedToNativeDiags) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(srcCtx, ComInterfaceGeneratorHelpers.GetGeneratorResolver);
                }
                if (srcCtx.VtableIndexData.Direction is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                {
                    (_, nativeToManagedDiags) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(srcCtx, ComInterfaceGeneratorHelpers.GetGeneratorResolver);
                }

                // Deduplicate diagnostics reported for both directions (matching original generator behavior)
                foreach (DiagnosticInfo diag in managedToNativeDiags.Union(nativeToManagedDiags))
                    context.ReportDiagnostic(diag.ToDiagnostic());
            }
        }

        /// <summary>
        /// Builds the ancestor chain for context creation (root-to-parent order, then the current interface last).
        /// Only successfully-computed ancestors are included; if an ancestor fails, the chain stops there and
        /// <see cref="ComInterfaceContext.GetContexts"/> will emit <see cref="GeneratorDiagnostics.BaseInterfaceIsNotGenerated"/>
        /// for the next derived interface.
        /// </summary>
        private static ImmutableArray<ComInterfaceInfo> BuildContextChain(
            INamedTypeSymbol typeSymbol,
            ComInterfaceInfo cii,
            StubEnvironment env,
            INamedTypeSymbol generatedComInterfaceAttrType,
            ConcurrentDictionary<INamedTypeSymbol, DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)>> interfaceInfoCache,
            CancellationToken ct)
        {
            // For external base interfaces, CreateInterfaceInfoForBaseInterfacesInOtherCompilations already
            // provides the full ancestor chain ordered from root to immediate parent.
            ImmutableArray<(ComInterfaceInfo, INamedTypeSymbol)> externalBases =
                ComInterfaceInfo.CreateInterfaceInfoForBaseInterfacesInOtherCompilations(typeSymbol);
            if (!externalBases.IsEmpty)
            {
                return [.. externalBases.Select(static e => e.Item1), cii];
            }

            // Traverse same-compilation base interfaces, inserting at the front to get root-first order.
            var ancestorChain = new List<ComInterfaceInfo>();
            INamedTypeSymbol current = typeSymbol;

            while (true)
            {
                INamedTypeSymbol? baseSymbol = FindBaseComInterfaceSymbol(current, generatedComInterfaceAttrType);
                if (baseSymbol is null)
                    break;

                if (!SymbolEqualityComparer.Default.Equals(baseSymbol.ContainingAssembly, typeSymbol.ContainingAssembly))
                {
                    // Switch to external base handling
                    ImmutableArray<(ComInterfaceInfo, INamedTypeSymbol)> externalInfos =
                        ComInterfaceInfo.CreateInterfaceInfoForBaseInterfacesInOtherCompilations(current);
                    ancestorChain.InsertRange(0, externalInfos.Select(static e => e.Item1));
                    break;
                }

                // Get or compute the base's ComInterfaceInfo (using the cache for deduplication)
                DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)> baseResult = interfaceInfoCache.GetOrAdd(
                    baseSymbol,
                    sym =>
                    {
                        InterfaceDeclarationSyntax? baseSyntax = FindInterfaceSyntaxWithAttribute(sym, generatedComInterfaceAttrType, ct);
                        if (baseSyntax is null)
                            return DiagnosticOr<(ComInterfaceInfo, INamedTypeSymbol)>.From(
                                DiagnosticInfo.Create(GeneratorDiagnostics.CannotAnalyzeInterfacePattern, sym.Locations.FirstOrDefault() ?? Location.None, sym.Name));
                        return ComInterfaceInfo.From(sym, baseSyntax, env, ct);
                    });

                if (!baseResult.HasValue)
                    break; // Base failed — GetContexts will report BaseInterfaceIsNotGenerated for this interface

                ancestorChain.Insert(0, baseResult.Value.Item1);
                current = baseSymbol;
            }

            ancestorChain.Add(cii);
            return ancestorChain.ToImmutableArray();
        }

        /// <summary>
        /// Finds the first direct base interface of <paramref name="typeSymbol"/> that has the <see cref="TypeNames.GeneratedComInterfaceAttribute"/>.
        /// </summary>
        private static INamedTypeSymbol? FindBaseComInterfaceSymbol(INamedTypeSymbol typeSymbol, INamedTypeSymbol generatedComInterfaceAttrType)
        {
            foreach (INamedTypeSymbol iface in typeSymbol.Interfaces)
            {
                foreach (AttributeData attr in iface.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedComInterfaceAttrType))
                        return iface;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the <see cref="InterfaceDeclarationSyntax"/> for <paramref name="symbol"/> that carries the <see cref="TypeNames.GeneratedComInterfaceAttribute"/>.
        /// For partial types, this is the specific partial declaration that has the attribute.
        /// </summary>
        private static InterfaceDeclarationSyntax? FindInterfaceSyntaxWithAttribute(
            INamedTypeSymbol symbol,
            INamedTypeSymbol generatedComInterfaceAttrType,
            CancellationToken ct)
        {
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedComInterfaceAttrType))
                {
                    SyntaxReference? attrSyntaxRef = attr.ApplicationSyntaxReference;
                    if (attrSyntaxRef is not null)
                    {
                        SyntaxNode attrSyntax = attrSyntaxRef.GetSyntax(ct);
                        // Attribute syntax structure: AttributeSyntax -> AttributeListSyntax -> InterfaceDeclarationSyntax
                        if (attrSyntax.Parent?.Parent is InterfaceDeclarationSyntax ifaceSyntax)
                            return ifaceSyntax;
                    }
                    foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(ct) is InterfaceDeclarationSyntax ifaceSyntax)
                            return ifaceSyntax;
                    }
                    break;
                }
            }
            return null;
        }
    }
}
