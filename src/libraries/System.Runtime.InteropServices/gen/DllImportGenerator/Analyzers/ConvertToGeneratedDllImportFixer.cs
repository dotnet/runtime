// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class ConvertToGeneratedDllImportFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Ids.ConvertToGeneratedDllImport);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private const string ConvertToGeneratedDllImportKey = "ConvertToGeneratedDllImport";

        private static readonly string[] s_preferredAttributeArgumentOrder =
            {
                nameof(DllImportAttribute.EntryPoint),
                nameof(DllImportAttribute.BestFitMapping),
                nameof(DllImportAttribute.CallingConvention),
                nameof(DllImportAttribute.CharSet),
                nameof(DllImportAttribute.ExactSpelling),
                nameof(DllImportAttribute.PreserveSig),
                nameof(DllImportAttribute.SetLastError),
                nameof(DllImportAttribute.ThrowOnUnmappableChar)
            };

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root and semantic model
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SemanticModel? model = await doc.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null || model == null)
                return;

            // Nothing to do if the GeneratedDllImportAttribute is not in the compilation
            INamedTypeSymbol? generatedDllImportAttrType = model.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
            if (generatedDllImportAttrType == null)
                return;

            INamedTypeSymbol? dllImportAttrType = model.Compilation.GetTypeByMetadataName(typeof(DllImportAttribute).FullName);
            if (dllImportAttrType == null)
                return;

            // Get the syntax node tied to the diagnostic and check that it is a method declaration
            if (root.FindNode(context.Span) is not MethodDeclarationSyntax methodSyntax)
                return;

            if (model.GetDeclaredSymbol(methodSyntax, context.CancellationToken) is not IMethodSymbol methodSymbol)
                return;

            // Make sure the method has the DllImportAttribute
            if (!TryGetAttribute(methodSymbol, dllImportAttrType, out AttributeData? dllImportAttr))
                return;

            // Register code fix
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.ConvertToGeneratedDllImport,
                    cancelToken => ConvertToGeneratedDllImport(
                        context.Document,
                        methodSyntax,
                        methodSymbol,
                        dllImportAttr!,
                        generatedDllImportAttrType,
                        cancelToken),
                    equivalenceKey: ConvertToGeneratedDllImportKey),
                context.Diagnostics);
        }

        private async Task<Document> ConvertToGeneratedDllImport(
            Document doc,
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol methodSymbol,
            AttributeData dllImportAttr,
            INamedTypeSymbol generatedDllImportAttrType,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            var dllImportSyntax = (AttributeSyntax)dllImportAttr!.ApplicationSyntaxReference!.GetSyntax(cancellationToken);

            // Create GeneratedDllImport attribute based on the DllImport attribute
            SyntaxNode generatedDllImportSyntax = GetGeneratedDllImportAttribute(
                editor,
                generator,
                dllImportSyntax,
                methodSymbol.GetDllImportData()!,
                generatedDllImportAttrType,
                out SyntaxNode? unmanagedCallConvAttributeMaybe);

            // Add annotation about potential behavioural and compatibility changes
            generatedDllImportSyntax = generatedDllImportSyntax.WithAdditionalAnnotations(
                WarningAnnotation.Create(string.Format(Resources.ConvertToGeneratedDllImportWarning, "[TODO] Documentation link")));

            // Replace DllImport with GeneratedDllImport
            SyntaxNode generatedDeclaration = generator.ReplaceNode(methodSyntax, dllImportSyntax, generatedDllImportSyntax);

            if (unmanagedCallConvAttributeMaybe is not null)
            {
                generatedDeclaration = generator.AddAttributes(generatedDeclaration, unmanagedCallConvAttributeMaybe);
            }

            // Replace extern keyword with partial keyword
            generatedDeclaration = generator.WithModifiers(
                generatedDeclaration,
                generator.GetModifiers(methodSyntax)
                    .WithIsExtern(false)
                    .WithPartial(true));

            // Replace the original method with the updated one
            editor.ReplaceNode(methodSyntax, generatedDeclaration);

            return editor.GetChangedDocument();
        }

        private SyntaxNode GetGeneratedDllImportAttribute(
            DocumentEditor editor,
            SyntaxGenerator generator,
            AttributeSyntax dllImportSyntax,
            DllImportData dllImportData,
            INamedTypeSymbol generatedDllImportAttrType,
            out SyntaxNode? unmanagedCallConvAttributeMaybe)
        {
            unmanagedCallConvAttributeMaybe = null;
            // Create GeneratedDllImport based on the DllImport attribute
            SyntaxNode generatedDllImportSyntax = generator.ReplaceNode(dllImportSyntax,
                dllImportSyntax.Name,
                generator.TypeExpression(generatedDllImportAttrType));

            // Update attribute arguments for GeneratedDllImport
            List<SyntaxNode> argumentsToRemove = new List<SyntaxNode>();
            foreach (SyntaxNode argument in generator.GetAttributeArguments(generatedDllImportSyntax))
            {
                if (argument is not AttributeArgumentSyntax attrArg)
                    continue;

                if (dllImportData.BestFitMapping != null
                    && !dllImportData.BestFitMapping.Value
                    && IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.BestFitMapping)))
                {
                    // BestFitMapping=false is explicitly set
                    // GeneratedDllImport does not support setting BestFitMapping. The generated code
                    // has the equivalent behaviour of BestFitMapping=false, so we can remove the argument.
                    argumentsToRemove.Add(argument);
                }
                else if (dllImportData.ThrowOnUnmappableCharacter != null
                    && !dllImportData.ThrowOnUnmappableCharacter.Value
                    && IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.ThrowOnUnmappableChar)))
                {
                    // ThrowOnUnmappableChar=false is explicitly set
                    // GeneratedDllImport does not support setting ThrowOnUnmappableChar. The generated code
                    // has the equivalent behaviour of ThrowOnUnmappableChar=false, so we can remove the argument.
                    argumentsToRemove.Add(argument);
                }
                else if (IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.CallingConvention)))
                {
                    if (TryCreateUnmanagedCallConvAttributeToEmit(
                        editor,
                        generator,
                        dllImportData.CallingConvention,
                        out unmanagedCallConvAttributeMaybe))
                    {
                        argumentsToRemove.Add(argument);
                    }
                }
            }

            generatedDllImportSyntax = generator.RemoveNodes(generatedDllImportSyntax, argumentsToRemove);
            return SortDllImportAttributeArguments((AttributeSyntax)generatedDllImportSyntax, generator);
        }

        private static SyntaxNode SortDllImportAttributeArguments(AttributeSyntax attribute, SyntaxGenerator generator)
        {
            AttributeArgumentListSyntax updatedArgList = attribute.ArgumentList.WithArguments(
                SyntaxFactory.SeparatedList(
                    attribute.ArgumentList.Arguments.OrderBy(arg =>
                    {
                        // Unnamed arguments first
                        if (arg.NameEquals == null)
                            return -1;

                        // Named arguments in specified order, followed by any named arguments with no preferred order
                        string name = arg.NameEquals.Name.Identifier.Text;
                        int index = System.Array.IndexOf(s_preferredAttributeArgumentOrder, name);
                        return index == -1 ? int.MaxValue : index;
                    })));
            return generator.ReplaceNode(attribute, attribute.ArgumentList, updatedArgList);
        }

        private bool TryCreateUnmanagedCallConvAttributeToEmit(
            DocumentEditor editor,
            SyntaxGenerator generator,
            CallingConvention callingConvention,
            out SyntaxNode? unmanagedCallConvAttribute)
        {
            if (editor.SemanticModel.Compilation.GetTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute) is null)
            {
                unmanagedCallConvAttribute = null;
                return false;
            }

            if (callingConvention == CallingConvention.Winapi)
            {
                // Winapi is the default, so we return true that we've created the attribute to emit,
                // but set the attribute-to-emit to null since we don't need to emit an attribute.
                unmanagedCallConvAttribute = null;
                return true;
            }

            ITypeSymbol? callingConventionType = callingConvention switch
            {
                CallingConvention.Cdecl => editor.SemanticModel.Compilation.ObjectType.ContainingAssembly.
                GetTypeByMetadataName($"System.Runtime.CompilerServices.CallConvCdecl"),
                CallingConvention.StdCall => editor.SemanticModel.Compilation.ObjectType.ContainingAssembly.
                GetTypeByMetadataName($"System.Runtime.CompilerServices.CallConvStdcall"),
                CallingConvention.ThisCall => editor.SemanticModel.Compilation.ObjectType.ContainingAssembly.
                GetTypeByMetadataName($"System.Runtime.CompilerServices.CallConvThiscall"),
                CallingConvention.FastCall => editor.SemanticModel.Compilation.ObjectType.ContainingAssembly.
                GetTypeByMetadataName($"System.Runtime.CompilerServices.CallConvFastcall"),
                _ => null
            };

            // The user is using a calling convention type that doesn't have a matching CallConv type.
            // There are no calling conventions like this, so we're already in a state that won't work at runtime.
            // Leave the value as-is for now and let the user handle this however they see fit.
            if (callingConventionType is null)
            {
                unmanagedCallConvAttribute = null;
                return false;
            }

            unmanagedCallConvAttribute = generator.Attribute(TypeNames.UnmanagedCallConvAttribute,
                generator.AttributeArgument("CallConvs",
                    generator.ArrayCreationExpression(
                        generator.TypeExpression(editor.SemanticModel.Compilation.GetTypeByMetadataName(TypeNames.System_Type)),
                        new[] { generator.TypeOfExpression(generator.TypeExpression(callingConventionType)) })));

            return true;
        }

        private static bool TryGetAttribute(IMethodSymbol method, INamedTypeSymbol attributeType, out AttributeData? attr)
        {
            attr = default;
            foreach (AttributeData attrLocal in method.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attrLocal.AttributeClass, attributeType))
                {
                    attr = attrLocal;
                    return true;
                }
            }

            return false;
        }

        private static bool IsMatchingNamedArg(AttributeArgumentSyntax arg, string nameToMatch)
        {
            return arg.NameEquals != null && arg.NameEquals.Name.Identifier.Text == nameToMatch;
        }
    }
}
