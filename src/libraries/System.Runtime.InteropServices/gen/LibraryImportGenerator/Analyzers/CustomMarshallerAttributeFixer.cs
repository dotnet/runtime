// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CustomMarshallerAttributeFixer : CodeFixProvider
    {
        private const string AddMissingCustomTypeMarshallerMembersKey = nameof(AddMissingCustomTypeMarshallerMembersKey);

        private sealed class CustomFixAllProvider : FixAllProvider
        {
            public static FixAllProvider Instance { get; } = new CustomFixAllProvider();

            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                ImmutableArray<Diagnostic> diagnostics = await GetAllDiagnosticsInScope(fixAllContext).ConfigureAwait(false);
                Dictionary<(INamedTypeSymbol marshallerType, ITypeSymbol managedType, bool isLinearCollectionMarshaller), HashSet<string>> uniqueMarshallersToFix = new();
                // Organize all the diagnostics by marshaller, managed type, and whether or not it's a collection marshaller
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    Document doc = fixAllContext.Solution.GetDocument(diagnostic.Location.SourceTree);
                    SemanticModel model = await doc.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                    var entryPointTypeSymbol = (INamedTypeSymbol)model.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start, fixAllContext.CancellationToken);
                    ITypeSymbol? managedType = GetManagedTypeInAttributeSyntax(diagnostic.Location, entryPointTypeSymbol);

                    SyntaxNode root = await diagnostic.Location.SourceTree.GetRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                    SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);
                    var marshallerType = (INamedTypeSymbol)model.GetSymbolInfo(node, fixAllContext.CancellationToken).Symbol;
                    var uniqueMarshallerFixKey = (marshallerType, managedType, ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointTypeSymbol));
                    if (!uniqueMarshallersToFix.TryGetValue(uniqueMarshallerFixKey, out HashSet<string> membersToAdd))
                    {
                        uniqueMarshallersToFix[uniqueMarshallerFixKey] = membersToAdd = new HashSet<string>();
                    }

                    membersToAdd.UnionWith(diagnostic.Properties[MissingMemberNames.Key].Split(MissingMemberNames.Delimiter));
                }

                Dictionary<INamedTypeSymbol, INamedTypeSymbol> partiallyUpdatedSymbols = new(SymbolEqualityComparer.Default);

                SymbolEditor symbolEditor = SymbolEditor.Create(fixAllContext.Solution);

                // Apply each fix
                foreach (var marshallerInfo in uniqueMarshallersToFix)
                {
                    var (marshallerType, managedType, isLinearCollectionMarshaller) = marshallerInfo.Key;
                    HashSet<string> missingMembers = marshallerInfo.Value;

                    if (!partiallyUpdatedSymbols.TryGetValue(marshallerType, out INamedTypeSymbol newMarshallerType))
                    {
                        newMarshallerType = marshallerType;
                    }

                    newMarshallerType = (INamedTypeSymbol)await symbolEditor.EditOneDeclarationAsync(
                        marshallerType,
                        (editor, decl) => AddMissingMembers(
                            editor,
                            decl,
                            marshallerType,
                            managedType,
                            missingMembers,
                            isLinearCollectionMarshaller),
                        fixAllContext.CancellationToken).ConfigureAwait(false);

                    partiallyUpdatedSymbols[marshallerType] = newMarshallerType;
                }

                return CodeAction.Create(SR.AddMissingCustomTypeMarshallerMembers, ct => Task.FromResult(symbolEditor.ChangedSolution));
            }

            private static async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsInScope(FixAllContext context)
            {
                switch (context.Scope)
                {
                    case FixAllScope.Document:
                        return await context.GetDocumentDiagnosticsAsync(context.Document).ConfigureAwait(false);
                    case FixAllScope.Project:
                        return await context.GetAllDiagnosticsAsync(context.Project).ConfigureAwait(false);
                    case FixAllScope.Solution:
                        return ImmutableArray.CreateRange((await Task.WhenAll(context.Solution.Projects.Select(context.GetAllDiagnosticsAsync)).ConfigureAwait(false)).SelectMany(arr => arr));
                    default:
                        throw new UnreachableException();
                }
            }
        }

        public override FixAllProvider? GetFixAllProvider() => CustomFixAllProvider.Instance;

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            SyntaxNode node = root.FindNode(context.Span);
            var (missingMemberNames, missingMembersDiagnostics) = GetRequiredShapeMissingMemberNames(context.Diagnostics);

            if (missingMembersDiagnostics.Count > 0)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        SR.AddMissingCustomTypeMarshallerMembers,
                        ct => AddMissingMembers(doc, node, missingMemberNames, ct),
                        AddMissingCustomTypeMarshallerMembersKey),
                    missingMembersDiagnostics);
            }
        }

        private static (HashSet<string> missingMembers, List<Diagnostic> fixedDiagnostics) GetRequiredShapeMissingMemberNames(IEnumerable<Diagnostic> diagnostics)
        {
            HashSet<string> missingMemberNames = new();
            List<Diagnostic> requiredShapeDiagnostics = new();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape)
                {
                    requiredShapeDiagnostics.Add(diagnostic);
                    if (diagnostic.Properties.TryGetValue(MissingMemberNames.Key, out string missingMembers))
                    {
                        missingMemberNames.UnionWith(missingMembers.Split(MissingMemberNames.Delimiter));
                    }
                }
            }

            return (missingMemberNames, requiredShapeDiagnostics);
        }

        private static void IgnoreArityMismatch(INamedTypeSymbol marshallerType, INamedTypeSymbol managedType)
        {
        }

        private static async Task<Solution> AddMissingMembers(Document doc, SyntaxNode node, HashSet<string> missingMemberNames, CancellationToken ct)
        {
            var model = await doc.GetSemanticModelAsync(ct).ConfigureAwait(false);

            var entryPointTypeSymbol = (INamedTypeSymbol)model.GetEnclosingSymbol(node.SpanStart, ct);

            // TODO: Convert to use the IOperation tree once IAttributeOperation is available
            var managedTypeSymbolInAttribute = GetManagedTypeInAttributeSyntax(node.GetLocation(), entryPointTypeSymbol);

            bool isLinearCollectionMarshaller = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointTypeSymbol);

            // Explicitly ignore the generic arity mismatch diagnostics as we will only reach here if there are no mismatches.
            // The analyzer will not for missing members if the managed type cannot be resolved.
            ManualTypeMarshallingHelper.TryResolveManagedType(entryPointTypeSymbol, ManualTypeMarshallingHelper.ReplaceGenericPlaceholderInType(managedTypeSymbolInAttribute, entryPointTypeSymbol, model.Compilation), isLinearCollectionMarshaller, IgnoreArityMismatch, out ITypeSymbol managedType);

            SymbolEditor editor = SymbolEditor.Create(doc.Project.Solution);

            INamedTypeSymbol marshallerType = (INamedTypeSymbol)model.GetSymbolInfo(node, ct).Symbol;

            await editor.EditOneDeclarationAsync(marshallerType, (editor, decl) => AddMissingMembers(editor, decl, marshallerType, managedType, missingMemberNames, isLinearCollectionMarshaller), ct).ConfigureAwait(false);

            return editor.ChangedSolution;
        }

        // Get the managed type from the CustomMarshallerAttribute located at the provided location in source on the provided type.
        // As we only get fixable diagnostics for types that have valid non-null managed types in the CustomMarshallerAttribute applications,
        // we do not need to worry about the returned symbol being null.
        private static ITypeSymbol GetManagedTypeInAttributeSyntax(Location locationInAttribute, INamedTypeSymbol attributedTypeSymbol)
            => (ITypeSymbol)attributedTypeSymbol.GetAttributes().First(attr =>
                    attr.ApplicationSyntaxReference.SyntaxTree == locationInAttribute.SourceTree
                    && attr.ApplicationSyntaxReference.Span.Contains(locationInAttribute.SourceSpan)).ConstructorArguments[0].Value!;

        private static void AddMissingMembers(
            DocumentEditor editor,
            SyntaxNode declaringSyntax,
            INamedTypeSymbol marshallerType,
            ITypeSymbol managedType,
            HashSet<string> missingMemberNames,
            bool isLinearCollectionMarshaller)
        {
            if (marshallerType.IsStatic && marshallerType.IsReferenceType)
            {
                AddMissingMembersToStatelessMarshaller(editor, declaringSyntax, marshallerType, managedType, missingMemberNames, isLinearCollectionMarshaller);
            }
            if (marshallerType.IsValueType)
            {
                AddMissingMembersToStatefulMarshaller(editor, declaringSyntax, marshallerType, managedType, missingMemberNames, isLinearCollectionMarshaller);
            }
        }

        private static void AddMissingMembersToStatelessMarshaller(DocumentEditor editor, SyntaxNode declaringSyntax, INamedTypeSymbol marshallerType, ITypeSymbol managedType, HashSet<string> missingMemberNames, bool isLinearCollectionMarshaller)
        {
            SyntaxGenerator gen = editor.Generator;
            // Get the methods of the shape so we can use them to determine what types to use in signatures that are not obvious.
            var (_, methods) = StatelessMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, isLinearCollectionMarshaller, editor.SemanticModel.Compilation);
            INamedTypeSymbol spanOfT = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_Span_Metadata)!;
            INamedTypeSymbol readOnlySpanOfT = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_ReadOnlySpan_Metadata)!;
            var (typeParameters, _) = marshallerType.GetAllTypeArgumentsIncludingInContainingTypes();

            // Use a lazy factory for the type syntaxes to avoid re-checking the various methods and reconstructing the syntax.
            Lazy<SyntaxNode> unmanagedTypeSyntax = new(CreateUnmanagedTypeSyntax, isThreadSafe: false);
            Lazy<ITypeSymbol> managedElementTypeSymbol = new(CreateManagedElementTypeSymbol, isThreadSafe: false);

            List<SyntaxNode> newMembers = new();

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateless.ConvertToUnmanaged,
                        parameters: new[] { gen.ParameterDeclaration("managed", gen.TypeExpression(managedType)) },
                        returnType: unmanagedTypeSyntax.Value,
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateless.ConvertToManaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateless.ConvertToManaged,
                        parameters: new[] { gen.ParameterDeclaration("unmanaged", unmanagedTypeSyntax.Value) },
                        returnType: gen.TypeExpression(managedType),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.BufferSize))
            {
                newMembers.Add(
                    gen.WithAccessorDeclarations(
                        gen.PropertyDeclaration(ShapeMemberNames.BufferSize,
                            gen.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)),
                            Accessibility.Public,
                            DeclarationModifiers.Static),
                        gen.GetAccessorDeclaration(statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) })));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("managed", gen.TypeExpression(managedType)),
                            gen.ParameterDeclaration("numElements", type: gen.TypeExpression(SpecialType.System_Int32), refKind: RefKind.Out),
                        },
                        returnType: unmanagedTypeSyntax.Value,
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("unmanaged", unmanagedTypeSyntax.Value),
                            gen.ParameterDeclaration("numElements", type: gen.TypeExpression(SpecialType.System_Int32)),
                        },
                        returnType: gen.TypeExpression(managedType),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("managed", gen.TypeExpression(managedType))
                        },
                        returnType: gen.TypeExpression(readOnlySpanOfT.Construct(managedElementTypeSymbol.Value)),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("unmanaged", unmanagedTypeSyntax.Value),
                            gen.ParameterDeclaration("numElements", gen.TypeExpression(SpecialType.System_Int32))
                        },
                        returnType: gen.TypeExpression(spanOfT.Construct(typeParameters[typeParameters.Length - 1])),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("unmanaged", unmanagedTypeSyntax.Value),
                            gen.ParameterDeclaration("numElements", gen.TypeExpression(SpecialType.System_Int32))
                        },
                        returnType: gen.TypeExpression(readOnlySpanOfT.Construct(typeParameters[typeParameters.Length - 1])),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("managed", gen.TypeExpression(managedType))
                        },
                        returnType: gen.TypeExpression(spanOfT.Construct(managedElementTypeSymbol.Value)),
                        accessibility: Accessibility.Public,
                        modifiers: DeclarationModifiers.Static,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            editor.ReplaceNode(declaringSyntax, (declaringSyntax, gen) => gen.AddMembers(declaringSyntax, newMembers));

            SyntaxNode CreateUnmanagedTypeSyntax()
            {
                ITypeSymbol? unmanagedType = null;
                if (methods.ToUnmanaged is not null)
                {
                    unmanagedType = methods.ToUnmanaged.ReturnType;
                }
                else if (methods.ToUnmanagedWithBuffer is not null)
                {
                    unmanagedType = methods.ToUnmanagedWithBuffer.ReturnType;
                }
                else if (methods.ToManaged is not null)
                {
                    unmanagedType = methods.ToManaged.Parameters[0].Type;
                }
                else if (methods.ToManagedFinally is not null)
                {
                    unmanagedType = methods.ToManagedFinally.Parameters[0].Type;
                }
                else if (methods.UnmanagedValuesSource is not null)
                {
                    unmanagedType = methods.UnmanagedValuesSource.Parameters[0].Type;
                }
                else if (methods.UnmanagedValuesDestination is not null)
                {
                    unmanagedType = methods.UnmanagedValuesDestination.Parameters[0].Type;
                }

                if (unmanagedType is not null)
                {
                    return gen.TypeExpression(unmanagedType);
                }
                return gen.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IntPtr));
            }

            ITypeSymbol CreateManagedElementTypeSymbol()
            {
                if (methods.ManagedValuesSource is not null)
                {
                    return ((INamedTypeSymbol)methods.ManagedValuesSource.ReturnType).TypeArguments[0];
                }
                if (methods.ManagedValuesDestination is not null)
                {
                    return ((INamedTypeSymbol)methods.ManagedValuesDestination.ReturnType).TypeArguments[0];
                }

                return editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IntPtr);
            }
        }

        private static void AddMissingMembersToStatefulMarshaller(DocumentEditor editor, SyntaxNode declaringSyntax, INamedTypeSymbol marshallerType, ITypeSymbol managedType, HashSet<string> missingMemberNames, bool isLinearCollectionMarshaller)
        {
            SyntaxGenerator gen = editor.Generator;
            // Get the methods of the shape so we can use them to determine what types to use in signatures that are not obvious.
            var (_, methods) = StatefulMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, isLinearCollectionMarshaller, editor.SemanticModel.Compilation);
            INamedTypeSymbol spanOfT = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_Span_Metadata)!;
            INamedTypeSymbol readOnlySpanOfT = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_ReadOnlySpan_Metadata)!;
            var (typeParameters, _) = marshallerType.GetAllTypeArgumentsIncludingInContainingTypes();

            // Use a lazy factory for the type syntaxes to avoid re-checking the various methods and reconstructing the syntax.
            Lazy<SyntaxNode> unmanagedTypeSyntax = new(CreateUnmanagedTypeSyntax, isThreadSafe: false);
            Lazy<ITypeSymbol> managedElementTypeSymbol = new(CreateManagedElementTypeSymbol, isThreadSafe: false);

            List<SyntaxNode> newMembers = new();

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateful.FromManaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateful.FromManaged,
                        parameters: new[] { gen.ParameterDeclaration("managed", gen.TypeExpression(managedType)) },
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateful.ToUnmanaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateful.ToUnmanaged,
                        returnType: unmanagedTypeSyntax.Value,
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateful.FromUnmanaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateful.FromUnmanaged,
                        parameters: new[] { gen.ParameterDeclaration("unmanaged", unmanagedTypeSyntax.Value) },
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.Value.Stateful.ToManaged))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateful.ToManaged,
                        returnType: gen.TypeExpression(managedType),
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.BufferSize))
            {
                newMembers.Add(
                    gen.WithAccessorDeclarations(
                        gen.PropertyDeclaration(ShapeMemberNames.BufferSize,
                            gen.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)),
                            Accessibility.Public,
                            DeclarationModifiers.Static),
                        gen.GetAccessorDeclaration(statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) })));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource,
                        returnType: gen.TypeExpression(readOnlySpanOfT.Construct(managedElementTypeSymbol.Value)),
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination,
                        returnType: gen.TypeExpression(spanOfT.Construct(typeParameters[typeParameters.Length - 1])),
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("numElements", gen.TypeExpression(SpecialType.System_Int32))
                        },
                        returnType: gen.TypeExpression(readOnlySpanOfT.Construct(typeParameters[typeParameters.Length - 1])),
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination,
                        parameters: new[]
                        {
                            gen.ParameterDeclaration("numElements", gen.TypeExpression(SpecialType.System_Int32))
                        },
                        returnType: gen.TypeExpression(spanOfT.Construct(managedElementTypeSymbol.Value)),
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            if (missingMemberNames.Contains(ShapeMemberNames.Free))
            {
                newMembers.Add(
                    gen.MethodDeclaration(
                        ShapeMemberNames.Value.Stateful.Free,
                        accessibility: Accessibility.Public,
                        statements: new[] { DefaultMethodStatement(gen, editor.SemanticModel.Compilation) }));
            }

            editor.ReplaceNode(declaringSyntax, (declaringSyntax, gen) => gen.AddMembers(declaringSyntax, newMembers));

            SyntaxNode CreateUnmanagedTypeSyntax()
            {
                ITypeSymbol? unmanagedType = null;
                if (methods.ToUnmanaged is not null)
                {
                    unmanagedType = methods.ToUnmanaged.ReturnType;
                }
                else if (methods.FromUnmanaged is not null)
                {
                    unmanagedType = methods.FromUnmanaged.Parameters[0].Type;
                }
                else if (methods.UnmanagedValuesSource is not null)
                {
                    unmanagedType = methods.UnmanagedValuesSource.Parameters[0].Type;
                }
                else if (methods.UnmanagedValuesDestination is not null)
                {
                    unmanagedType = methods.UnmanagedValuesDestination.Parameters[0].Type;
                }

                if (unmanagedType is not null)
                {
                    return gen.TypeExpression(unmanagedType);
                }
                return gen.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IntPtr));
            }

            ITypeSymbol CreateManagedElementTypeSymbol()
            {
                if (methods.ManagedValuesSource is not null)
                {
                    return ((INamedTypeSymbol)methods.ManagedValuesSource.ReturnType).TypeArguments[0];
                }
                if (methods.ManagedValuesDestination is not null)
                {
                    return ((INamedTypeSymbol)methods.ManagedValuesDestination.ReturnType).TypeArguments[0];
                }

                return editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IntPtr);
            }
        }

        private static SyntaxNode DefaultMethodStatement(SyntaxGenerator generator, Compilation compilation)
        {
            return generator.ThrowStatement(generator.ObjectCreationExpression(
                generator.TypeExpression(
                    compilation.GetTypeByMetadataName("System.NotImplementedException"))));
        }
    }
}
