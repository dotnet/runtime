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
                nameof(StringMarshalling),
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

            // Nothing to do if GeneratedDllImportAttribute is not in the compilation
            INamedTypeSymbol? generatedDllImportAttrType = model.Compilation.GetTypeByMetadataName(TypeNames.GeneratedDllImportAttribute);
            if (generatedDllImportAttrType == null)
                return;

            // Nothing to do if DllImportAttribute is not in the compilation
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
                        entryPointSuffix: null,
                        cancelToken),
                    equivalenceKey: ConvertToGeneratedDllImportKey),
                context.Diagnostics);

            DllImportData dllImportData = methodSymbol.GetDllImportData()!;
            if (!dllImportData.ExactSpelling)
            {
                // CharSet.Auto traditionally maps to either an A or W suffix
                // depending on the default CharSet of the platform.
                // We will offer both suffix options when CharSet.Auto is provided
                // to enable developers to pick which variant they mean (since they could explicitly decide they want one or the other)
                if (dllImportData.CharacterSet is CharSet.None or CharSet.Ansi or CharSet.Auto)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format(Resources.ConvertToGeneratedDllImportWithSuffix, "A"),
                            cancelToken => ConvertToGeneratedDllImport(
                                context.Document,
                                methodSyntax,
                                methodSymbol,
                                dllImportAttr!,
                                generatedDllImportAttrType,
                                entryPointSuffix: 'A',
                                cancelToken),
                            equivalenceKey: ConvertToGeneratedDllImportKey + "A"),
                        context.Diagnostics);
                }
                if (dllImportData.CharacterSet is CharSet.Unicode or CharSet.Auto)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            string.Format(Resources.ConvertToGeneratedDllImportWithSuffix, "W"),
                            cancelToken => ConvertToGeneratedDllImport(
                                context.Document,
                                methodSyntax,
                                methodSymbol,
                                dllImportAttr!,
                                generatedDllImportAttrType,
                                entryPointSuffix: 'W',
                                cancelToken),
                            equivalenceKey: ConvertToGeneratedDllImportKey + "W"),
                        context.Diagnostics);
                }
            }
        }

        private async Task<Document> ConvertToGeneratedDllImport(
            Document doc,
            MethodDeclarationSyntax methodSyntax,
            IMethodSymbol methodSymbol,
            AttributeData dllImportAttr,
            INamedTypeSymbol generatedDllImportAttrType,
            char? entryPointSuffix,
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
                methodSymbol,
                generatedDllImportAttrType,
                entryPointSuffix,
                out SyntaxNode? unmanagedCallConvAttributeMaybe);

            // Add annotation about potential behavioural and compatibility changes
            generatedDllImportSyntax = generatedDllImportSyntax.WithAdditionalAnnotations(
                WarningAnnotation.Create(string.Format(Resources.ConvertToGeneratedDllImportWarning, "[TODO] Documentation link")));

            // Replace DllImport with GeneratedDllImport
            SyntaxNode generatedDeclaration = generator.ReplaceNode(methodSyntax, dllImportSyntax, generatedDllImportSyntax);

            if (!methodSymbol.MethodImplementationFlags.HasFlag(System.Reflection.MethodImplAttributes.PreserveSig))
            {
                generatedDeclaration = await RemoveNoPreserveSigTransform(editor, generatedDeclaration, methodSymbol, cancellationToken).ConfigureAwait(false);
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

            // Replace the original method with the updated one
            editor.ReplaceNode(methodSyntax, generatedDeclaration);

            return editor.GetChangedDocument();
        }

        private async Task<SyntaxNode> RemoveNoPreserveSigTransform(DocumentEditor editor, SyntaxNode generatedDeclaration, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            Document? document = editor.OriginalDocument;
            IEnumerable<ReferencedSymbol>? referencedSymbols = await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Sometimes we can't validate that we've fixed all callers, so we warn the user that this fix might produce invalid code.
            bool shouldWarn = false;

            List<(SyntaxNode invocation, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> action)> invocations = new();

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
                        invocations.Add((invocation, WrapInvocationWithHRExceptionThrow));
                    }
                    else if (invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        // The return value isn't used, so discard the new out parameter value
                        invocations.Add((invocation,
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
                        ));
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
                        invocations.Add((declaration,
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
                        ));
                    }
                    else if (invocation.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression) && invocation.Parent.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        invocations.Add((invocation.Parent,
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
                        ));
                    }
                    else
                    {
                        shouldWarn = true;
                    }
                }
            }

            foreach ((SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> action) nodesWithReplaceAction in invocations)
            {
                editor.ReplaceNode(
                    nodesWithReplaceAction.node, (node, generator) =>
                    {
                        return nodesWithReplaceAction.action(node, generator);
                    });
            }

            SyntaxNode noPreserveSigDeclaration = editor.Generator.WithType(
                generatedDeclaration,
                editor.Generator.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)));

            if (!methodSymbol.ReturnsVoid)
            {
                noPreserveSigDeclaration = editor.Generator.AddParameters(
                    noPreserveSigDeclaration,
                    new[]
                    {
                        editor.Generator.ParameterDeclaration("@return", editor.Generator.GetType(generatedDeclaration), refKind: RefKind.Out)
                    });
            }

            if (shouldWarn)
            {
                noPreserveSigDeclaration = noPreserveSigDeclaration.WithAdditionalAnnotations(WarningAnnotation.Create(Resources.ConvertNoPreserveSigDllImportToGeneratedMayProduceInvalidCode));
            }
            return noPreserveSigDeclaration;

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

        private SyntaxNode GetGeneratedDllImportAttribute(
            DocumentEditor editor,
            SyntaxGenerator generator,
            AttributeSyntax dllImportSyntax,
            IMethodSymbol methodSymbol,
            INamedTypeSymbol generatedDllImportAttrType,
            char? entryPointSuffix,
            out SyntaxNode? unmanagedCallConvAttributeMaybe)
        {
            unmanagedCallConvAttributeMaybe = null;

            DllImportData dllImportData = methodSymbol.GetDllImportData()!;
            string methodName = methodSymbol.Name;

            // Create GeneratedDllImport based on the DllImport attribute
            SyntaxNode generatedDllImportSyntax = generator.ReplaceNode(dllImportSyntax,
                dllImportSyntax.Name,
                generator.TypeExpression(generatedDllImportAttrType));

            // Update attribute arguments for GeneratedDllImport
            bool hasEntryPointAttributeArgument = false;
            List<SyntaxNode> argumentsToAdd= new List<SyntaxNode>();
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

            generatedDllImportSyntax = generator.RemoveNodes(generatedDllImportSyntax, argumentsToRemove);
            generatedDllImportSyntax = generator.AddAttributeArguments(generatedDllImportSyntax, argumentsToAdd);
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
