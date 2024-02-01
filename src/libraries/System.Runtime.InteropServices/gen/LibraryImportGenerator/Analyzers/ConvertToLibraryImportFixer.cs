// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class ConvertToLibraryImportFixer : ConvertToSourceGeneratedInteropFixer
    {
        private const string CharSetOption = nameof(CharSetOption);

        public const string SelectedSuffixOption = nameof(SelectedSuffixOption);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Ids.ConvertToLibraryImport);

        protected override string BaseEquivalenceKey => "ConvertToLibraryImport";

        protected override string GetDiagnosticTitle(ImmutableDictionary<string, Option> selectedOptions)
        {
            bool allowUnsafe = selectedOptions.TryGetValue(Option.AllowUnsafe, out Option? allowUnsafeOption) && allowUnsafeOption is Option.Bool(true);
            string? suffix = null;
            bool hasSuffix = false;
            if (selectedOptions.TryGetValue(SelectedSuffixOption, out Option? suffixOption) && suffixOption is Option.String(string suffixValue))
            {
                hasSuffix = true;
                suffix = suffixValue;
            }
            return (allowUnsafe, hasSuffix) switch
            {
                (true, true) => SR.Format(SR.ConvertToLibraryImportWithSuffixAddUnsafe, suffix),
                (true, false) => SR.ConvertToLibraryImportAddUnsafe,
                (false, true) => SR.Format(SR.ConvertToLibraryImportWithSuffix, suffix),
                (false, false) => SR.ConvertToLibraryImport
            };
        }

        protected override ImmutableDictionary<string, Option> ParseOptionsFromDiagnostic(Diagnostic diagnostic)
        {
            var optionsBuilder = ImmutableDictionary.CreateBuilder<string, Option>();
            // Only add the "May require additional work" option if it is true. This simplifies our equivalence key and makes testing easier.
            if (diagnostic.Properties.TryGetValue(ConvertToLibraryImportAnalyzer.MayRequireAdditionalWork, out string? mayRequireAdditionalWork) && bool.Parse(mayRequireAdditionalWork))
            {
                optionsBuilder.Add(Option.MayRequireAdditionalWork, new Option.Bool(true));
            }
            if (!bool.Parse(diagnostic.Properties[ConvertToLibraryImportAnalyzer.ExactSpelling]))
            {
                optionsBuilder.Add(CharSetOption, new Option.String(diagnostic.Properties[ConvertToLibraryImportAnalyzer.CharSet]));
            }
            return optionsBuilder.ToImmutable();
        }

        protected override IEnumerable<ConvertToSourceGeneratedInteropFix> CreateAllFixesForDiagnosticOptions(SyntaxNode node, ImmutableDictionary<string, Option> options)
        {
            bool warnForAdditionalWork = options.TryGetValue(Option.MayRequireAdditionalWork, out Option mayRequireAdditionalWork) && mayRequireAdditionalWork is Option.Bool(true);

            CharSet? charSet = options.TryGetValue(CharSetOption, out Option charSetOption) && charSetOption is Option.String(string charSetString) && Enum.TryParse<CharSet>(charSetString, out CharSet result) ? result : null;

            // We don't want the CharSet option contributing to the "selected options" set for the fix, so we remove it here.
            var selectedOptions = options.Remove(CharSetOption);

            yield return new ConvertToSourceGeneratedInteropFix(
                (editor, ct) =>
                    ConvertToLibraryImport(
                        editor,
                        node,
                        warnForAdditionalWork,
                        null,
                        ct),
                selectedOptions);

            if (charSet is not null)
            {

                // CharSet.Auto traditionally maps to either an A or W suffix
                // depending on the default CharSet of the platform.
                // We will offer both suffix options when CharSet.Auto is provided
                // to enable developers to pick which variant they mean (since they could explicitly decide they want one or the other)
                if (charSet is CharSet.None or CharSet.Ansi or CharSet.Auto)
                {
                    yield return new ConvertToSourceGeneratedInteropFix(
                        (editor, ct) =>
                            ConvertToLibraryImport(
                                editor,
                                node,
                                warnForAdditionalWork,
                                'A',
                                ct),
                        selectedOptions.Add(SelectedSuffixOption, new Option.String("A")));
                }
                if (charSet is CharSet.Unicode or CharSet.Auto)
                {
                    yield return new ConvertToSourceGeneratedInteropFix(
                        (editor, ct) =>
                            ConvertToLibraryImport(
                                editor,
                                node,
                                warnForAdditionalWork,
                                'W',
                                ct),
                        selectedOptions.Add(SelectedSuffixOption, new Option.String("W")));
                }
            }
        }

        protected override Func<DocumentEditor, CancellationToken, Task> CreateFixForSelectedOptions(SyntaxNode node, ImmutableDictionary<string, Option> selectedOptions)
        {
            bool warnForAdditionalWork = selectedOptions.TryGetValue(Option.MayRequireAdditionalWork, out Option mayRequireAdditionalWork) && mayRequireAdditionalWork is Option.Bool(true);
            char? suffix = selectedOptions.TryGetValue(SelectedSuffixOption, out Option selectedSuffixOption) && selectedSuffixOption is Option.String(string selectedSuffix) ? selectedSuffix[0] : null;
            return (editor, ct) =>
                ConvertToLibraryImport(
                    editor,
                    node,
                    warnForAdditionalWork,
                    suffix,
                    ct);
        }

        private static string AppendSuffix(string entryPoint, char? entryPointSuffix)
            => entryPointSuffix.HasValue && entryPoint.LastOrDefault() == entryPointSuffix.Value
                ? entryPoint
                : entryPoint + entryPointSuffix;

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

        private static async Task ConvertToLibraryImport(
            DocumentEditor editor,
            SyntaxNode methodSyntax,
            bool warnForAdditionalWork,
            char? entryPointSuffix,
            CancellationToken cancellationToken)
        {
            SyntaxGenerator generator = editor.Generator;

            if (editor.SemanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken) is not IMethodSymbol methodSymbol)
                return;

            SyntaxNode generatedDeclaration = await ConvertMethodDeclarationToLibraryImport(
                methodSyntax,
                editor,
                generator,
                methodSymbol,
                warnForAdditionalWork,
                entryPointSuffix,
                cancellationToken).ConfigureAwait(false);

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

            MakeNodeParentsPartial(editor, methodSyntax);
        }

        private static async Task<SyntaxNode> ConvertMethodDeclarationToLibraryImport(
            SyntaxNode methodSyntax,
            DocumentEditor editor,
            SyntaxGenerator generator,
            IMethodSymbol methodSymbol,
            bool warnForAdditionalWork,
            char? entryPointSuffix,
            CancellationToken cancellationToken)
        {
            INamedTypeSymbol? dllImportAttrType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.DllImportAttribute);
            if (dllImportAttrType == null)
                return methodSyntax;

            // We wouldn't have offered this code fix if the LibraryImport type isn't available, so we can be sure it isn't null here.
            INamedTypeSymbol libraryImportAttrType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.LibraryImportAttribute)!;

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
            if (warnForAdditionalWork)
            {
                libraryImportSyntax = libraryImportSyntax.WithAdditionalAnnotations(
                    WarningAnnotation.Create(SR.ConvertToLibraryImportMayRequireCustomMarshalling));
            }

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

            generatedDeclaration = AddExplicitDefaultBoolMarshalling(generator, methodSymbol, generatedDeclaration, "Bool");

            return generatedDeclaration;
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
                            // We can't fix initializations without introducing or prepending to a static constructor
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
                                    editor.SemanticModel.Compilation.GetBestTypeByMetadataName(
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
            List<SyntaxNode> argumentsToAdd = new List<SyntaxNode>();
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
                        if (dllImportData.CharacterSet == CharSet.Unicode || (dllImportData.CharacterSet == CharSet.Auto && entryPointSuffix is 'W' or null))
                        {
                            ITypeSymbol stringMarshallingType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.StringMarshalling)!;
                            argumentsToAdd.Add(generator.AttributeArgument(
                                nameof(StringMarshalling),
                                generator.MemberAccessExpression(
                                    generator.TypeExpression(stringMarshallingType),
                                    generator.IdentifierName(nameof(StringMarshalling.Utf16)))));
                        }
                        else if (dllImportData.CharacterSet == CharSet.Ansi || (dllImportData.CharacterSet == CharSet.Auto && entryPointSuffix == 'A'))
                        {
                            ITypeSymbol stringMarshallingType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.StringMarshalling)!;
                            argumentsToAdd.Add(generator.AttributeArgument(
                                nameof(StringMarshalling),
                                generator.MemberAccessExpression(
                                    generator.TypeExpression(stringMarshallingType),
                                    generator.IdentifierName(nameof(StringMarshalling.Custom)))));
                            argumentsToAdd.Add(generator.AttributeArgument(
                                "StringMarshallingCustomType",
                                generator.TypeOfExpression(generator.TypeExpression(editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.AnsiStringMarshaller)))));
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
                        dllImportSyntax.SyntaxTree,
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
                                        SyntaxFactory.Literal(AppendSuffix(entryPoint, entryPointSuffix)))));
                            }
                        }
                        else
                        {
                            if (dllImportData.EntryPointName!.LastOrDefault() != entryPointSuffix.Value)
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
                    generator.LiteralExpression(AppendSuffix(methodName, entryPointSuffix.Value))));
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
            SyntaxTree documentSyntaxTree,
            SyntaxGenerator generator,
            CallingConvention callingConvention,
            out SyntaxNode? unmanagedCallConvAttribute)
        {
            if (editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.UnmanagedCallConvAttribute) is null)
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

            // By default we always prefer collection expressions for C# 12 and above
            bool useCollectionExpression = ((CSharpParseOptions)documentSyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp12;

            AnalyzerConfigOptions options = editor.OriginalDocument.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(documentSyntaxTree);

            if (options.TryGetValue("dotnet_style_prefer_collection_expression", out string? preferCollectionExpressionsRule))
            {
                // Option may be declared with `value:severity` syntax. We don't need severity, so just extract the `value` from the whole string
                int indexOfColon = preferCollectionExpressionsRule.IndexOf(':');
                if (indexOfColon > -1)
                {
                    preferCollectionExpressionsRule = preferCollectionExpressionsRule.Substring(0, indexOfColon);
                }

                if (preferCollectionExpressionsRule is "false" or "never")
                {
                    // User explicitly specified that he doesn't prefer collection expressions
                    useCollectionExpression = false;
                }
            }

            ExpressionSyntax typeOfExpression = (ExpressionSyntax)generator.TypeOfExpression(generator.TypeExpression(callingConventionType));

            SyntaxNode argumentValue = useCollectionExpression
                ? SyntaxFactory.CollectionExpression(
                    SyntaxFactory.SingletonSeparatedList<CollectionElementSyntax>(
                        SyntaxFactory.ExpressionElement(typeOfExpression)))
                : generator.ArrayCreationExpression(
                    generator.TypeExpression(editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_Type)),
                    [typeOfExpression]);

            unmanagedCallConvAttribute = generator.Attribute(TypeNames.UnmanagedCallConvAttribute,
                generator.AttributeArgument("CallConvs", argumentValue));

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
