// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
using Microsoft.CodeAnalysis.FindSymbols;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class ConvertToLibraryImportFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Ids.ConvertToLibraryImport);

        public override FixAllProvider GetFixAllProvider() => CustomFixAllProvider.Instance;

        private const string ConvertToLibraryImportKey = "ConvertToLibraryImport";
        private const string ConvertToLibraryImportWithASuffixKey = "ConvertToLibraryImportA";
        private const string ConvertToLibraryImportWithWSuffixKey = "ConvertToLibraryImportW";

        private static readonly string[] s_preferredAttributeArgumentOrder =
            {
                nameof(DllImportAttribute.EntryPoint),
                nameof(DllImportAttribute.BestFitMapping),
                nameof(DllImportAttribute.CallingConvention),
                nameof(DllImportAttribute.CharSet),
                nameof(DllImportAttribute.ExactSpelling),
                nameof(DllImportAttribute.PreserveSig),
                nameof(DllImportAttribute.SetLastError),
                nameof(StringMarshalling),
                nameof(DllImportAttribute.ThrowOnUnmappableChar)
            };

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root and semantic model
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            // Get the syntax node tied to the diagnostic and check that it is a method declaration
            if (root.FindNode(context.Span) is not MethodDeclarationSyntax methodSyntax)
                return;

            // Register code fix
            context.RegisterCodeFix(
                CodeAction.Create(
                    SR.ConvertToLibraryImport,
                    cancelToken => ConvertToLibraryImport(
                        context.Document,
                        methodSyntax,
                        entryPointSuffix: null,
                        cancelToken),
                    equivalenceKey: ConvertToLibraryImportKey),
                context.Diagnostics);

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                if (!bool.Parse(diagnostic.Properties[ConvertToLibraryImportAnalyzer.ExactSpelling]))
                {
                    CharSet charSet = (CharSet)Enum.Parse(typeof(CharSet), diagnostic.Properties[ConvertToLibraryImportAnalyzer.CharSet]);
                    // CharSet.Auto traditionally maps to either an A or W suffix
                    // depending on the default CharSet of the platform.
                    // We will offer both suffix options when CharSet.Auto is provided
                    // to enable developers to pick which variant they mean (since they could explicitly decide they want one or the other)
                    if (charSet is CharSet.None or CharSet.Ansi or CharSet.Auto)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                string.Format(SR.ConvertToLibraryImportWithSuffix, "A"),
                                cancelToken => ConvertToLibraryImport(
                                    context.Document,
                                    methodSyntax,
                                    entryPointSuffix: 'A',
                                    cancelToken),
                                equivalenceKey: ConvertToLibraryImportWithASuffixKey),
                            context.Diagnostics);
                    }
                    if (charSet is CharSet.Unicode or CharSet.Auto)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                string.Format(SR.ConvertToLibraryImportWithSuffix, "W"),
                                cancelToken => ConvertToLibraryImport(
                                    context.Document,
                                    methodSyntax,
                                    entryPointSuffix: 'W',
                                    cancelToken),
                                equivalenceKey: ConvertToLibraryImportWithWSuffixKey),
                            context.Diagnostics);
                    }
                }
            }
        }

        private class CustomFixAllProvider : DocumentBasedFixAllProvider
        {
            public static readonly CustomFixAllProvider Instance = new();

            protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                DocumentEditor editor = await DocumentEditor.CreateAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);
                SyntaxGenerator generator = editor.Generator;

                SyntaxNode? root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                if (root == null)
                    return document;

                foreach (Diagnostic diagnostic in diagnostics)
                {
                    // Get the syntax node tied to the diagnostic and check that it is a method declaration
                    if (root.FindNode(diagnostic.Location.SourceSpan) is not MethodDeclarationSyntax methodSyntax)
                        continue;
                    if (editor.SemanticModel.GetDeclaredSymbol(methodSyntax, fixAllContext.CancellationToken) is not IMethodSymbol methodSymbol)
                        continue;

                    SyntaxNode generatedDeclaration = await ConvertMethodDeclarationToLibraryImport(methodSyntax, editor, generator, methodSymbol, GetSuffixFromEquivalenceKey(fixAllContext.CodeActionEquivalenceKey), fixAllContext.CancellationToken).ConfigureAwait(false);

                    if (!methodSymbol.MethodImplementationFlags.HasFlag(System.Reflection.MethodImplAttributes.PreserveSig))
                    {
                        bool shouldWarn = await TransformCallersOfNoPreserveSigMethod(editor, methodSymbol, fixAllContext.CancellationToken).ConfigureAwait(false);
                        if (shouldWarn)
                        {
                            generatedDeclaration = generatedDeclaration.WithAdditionalAnnotations(WarningAnnotation.Create(SR.ConvertNoPreserveSigDllImportToGeneratedMayProduceInvalidCode));
                        }
                    }

                    // Replace the original method with the updated one
                    editor.ReplaceNode(methodSyntax, generatedDeclaration);

                    MakeEnclosingTypesPartial(editor, methodSyntax);
                }

                return editor.GetChangedDocument();
            }

            private static char? GetSuffixFromEquivalenceKey(string equivalenceKey) => equivalenceKey switch
            {
                ConvertToLibraryImportWithASuffixKey => 'A',
                ConvertToLibraryImportWithWSuffixKey => 'W',
                _ => null
            };
        }

        private static async Task<Document> ConvertToLibraryImport(
            Document doc,
            MethodDeclarationSyntax methodSyntax,
            char? entryPointSuffix,
            CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);

            SyntaxGenerator generator = editor.Generator;

            if (editor.SemanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken) is not IMethodSymbol methodSymbol)
                return doc;

            SyntaxNode generatedDeclaration = await ConvertMethodDeclarationToLibraryImport(methodSyntax, editor, generator, methodSymbol, entryPointSuffix, cancellationToken).ConfigureAwait(false);

            if (!methodSymbol.MethodImplementationFlags.HasFlag(System.Reflection.MethodImplAttributes.PreserveSig))
            {
                bool shouldWarn = await TransformCallersOfNoPreserveSigMethod(editor, methodSymbol, cancellationToken).ConfigureAwait(false);
                if (shouldWarn)
                {
                    generatedDeclaration = generatedDeclaration.WithAdditionalAnnotations(WarningAnnotation.Create(SR.ConvertNoPreserveSigDllImportToGeneratedMayProduceInvalidCode));
                }
            }

            // Replace the original method with the updated one
            editor.ReplaceNode(methodSyntax, generatedDeclaration);

            MakeEnclosingTypesPartial(editor, methodSyntax);

            return editor.GetChangedDocument();
        }

        private static async Task<SyntaxNode> ConvertMethodDeclarationToLibraryImport(
            MethodDeclarationSyntax methodSyntax,
            DocumentEditor editor,
            SyntaxGenerator generator,
            IMethodSymbol methodSymbol,
            char? entryPointSuffix,
            CancellationToken cancellationToken)
        {
            INamedTypeSymbol? dllImportAttrType = editor.SemanticModel.Compilation.GetTypeByMetadataName(typeof(DllImportAttribute).FullName);
            if (dllImportAttrType == null)
                return methodSyntax;

            // We wouldn't have offered this code fix if the LibraryImport type isn't available, so we can be sure it isn't null here.
            INamedTypeSymbol libraryImportAttrType = editor.SemanticModel.Compilation.GetTypeByMetadataName(TypeNames.LibraryImportAttribute)!;

            // Make sure the method has the DllImportAttribute
            if (!TryGetAttribute(methodSymbol, dllImportAttrType, out AttributeData? dllImportAttr))
                return methodSyntax;

            var dllImportSyntax = (AttributeSyntax)await dllImportAttr!.ApplicationSyntaxReference!.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            // Create LibraryImport attribute based on the DllImport attribute
            SyntaxNode libraryImportSyntax = GetLibraryImportAttribute(
                editor,
                generator,
                dllImportSyntax,
                methodSymbol,
                libraryImportAttrType,
                entryPointSuffix,
                out SyntaxNode? unmanagedCallConvAttributeMaybe);

            // Add annotation about potential behavioural and compatibility changes
            libraryImportSyntax = libraryImportSyntax.WithAdditionalAnnotations(
                WarningAnnotation.Create(string.Format(SR.ConvertToLibraryImportWarning, "[TODO] Documentation link")));

            // Replace DllImport with LibraryImport
            SyntaxNode generatedDeclaration = generator.ReplaceNode(methodSyntax, dllImportSyntax, libraryImportSyntax);
            if (!methodSymbol.MethodImplementationFlags.HasFlag(System.Reflection.MethodImplAttributes.PreserveSig))
            {
                if (!methodSymbol.ReturnsVoid)
                {
                    generatedDeclaration = editor.Generator.AddParameters(
                        generatedDeclaration,
                        new[]
                        {
                        editor.Generator.ParameterDeclaration("@return", editor.Generator.GetType(generatedDeclaration), refKind: RefKind.Out)
                        });
                }

                generatedDeclaration = editor.Generator.WithType(
                    generatedDeclaration,
                    editor.Generator.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)));
            }

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

            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.SpecialType == SpecialType.System_Boolean
                    && !parameter.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                {
                    MethodDeclarationSyntax generatedDeclarationSyntax = (MethodDeclarationSyntax)generatedDeclaration;
                    ParameterSyntax generatedParameterSyntax = generatedDeclarationSyntax.ParameterList.Parameters[parameter.Ordinal];
                    generatedDeclaration = generator.ReplaceNode(generatedDeclaration, generatedParameterSyntax, generator.AddAttributes(generatedParameterSyntax,
                                    GenerateMarshalAsUnmanagedTypeBoolAttribute(generator)));
                }
            }

            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean
                && !methodSymbol.GetReturnTypeAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
            {
                generatedDeclaration = generator.AddReturnAttributes(generatedDeclaration,
                    GenerateMarshalAsUnmanagedTypeBoolAttribute(generator));
            }

            return generatedDeclaration;
        }

        private static SyntaxNode GenerateMarshalAsUnmanagedTypeBoolAttribute(SyntaxGenerator generator)
         => generator.Attribute(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute,
             generator.AttributeArgument(
                 generator.MemberAccessExpression(
                     generator.DottedName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                     generator.IdentifierName("Bool"))));

        private static void MakeEnclosingTypesPartial(DocumentEditor editor, MethodDeclarationSyntax method)
        {
            for (TypeDeclarationSyntax? typeDecl = method.FirstAncestorOrSelf<TypeDeclarationSyntax>(); typeDecl is not null; typeDecl = typeDecl.Parent.FirstAncestorOrSelf<TypeDeclarationSyntax>())
            {
                editor.ReplaceNode(typeDecl, (node, generator) => generator.WithModifiers(node, generator.GetModifiers(node).WithPartial(true)));
            }
        }

        private static async Task<bool> TransformCallersOfNoPreserveSigMethod(DocumentEditor editor, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            Document? document = editor.OriginalDocument;
            IEnumerable<ReferencedSymbol>? referencedSymbols = await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Sometimes we can't validate that we've fixed all callers, so we warn the user that this fix might produce invalid code.
            bool shouldWarn = false;

            foreach (ReferencedSymbol? referencedSymbol in referencedSymbols)
            {
                foreach (ReferenceLocation location in referencedSymbol.Locations)
                {
                    if (!location.Document.Id.Equals(document.Id))
                    {
                        shouldWarn = true;
                        continue;
                    }
                    // We limited the search scope to the single document,
                    // so all reference should be in the same tree.
                    SyntaxNode? referenceNode = root.FindNode(location.Location.SourceSpan);
                    if (referenceNode is not IdentifierNameSyntax identifierNode)
                    {
                        // Unexpected scenario, skip and warn.
                        shouldWarn = true;
                        continue;
                    }

                    InvocationExpressionSyntax? invocation = identifierNode switch
                    {
                        { Parent: InvocationExpressionSyntax invocationInScope } => invocationInScope,
                        { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocationOnType } } => invocationOnType,
                        _ => null!
                    };

                    if (invocation is null)
                    {
                        // We won't be able to fix non-invocation references,
                        // e.g. creating a delegate.
                        shouldWarn = true;
                        continue;
                    }

                    if (methodSymbol.ReturnsVoid)
                    {
                        // There is no return value, so we don't need to add any arguments to the invocation.
                        // We only need to wrap the invocation with a call to ThrowExceptionForHR
                        editor.ReplaceNode(invocation, WrapInvocationWithHRExceptionThrow);
                    }
                    else if (invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        // The return value isn't used, so discard the new out parameter value
                        editor.ReplaceNode(invocation,
                           (node, generator) =>
                           {
                               return WrapInvocationWithHRExceptionThrow(
                                   ((InvocationExpressionSyntax)node).AddArgumentListArguments(
                                       SyntaxFactory.Argument(SyntaxFactory.IdentifierName(
                                           SyntaxFactory.Identifier(
                                               SyntaxFactory.TriviaList(),
                                               SyntaxKind.UnderscoreToken,
                                               "_",
                                               "_",
                                               SyntaxFactory.TriviaList())))
                                           .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))),
                                   generator);
                           }
                        );
                    }
                    else if (invocation.Parent.IsKind(SyntaxKind.EqualsValueClause))
                    {
                        LocalDeclarationStatementSyntax declaration = invocation.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
                        if (declaration.IsKind(SyntaxKind.FieldDeclaration) || declaration.IsKind(SyntaxKind.EventFieldDeclaration))
                        {
                            // We can't fix initalizations without introducing or prepending to a static constructor
                            // for what is an unlikely scenario.
                            continue;
                        }
                        if (declaration.Declaration.Variables.Count != 1)
                        {
                            // We can't handle multiple variable initializations easily
                            continue;
                        }
                        // The result was used to initialize a variable,
                        // so initialize the variable inline
                        editor.ReplaceNode(declaration,
                           (node, generator) =>
                           {
                               var declaration = (LocalDeclarationStatementSyntax)node;
                               var invocation = (InvocationExpressionSyntax)declaration.Declaration.Variables[0].Initializer.Value;
                               return generator.ExpressionStatement(
                                   WrapInvocationWithHRExceptionThrow(
                                       invocation.AddArgumentListArguments(
                                           SyntaxFactory.Argument(SyntaxFactory.DeclarationExpression(
                                            declaration.Declaration.Type,
                                            SyntaxFactory.SingleVariableDesignation(
                                                declaration.Declaration.Variables[0].Identifier.WithoutTrivia())))
                                               .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))),
                                               generator));
                           }
                        );
                    }
                    else if (invocation.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression) && invocation.Parent.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        editor.ReplaceNode(invocation.Parent,
                           (node, generator) =>
                           {
                               var assignment = (AssignmentExpressionSyntax)node;
                               var invocation = (InvocationExpressionSyntax)assignment.Right;
                               return WrapInvocationWithHRExceptionThrow(
                                   invocation.AddArgumentListArguments(
                                       SyntaxFactory.Argument(generator.ClearTrivia(assignment.Left))
                                           .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))),
                                   generator);
                           }
                        );
                    }
                    else
                    {
                        shouldWarn = true;
                    }
                }
            }

            return shouldWarn;

            SyntaxNode WrapInvocationWithHRExceptionThrow(SyntaxNode node, SyntaxGenerator generator)
            {
                return generator.InvocationExpression(
                            generator.MemberAccessExpression(
                                generator.NameExpression(
                                    editor.SemanticModel.Compilation.GetTypeByMetadataName(
                                        TypeNames.System_Runtime_InteropServices_Marshal)),
                                "ThrowExceptionForHR"),
                            node);
            }
        }

        private static SyntaxNode GetLibraryImportAttribute(
            DocumentEditor editor,
            SyntaxGenerator generator,
            AttributeSyntax dllImportSyntax,
            IMethodSymbol methodSymbol,
            INamedTypeSymbol libraryImportAttrType,
            char? entryPointSuffix,
            out SyntaxNode? unmanagedCallConvAttributeMaybe)
        {
            unmanagedCallConvAttributeMaybe = null;

            DllImportData dllImportData = methodSymbol.GetDllImportData()!;
            string methodName = methodSymbol.Name;

            // Create LibraryImport based on the DllImport attribute
            SyntaxNode libraryImportSyntax = generator.ReplaceNode(dllImportSyntax,
                dllImportSyntax.Name,
                generator.TypeExpression(libraryImportAttrType));

            // Update attribute arguments for LibraryImport
            bool hasEntryPointAttributeArgument = false;
            List<SyntaxNode> argumentsToAdd= new List<SyntaxNode>();
            List<SyntaxNode> argumentsToRemove = new List<SyntaxNode>();
            foreach (SyntaxNode argument in generator.GetAttributeArguments(libraryImportSyntax))
            {
                if (argument is not AttributeArgumentSyntax attrArg)
                    continue;

                if (dllImportData.BestFitMapping != null
                    && !dllImportData.BestFitMapping.Value
                    && IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.BestFitMapping)))
                {
                    // BestFitMapping=false is explicitly set
                    // LibraryImport does not support setting BestFitMapping. The generated code
                    // has the equivalent behaviour of BestFitMapping=false, so we can remove the argument.
                    argumentsToRemove.Add(argument);
                }
                else if (IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.CharSet)))
                {
                    if (MethodRequiresStringMarshalling(methodSymbol))
                    {
                        // For Unicode, we can translate the argument to StringMarshalling.Utf16
                        // TODO: Handle ANSI once we have a public marshaller type for ANSI strings that we can use with StringMarshallerCustomType
                        if (dllImportData.CharacterSet == CharSet.Unicode)
                        {
                            ITypeSymbol stringMarshallingType = editor.SemanticModel.Compilation.GetTypeByMetadataName(TypeNames.StringMarshalling)!;
                            argumentsToAdd.Add(generator.AttributeArgument(
                                nameof(StringMarshalling),
                                generator.MemberAccessExpression(
                                    generator.TypeExpression(stringMarshallingType),
                                    generator.IdentifierName(nameof(StringMarshalling.Utf16)))));
                        }
                    }

                    argumentsToRemove.Add(argument);
                }
                else if (dllImportData.ThrowOnUnmappableCharacter != null
                    && !dllImportData.ThrowOnUnmappableCharacter.Value
                    && IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.ThrowOnUnmappableChar)))
                {
                    // ThrowOnUnmappableChar=false is explicitly set
                    // LibraryImport does not support setting ThrowOnUnmappableChar. The generated code
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
                else if (IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.ExactSpelling)))
                {
                    argumentsToRemove.Add(argument);
                }
                else if (IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.EntryPoint)))
                {
                    hasEntryPointAttributeArgument = true;
                    if (!dllImportData.ExactSpelling && entryPointSuffix.HasValue)
                    {
                        if (attrArg.Expression.IsKind(SyntaxKind.StringLiteralExpression))
                        {
                            string? entryPoint = (string?)((LiteralExpressionSyntax)attrArg.Expression).Token.Value;
                            if (entryPoint is not null)
                            {
                                argumentsToRemove.Add(attrArg);
                                argumentsToAdd.Add(attrArg.WithExpression(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(entryPoint + entryPointSuffix))));
                            }
                        }
                        else
                        {
                            argumentsToRemove.Add(attrArg);
                            argumentsToAdd.Add(attrArg.WithExpression(
                                SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                                    attrArg.Expression,
                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(entryPointSuffix.ToString())))));
                        }
                    }
                }
                else if (IsMatchingNamedArg(attrArg, nameof(DllImportAttribute.PreserveSig)))
                {
                    // We transform the signature for PreserveSig, so we can remove the argument
                    argumentsToRemove.Add(argument);
                }
            }

            if (entryPointSuffix.HasValue && !hasEntryPointAttributeArgument)
            {
                argumentsToAdd.Add(generator.AttributeArgument("EntryPoint",
                    generator.LiteralExpression(methodName + entryPointSuffix.Value)));
            }

            libraryImportSyntax = generator.RemoveNodes(libraryImportSyntax, argumentsToRemove);
            libraryImportSyntax = generator.AddAttributeArguments(libraryImportSyntax, argumentsToAdd);
            return SortDllImportAttributeArguments((AttributeSyntax)libraryImportSyntax, generator);
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

        private static bool TryCreateUnmanagedCallConvAttributeToEmit(
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

        private static bool MethodRequiresStringMarshalling(IMethodSymbol method)
        {
            foreach (IParameterSymbol param in method.Parameters)
            {
                if (param.Type.SpecialType is SpecialType.System_String or SpecialType.System_Char)
                {
                    return true;
                }
            }

            return method.ReturnType.SpecialType is SpecialType.System_String or SpecialType.System_Char;
        }
    }
}
