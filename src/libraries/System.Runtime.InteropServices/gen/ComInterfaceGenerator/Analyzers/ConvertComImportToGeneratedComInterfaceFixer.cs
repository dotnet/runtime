// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class ConvertComImportToGeneratedComInterfaceFixer : ConvertToSourceGeneratedInteropFixer
    {
        private const string AddStringMarshallingOption = nameof(AddStringMarshallingOption);

        protected override string BaseEquivalenceKey => "ConvertToGeneratedComInterface";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerDiagnostics.Ids.ConvertToGeneratedComInterface);

        protected override string GetDiagnosticTitle(ImmutableDictionary<string, Option> selectedOptions)
        {
            return selectedOptions.TryGetValue(Option.AllowUnsafe, out Option allowUnsafeOption) && allowUnsafeOption is Option.Bool(true)
                ? SR.ConvertToGeneratedComInterfaceAddUnsafe
                : SR.ConvertToGeneratedComInterfaceTitle;
        }

        protected override Func<DocumentEditor, CancellationToken, Task> CreateFixForSelectedOptions(SyntaxNode node, ImmutableDictionary<string, Option> selectedOptions)
        {
            bool mayRequireAdditionalWork = selectedOptions.TryGetValue(Option.MayRequireAdditionalWork, out Option mayRequireAdditionalWorkOption) && mayRequireAdditionalWorkOption is Option.Bool(true);
            bool addStringMarshalling = selectedOptions.TryGetValue(AddStringMarshallingOption, out Option addStringMarshallingOption) && addStringMarshallingOption is Option.Bool(true);

            return (editor, ct) => ConvertComImportToGeneratedComInterfaceAsync(editor, node, mayRequireAdditionalWork, addStringMarshalling, ct);
        }

        protected override ImmutableDictionary<string, Option> ParseOptionsFromDiagnostic(Diagnostic diagnostic)
        {
            var optionsBuilder = ImmutableDictionary.CreateBuilder<string, Option>();
            // Only add the bool options if they are true. This simplifies our equivalence key and makes testing easier.
            if (diagnostic.Properties.TryGetValue(AnalyzerDiagnostics.Metadata.MayRequireAdditionalWork, out string? mayRequireAdditionalWork) && bool.Parse(mayRequireAdditionalWork))
            {
                optionsBuilder.Add(Option.MayRequireAdditionalWork, new Option.Bool(true));
            }
            if (diagnostic.Properties.TryGetValue(AnalyzerDiagnostics.Metadata.AddStringMarshalling, out string? addStringMarshalling) && bool.Parse(addStringMarshalling))
            {
                optionsBuilder.Add(AddStringMarshallingOption, new Option.Bool(true));
            }
            return optionsBuilder.ToImmutable();
        }

        protected override ImmutableDictionary<string, Option> CombineOptions(ImmutableDictionary<string, Option> fixAllOptions, ImmutableDictionary<string, Option> diagnosticOptions)
        {
            ImmutableDictionary<string, Option> combinedOptions = fixAllOptions;
            if (fixAllOptions.TryGetValue(AddStringMarshallingOption, out Option fixAllAddStringMarshallingOption)
                && fixAllAddStringMarshallingOption is Option.Bool(true)
                && (!diagnosticOptions.TryGetValue(AddStringMarshallingOption, out Option addStringMarshallingOption)
                    || addStringMarshallingOption is Option.Bool(false)))
            {
                combinedOptions = combinedOptions.Remove(AddStringMarshallingOption);
            }

            return combinedOptions;
        }

        private static async Task ConvertComImportToGeneratedComInterfaceAsync(DocumentEditor editor, SyntaxNode node, bool mayRequireAdditionalWork, bool addStringMarshalling, CancellationToken ct)
        {
            var gen = editor.Generator;
            var comp = editor.SemanticModel.Compilation;
            var declaringType = editor.SemanticModel.GetDeclaredSymbol(node, ct);

            var generatedComInterfaceAttribute = gen.Attribute(gen.TypeExpression(comp.GetTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute)).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation));

            if (addStringMarshalling)
            {
                generatedComInterfaceAttribute = gen.AddAttributeArguments(
                    generatedComInterfaceAttribute,
                    new[]
                    {
                       gen.AttributeArgument("StringMarshalling", gen.MemberAccessExpression(gen.DottedName(TypeNames.StringMarshalling), gen.IdentifierName(nameof(StringMarshalling.Custom)))),
                       gen.AttributeArgument("StringMarshallingCustomType", gen.TypeOfExpression(gen.TypeExpression(comp.GetTypeByMetadataName(TypeNames.BStrStringMarshaller))))
                    });
            }

            if (mayRequireAdditionalWork)
            {
                generatedComInterfaceAttribute = generatedComInterfaceAttribute.WithAdditionalAnnotations(
                    WarningAnnotation.Create(SR.ConvertComInterfaceMayProduceInvalidCode));
            }

            var comImportAttributeType = comp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ComImportAttribute);

            var comImportAttribute = await declaringType.GetAttributes().First(attr => attr.AttributeClass.Equals(comImportAttributeType, SymbolEqualityComparer.Default)).ApplicationSyntaxReference.GetSyntaxAsync(ct).ConfigureAwait(false);

            editor.ReplaceNode(comImportAttribute, generatedComInterfaceAttribute);

            foreach (var member in gen.GetMembers(node))
            {
                if (gen.GetDeclarationKind(member) != DeclarationKind.Method)
                {
                    continue;
                }

                var declarationModifiers = gen.GetModifiers(member);

                if (declarationModifiers.IsStatic)
                {
                    continue;
                }

                if (declarationModifiers.IsNew)
                {
                    // If this is a shadowing method, then we remove it.
                    // TODO: Do we want to be smarter here and try to match the number of methods to a base interface, etc.?
                    editor.RemoveNode(member);
                    continue;
                }

                IMethodSymbol method = (IMethodSymbol)editor.SemanticModel.GetDeclaredSymbol(member, ct);
                var generatedDeclaration = member;

                generatedDeclaration = AddExplicitDefaultBoolMarshalling(gen, method, generatedDeclaration, "VariantBool");

                if (method.MethodImplementationFlags.HasFlag(MethodImplAttributes.PreserveSig))
                {
                    generatedDeclaration = AddHResultStructAsErrorMarshalling(gen, method, generatedDeclaration);
                }

                editor.ReplaceNode(member, generatedDeclaration);
            }

            editor.ReplaceNode(node, (node, gen) => gen.WithModifiers(node, gen.GetModifiers(node).WithPartial(true)));

            MakeNodeParentsPartial(editor, node);
        }
    }
}
