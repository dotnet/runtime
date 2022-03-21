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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public class CustomTypeMarshallerFixer : CodeFixProvider
    {
        private const string AddMissingCustomTypeMarshallerMembersKey = nameof(AddMissingCustomTypeMarshallerMembersKey);
        private const string AddMissingCustomTypeMarshallerFeaturesKey = nameof(AddMissingCustomTypeMarshallerFeaturesKey);

        private class CustomFixAllProvider : DocumentBasedFixAllProvider
        {
            protected override async Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                SyntaxNode? root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                if (root == null)
                    return document;

                DocumentEditor editor = await DocumentEditor.CreateAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);

                switch (fixAllContext.CodeActionEquivalenceKey)
                {
                    case AddMissingCustomTypeMarshallerMembersKey:
                        foreach (IGrouping<TextSpan, Diagnostic> diagnosticsBySpan in diagnostics.GroupBy(d => d.Location.SourceSpan))
                        {
                            SyntaxNode node = root.FindNode(diagnosticsBySpan.Key);

                            AddMissingMembers(fixAllContext, editor, diagnosticsBySpan, node);
                        }
                        break;
                    case AddMissingCustomTypeMarshallerFeaturesKey:
                        foreach (IGrouping<TextSpan, Diagnostic> diagnosticsBySpan in diagnostics.GroupBy(d => d.Location.SourceSpan))
                        {
                            SyntaxNode node = root.FindNode(diagnosticsBySpan.Key);

                            await AddMissingFeatures(fixAllContext, editor, diagnosticsBySpan, node).ConfigureAwait(false);
                        }
                        break;
                    default:
                        break;
                }

                return editor.GetChangedDocument();
            }

            private static void AddMissingMembers(FixAllContext fixAllContext, DocumentEditor editor, IEnumerable<Diagnostic> diagnostics, SyntaxNode node)
            {
                var (missingMemberNames, _) = GetRequiredShapeMissingMemberNames(diagnostics);
                ITypeSymbol marshallerType = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(node);
                editor.ReplaceNode(
                    node,
                    (node, gen) =>
                        CustomTypeMarshallerFixer.AddMissingMembers(
                            node,
                            marshallerType,
                            missingMemberNames,
                            editor.SemanticModel.Compilation,
                            gen));
            }

            private static async Task AddMissingFeatures(FixAllContext fixAllContext, DocumentEditor editor, IEnumerable<Diagnostic> diagnostics, SyntaxNode node)
            {
                var (featuresToAdd, _) = GetFeaturesToAdd(diagnostics);
                ITypeSymbol marshallerType = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(node);
                AttributeData customTypeMarshallerAttribute = marshallerType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.CustomTypeMarshallerAttribute);

                SyntaxNode attributeSyntax = await customTypeMarshallerAttribute.ApplicationSyntaxReference!.GetSyntaxAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                editor.ReplaceNode(
                    attributeSyntax,
                    (node, gen) =>
                        CustomTypeMarshallerFixer.AddMissingFeatures(
                            gen.GetName(node),
                            customTypeMarshallerAttribute,
                            featuresToAdd,
                            gen));
            }
        }

        public override FixAllProvider? GetFixAllProvider() => new CustomFixAllProvider();

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape,
                AnalyzerDiagnostics.Ids.MissingAllocatingMarshallingFallback,
                AnalyzerDiagnostics.Ids.ProvidedMethodsNotSpecifiedInShape);

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
                        Resources.AddMissingCustomTypeMarshallerMembers,
                        ct => AddMissingMembers(doc, node, missingMemberNames, ct),
                        AddMissingCustomTypeMarshallerMembersKey),
                    missingMembersDiagnostics);
            }

            var (featuresToAdd, featuresToAddDiagnostics) = GetFeaturesToAdd(context.Diagnostics);

            if (featuresToAddDiagnostics.Count > 0 && featuresToAdd != CustomTypeMarshallerFeatures.None)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        Resources.AddMissingFeaturesToCustomTypeMarshaller,
                        ct => AddMissingFeatures(doc, node, featuresToAdd, ct),
                        AddMissingCustomTypeMarshallerFeaturesKey),
                    featuresToAddDiagnostics);
            }
        }

        private static (List<string> missingMembers, List<Diagnostic> fixedDiagnostics) GetRequiredShapeMissingMemberNames(IEnumerable<Diagnostic> diagnostics)
        {
            List<string> missingMemberNames = new();
            List<Diagnostic> requiredShapeDiagnostics = new();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape)
                {
                    requiredShapeDiagnostics.Add(diagnostic);
                    if (diagnostic.Properties.TryGetValue(CustomTypeMarshallerAnalyzer.MissingMemberNames.Key, out string missingMembers))
                    {
                        missingMemberNames.AddRange(missingMembers.Split(CustomTypeMarshallerAnalyzer.MissingMemberNames.Delimiter));
                    }
                }
            }

            return (missingMemberNames, requiredShapeDiagnostics);
        }
        private static (CustomTypeMarshallerFeatures featuresToAdd, List<Diagnostic> fixedDiagnostics) GetFeaturesToAdd(IEnumerable<Diagnostic> diagnostics)
        {
            List<Diagnostic> featuresToAddDiagnostics = new();
            CustomTypeMarshallerFeatures featuresToAdd = CustomTypeMarshallerFeatures.None;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == AnalyzerDiagnostics.Ids.ProvidedMethodsNotSpecifiedInShape)
                {
                    featuresToAddDiagnostics.Add(diagnostic);
                    if (diagnostic.Properties.TryGetValue(CustomTypeMarshallerAnalyzer.MissingFeaturesKey, out string missingFeatures)
                        && Enum.TryParse(missingFeatures, out CustomTypeMarshallerFeatures featuresValue))
                    {
                        featuresToAdd |= featuresValue;
                    }
                }
            }

            return (featuresToAdd, featuresToAddDiagnostics);
        }
        private static async Task<Document> AddMissingFeatures(Document doc, SyntaxNode node, CustomTypeMarshallerFeatures featuresToAdd, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(doc, ct).ConfigureAwait(false);
            var gen = editor.Generator;

            ISymbol marshallerType = editor.SemanticModel.GetDeclaredSymbol(node, ct);

            AttributeData customTypeMarshallerAttribute = marshallerType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.ToDisplayString() == TypeNames.CustomTypeMarshallerAttribute);

            SyntaxNode attributeSyntax = await customTypeMarshallerAttribute.ApplicationSyntaxReference!.GetSyntaxAsync(ct).ConfigureAwait(false);

            SyntaxNode updatedDeclaration = AddMissingFeatures(gen.GetName(attributeSyntax), customTypeMarshallerAttribute, featuresToAdd, gen);

            editor.ReplaceNode(attributeSyntax, updatedDeclaration);

            return editor.GetChangedDocument();
        }

        private static SyntaxNode AddMissingFeatures(string attributeName, AttributeData? customTypeMarshallerAttribute, CustomTypeMarshallerFeatures featuresToAdd, SyntaxGenerator gen)
        {
            SyntaxNode newAttributeSyntax = gen.Attribute(attributeName);

            newAttributeSyntax = gen.AddAttributeArguments(newAttributeSyntax, customTypeMarshallerAttribute.ConstructorArguments.Select(a => gen.AttributeArgument(gen.TypedConstantExpression(a))));

            CustomTypeMarshallerFeatures newFeaturesValue = featuresToAdd;
            int? featuresArgLocation = null;

            newAttributeSyntax = gen.AddAttributeArguments(newAttributeSyntax, customTypeMarshallerAttribute.NamedArguments
                .Where((a, i) =>
                {
                    if (a.Key == "Features")
                    {
                        // Capture the original location of the 'Features' named argument so we can update it "in place".
                        featuresArgLocation = customTypeMarshallerAttribute.ConstructorArguments.Length + i;
                        newFeaturesValue |= (CustomTypeMarshallerFeatures)a.Value.Value;
                        return false;
                    }
                    return true;
                })
                .Select(a => gen.AttributeArgument(a.Key, gen.TypedConstantExpression(a.Value))));

            SyntaxNode featureAttributeArgument = gen.AttributeArgument("Features",
                gen.GetEnumValueAsFlagsExpression(
                    customTypeMarshallerAttribute.AttributeClass.GetMembers(ManualTypeMarshallingHelper.CustomMarshallerAttributeFields.Features).OfType<IPropertySymbol>().First().Type,
                    (int)newFeaturesValue,
                    includeZeroValueFlags: false));

            newAttributeSyntax = featuresArgLocation is null
                ? gen.AddAttributeArguments(newAttributeSyntax, new[] { featureAttributeArgument })
                : gen.InsertAttributeArguments(newAttributeSyntax, featuresArgLocation.Value, new[] { featureAttributeArgument });

            return newAttributeSyntax;
        }

        private static async Task<Document> AddMissingMembers(Document doc, SyntaxNode node, List<string> missingMemberNames, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(doc, ct).ConfigureAwait(false);
            var gen = editor.Generator;

            SyntaxNode updatedDeclaration = AddMissingMembers(node, (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(node, ct), missingMemberNames, editor.SemanticModel.Compilation, gen);

            editor.ReplaceNode(node, updatedDeclaration);

            return editor.GetChangedDocument();
        }

        private static SyntaxNode AddMissingMembers(SyntaxNode node, ITypeSymbol
            marshallerType, List<string> missingMemberNames, Compilation compilation, SyntaxGenerator gen)
        {
            INamedTypeSymbol @byte = compilation.GetSpecialType(SpecialType.System_Byte);
            INamedTypeSymbol @object = compilation.GetSpecialType(SpecialType.System_Object);
            INamedTypeSymbol spanOfT = compilation.GetTypeByMetadataName(TypeNames.System_Span_Metadata)!;
            INamedTypeSymbol spanOfByte = spanOfT.Construct(@byte)!;
            INamedTypeSymbol readOnlySpanOfT = compilation.GetTypeByMetadataName(TypeNames.System_ReadOnlySpan_Metadata)!;
            INamedTypeSymbol readOnlySpanOfByte = readOnlySpanOfT.Construct(@byte)!;
            INamedTypeSymbol int32 = compilation.GetSpecialType(SpecialType.System_Int32);

            SyntaxNode updatedDeclaration = node;


            (_, ITypeSymbol managedType, _) = ManualTypeMarshallingHelper.GetMarshallerShapeInfo(marshallerType);

            IMethodSymbol? fromNativeValueMethod = ManualTypeMarshallingHelper.FindFromNativeValueMethod(marshallerType);
            IMethodSymbol? toNativeValueMethod = ManualTypeMarshallingHelper.FindToNativeValueMethod(marshallerType);
            IMethodSymbol? getManagedValuesSourceMethod = ManualTypeMarshallingHelper.FindGetManagedValuesSourceMethod(marshallerType, readOnlySpanOfT);
            IMethodSymbol? getManagedValuesDestinationMethod = ManualTypeMarshallingHelper.FindGetManagedValuesDestinationMethod(marshallerType, spanOfT);

            SyntaxNode[] throwNotImplementedStatements = new[]
            {
                gen.ThrowStatement(gen.ObjectCreationExpression(gen.DottedName("System.NotImplementedException")))
            };

            foreach (string missingMemberName in missingMemberNames)
            {
                switch (missingMemberName)
                {
                    case CustomTypeMarshallerAnalyzer.MissingMemberNames.ValueManagedToNativeConstructor:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.ConstructorDeclaration(
                            gen.GetName(node),
                            new[]
                            {
                                gen.ParameterDeclaration("managed", type: gen.TypeExpression(managedType))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case CustomTypeMarshallerAnalyzer.MissingMemberNames.ValueCallerAllocatedBufferConstructor:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.ConstructorDeclaration(
                            gen.GetName(node),
                            new[]
                            {
                                gen.ParameterDeclaration("managed", type: gen.TypeExpression(managedType)),
                                gen.ParameterDeclaration("buffer", type: gen.TypeExpression(spanOfByte))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case CustomTypeMarshallerAnalyzer.MissingMemberNames.CollectionManagedToNativeConstructor:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.ConstructorDeclaration(
                            gen.GetName(node),
                            new[]
                            {
                                gen.ParameterDeclaration("managed", type: gen.TypeExpression(managedType)),
                                gen.ParameterDeclaration("nativeElementSize", type: gen.TypeExpression(int32))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case CustomTypeMarshallerAnalyzer.MissingMemberNames.CollectionCallerAllocatedBufferConstructor:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.ConstructorDeclaration(
                            gen.GetName(node),
                            new[]
                            {
                                gen.ParameterDeclaration("managed", type: gen.TypeExpression(managedType)),
                                gen.ParameterDeclaration("buffer", type: gen.TypeExpression(spanOfByte)),
                                gen.ParameterDeclaration("nativeElementSize", type: gen.TypeExpression(int32))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case CustomTypeMarshallerAnalyzer.MissingMemberNames.CollectionNativeElementSizeConstructor:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.ConstructorDeclaration(
                            gen.GetName(node),
                            new[]
                            {
                                gen.ParameterDeclaration("nativeElementSize", type: gen.TypeExpression(int32))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.Value.ToManaged:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.Value.ToManaged,
                            returnType: gen.TypeExpression(managedType),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.Value.FreeNative:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(ShapeMemberNames.Value.FreeNative,
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.Value.FromNativeValue:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.Value.FromNativeValue,
                            parameters: new[]
                            {
                                gen.ParameterDeclaration("value",
                                    type: gen.TypeExpression(toNativeValueMethod?.ReturnType ?? @byte))
                            },
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.Value.ToNativeValue:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.Value.ToNativeValue,
                            returnType: gen.TypeExpression(fromNativeValueMethod?.Parameters[0].Type ?? @byte),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.LinearCollection.GetManagedValuesSource:
                        INamedTypeSymbol? getManagedValuesDestinationReturnType = (INamedTypeSymbol?)getManagedValuesDestinationMethod?.ReturnType;
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.LinearCollection.GetManagedValuesSource,
                            returnType: gen.TypeExpression(
                                readOnlySpanOfT.Construct(
                                    getManagedValuesDestinationReturnType?.TypeArguments[0] ?? @object)),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.LinearCollection.GetNativeValuesDestination:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.LinearCollection.GetNativeValuesDestination,
                            returnType: gen.TypeExpression(spanOfByte),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.LinearCollection.GetNativeValuesSource:
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.LinearCollection.GetNativeValuesSource,
                            parameters: new[]
                            {
                                gen.ParameterDeclaration("numElements", type: gen.TypeExpression(int32))
                            },
                            returnType: gen.TypeExpression(readOnlySpanOfByte),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    case ShapeMemberNames.LinearCollection.GetManagedValuesDestination:
                        INamedTypeSymbol? getManagedValuesSourceReturnType = (INamedTypeSymbol?)getManagedValuesSourceMethod?.ReturnType;
                        updatedDeclaration = gen.AddMembers(updatedDeclaration, gen.MethodDeclaration(
                            ShapeMemberNames.LinearCollection.GetNativeValuesDestination,
                            parameters: new[]
                            {
                                gen.ParameterDeclaration("numElements", type: gen.TypeExpression(int32))
                            },
                            returnType: gen.TypeExpression(
                                spanOfT.Construct(
                                    getManagedValuesSourceReturnType?.TypeArguments[0] ?? @object)),
                            accessibility: Accessibility.Public,
                            statements: throwNotImplementedStatements));
                        break;
                    default:
                        break;
                }
            }

            return updatedDeclaration;
        }
    }
}
