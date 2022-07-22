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
                Dictionary<(ITypeSymbol marshallerType, ITypeSymbol managedType, bool isLinearCollectionMarshaller), List<string>> uniqueMarshallersToFix = new();
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    Document doc = fixAllContext.Solution.GetDocument(diagnostic.Location.SourceTree);
                    SemanticModel model = await doc.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                    var entryPointTypeSymbol = (INamedTypeSymbol)model.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start, fixAllContext.CancellationToken);
                    ITypeSymbol? managedType = GetManagedTypeInAttributeSyntax(diagnostic.Location, entryPointTypeSymbol);

                    SyntaxNode node = (await diagnostic.Location.SourceTree.GetRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false)).FindNode(diagnostic.Location.SourceSpan);
                    var marshallerType = (INamedTypeSymbol)model.GetSymbolInfo(node, fixAllContext.CancellationToken).Symbol;
                    var uniqueMarshallerFixKey = (marshallerType, managedType, ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointTypeSymbol));
                    if (uniqueMarshallersToFix.TryGetValue(uniqueMarshallerFixKey, out List<string> membersToAdd))
                    {
                        membersToAdd.AddRange(diagnostic.Properties[MissingMemberNames.Key].Split(MissingMemberNames.Delimiter));
                    }
                    else
                    {
                        uniqueMarshallersToFix.Add(uniqueMarshallerFixKey, new List<string>(diagnostic.Properties[MissingMemberNames.Key].Split(MissingMemberNames.Delimiter)));
                    }
                }

                Dictionary<ITypeSymbol, ITypeSymbol> partiallyUpdatedSymbols = new(SymbolEqualityComparer.Default);

                SymbolEditor symbolEditor = SymbolEditor.Create(fixAllContext.Solution);

                foreach (var marshallerInfo in uniqueMarshallersToFix)
                {
                    var (marshallerType, managedType, isLinearCollectionMarshaller) = marshallerInfo.Key;
                    HashSet<string> missingMembers = new(marshallerInfo.Value);

                    if (!partiallyUpdatedSymbols.TryGetValue(marshallerType, out ITypeSymbol newMarshallerType))
                    {
                        newMarshallerType = marshallerType;
                    }

                    newMarshallerType = (ITypeSymbol)await symbolEditor.EditOneDeclarationAsync(
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
            List<string> missingMemberNames = new();
            List<Diagnostic> requiredShapeDiagnostics = new();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape)
                {
                    requiredShapeDiagnostics.Add(diagnostic);
                    if (diagnostic.Properties.TryGetValue(MissingMemberNames.Key, out string missingMembers))
                    {
                        missingMemberNames.AddRange(missingMembers.Split(MissingMemberNames.Delimiter));
                    }
                }
            }

            return (new HashSet<string>(missingMemberNames), requiredShapeDiagnostics);
        }

        private static async Task<Solution> AddMissingMembers(Document doc, SyntaxNode node, HashSet<string> missingMemberNames, CancellationToken ct)
        {
            var model = await doc.GetSemanticModelAsync(ct).ConfigureAwait(false);

            var entryPointTypeSymbol = (INamedTypeSymbol)model.GetEnclosingSymbol(node.SpanStart, ct);

            // TODO: Convert to use the IOperation tree once IAttributeOperation is available
            var managedTypeSymbolInAttribute = GetManagedTypeInAttributeSyntax(node.GetLocation(), entryPointTypeSymbol);

            bool isLinearCollectionMarshaller = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryPointTypeSymbol);

            ManualTypeMarshallingHelper.TryResolveManagedType(entryPointTypeSymbol, ManualTypeMarshallingHelper.ReplaceGenericPlaceholderInType(managedTypeSymbolInAttribute, entryPointTypeSymbol, model.Compilation), isLinearCollectionMarshaller, (_, _) => { }, out ITypeSymbol managedType);

            SymbolEditor editor = SymbolEditor.Create(doc.Project.Solution);

            ITypeSymbol marshallerType = (ITypeSymbol)model.GetSymbolInfo(node, ct).Symbol;

            await editor.EditOneDeclarationAsync(marshallerType, (editor, decl) => AddMissingMembers(editor, decl, marshallerType, managedType, missingMemberNames, isLinearCollectionMarshaller), ct).ConfigureAwait(false);

            return editor.ChangedSolution;
        }

        private static ITypeSymbol? GetManagedTypeInAttributeSyntax(Location locationInAttribute, INamedTypeSymbol? attributedTypeSymbol)
            => (ITypeSymbol)attributedTypeSymbol.GetAttributes().First(attr =>
                    attr.ApplicationSyntaxReference.SyntaxTree == locationInAttribute.SourceTree
                    && attr.ApplicationSyntaxReference.Span.Contains(locationInAttribute.SourceSpan)).ConstructorArguments[0].Value;

        private static void AddMissingMembers(
            DocumentEditor editor,
            SyntaxNode declaringSyntax,
            ITypeSymbol marshallerType,
            ITypeSymbol managedType,
            HashSet<string> missingMemberNames,
            bool isLinearCollectionMarshaller)
        {
            if (marshallerType.IsStatic && marshallerType.IsReferenceType)
            {
                AddMissingMembersToStatelessMarshaller(editor, declaringSyntax, marshallerType, managedType, missingMemberNames, isLinearCollectionMarshaller);
            }
        }

        private static void AddMissingMembersToStatelessMarshaller(DocumentEditor editor, SyntaxNode declaringSyntax, ITypeSymbol marshallerType, ITypeSymbol managedType, HashSet<string> missingMemberNames, bool isLinearCollectionMarshaller)
        {
            SyntaxGenerator gen = editor.Generator;
            Lazy<SyntaxNode> unmanagedTypeSyntax = new(() =>
                {
                    ITypeSymbol? unmanagedType = FindUnmanagedTypeFromExistingShape();
                    if (unmanagedType is not null)
                    {
                        return gen.TypeExpression(unmanagedType);
                    }
                    return gen.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_IntPtr)).WithAdditionalAnnotations(RenameAnnotation.Create());
                });

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

            editor.ReplaceNode(declaringSyntax, (declaringSyntax, gen) => gen.AddMembers(declaringSyntax, newMembers));

            ITypeSymbol? FindUnmanagedTypeFromExistingShape()
            {
                // Check for the presence of any method that has the unmanaged type in its signature.
                // If one is present, return the unmanaged type from that method.
                var (_, methods) = StatelessMarshallerShapeHelper.GetShapeForType(marshallerType, managedType, isLinearCollectionMarshaller, editor.SemanticModel.Compilation);

                if (methods.ToUnmanaged is not null)
                {
                    return methods.ToUnmanaged.ReturnType;
                }
                if (methods.ToUnmanagedWithBuffer is not null)
                {
                    return methods.ToUnmanagedWithBuffer.ReturnType;
                }
                if (methods.ToManaged is not null)
                {
                    return methods.ToManaged.Parameters[0].Type;
                }
                if (methods.ToManagedFinally is not null)
                {
                    return methods.ToManagedFinally.Parameters[0].Type;
                }
                if (methods.UnmanagedValuesSource is not null)
                {
                    return methods.UnmanagedValuesSource.Parameters[0].Type;
                }
                if (methods.UnmanagedValuesDestination is not null)
                {
                    return methods.UnmanagedValuesDestination.Parameters[0].Type;
                }
                return null;
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
