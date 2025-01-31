// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Generic;

namespace Microsoft.Interop.JavaScript
{
    [Generator]
    public sealed class JSExportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            JSSignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            JSExportData JSExportData,
            SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName);

            // Collect all methods adorned with JSExportAttribute
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.JSExportAttribute,
                   static (node, ct) => node is MethodDeclarationSyntax,
                   static (context, ct) => new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = (IMethodSymbol)context.TargetSymbol });

            // Validate if attributed methods can have source generated
            var methodsWithDiagnostics = attributedMethods.Select(static (data, ct) =>
            {
                Diagnostic? diagnostic = GetDiagnosticIfInvalidMethodForGeneration(data.Syntax, data.Symbol);
                return new { Syntax = data.Syntax, Symbol = data.Symbol, Diagnostic = diagnostic };
            });

            var methodsToGenerate = methodsWithDiagnostics.Where(static data => data.Diagnostic is null);
            var invalidMethodDiagnostics = methodsWithDiagnostics.Where(static data => data.Diagnostic is not null);

            // Report diagnostics for invalid methods
            context.RegisterSourceOutput(invalidMethodDiagnostics, static (context, invalidMethod) =>
            {
                context.ReportDiagnostic(invalidMethod.Diagnostic);
            });

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            // Validate environment that is being used to generate stubs.
            context.RegisterDiagnostics(stubEnvironment.Combine(attributedMethods.Collect()).SelectMany((data, ct) =>
            {
                if (data.Right.IsEmpty // no attributed methods
                    || data.Left.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true }) // Unsafe code enabled
                {
                    return ImmutableArray<DiagnosticInfo>.Empty;
                }

                return ImmutableArray.Create(DiagnosticInfo.Create(GeneratorDiagnostics.JSExportRequiresAllowUnsafeBlocks, null));
            }));

            IncrementalValuesProvider<(MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<DiagnosticInfo>)> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Select(static (data, ct) => new
                {
                    data.Left.Syntax,
                    data.Left.Symbol,
                    Environment = data.Right,
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation)
                .Select(
                    static (data, ct) => GenerateSource(data)
                )
                .WithComparer(Comparers.GeneratedSyntax4)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterDiagnostics(generateSingleStub.SelectMany((stubInfo, ct) => stubInfo.Item4));

            IncrementalValueProvider<ImmutableArray<(StatementSyntax, AttributeListSyntax)>> regSyntax = generateSingleStub
                .Select(
                    static (data, ct) => (data.Item2, data.Item3))
                .Collect();

            IncrementalValueProvider<string> registration = regSyntax
                .Combine(assemblyName)
                .Select(static (data, ct) => GenerateRegSource(data.Left, data.Right))
                .Select(static (data, ct) => data.NormalizeWhitespace().ToFullString());

            IncrementalValueProvider<ImmutableArray<(string, string)>> generated = generateSingleStub
                .Combine(registration)
                .Select(
                    static (data, ct) => (data.Left.Item1.NormalizeWhitespace().ToFullString(), data.Right))
                .Collect();


            context.RegisterSourceOutput(generated,
                (context, generatedSources) =>
                {
                    // Don't generate a file if we don't have to, to avoid the extra IDE overhead once we have generated
                    // files in play.
                    if (generatedSources.IsEmpty)
                        return;

                    StringBuilder source = new();
                    // Mark in source that the file is auto-generated.
                    source.Append("// <auto-generated/>\r\n");
                    // this is the assembly level registration
                    source.Append(generatedSources[0].Item2);
                    source.Append("\r\n");
                    // this is the method wrappers to be called from JS
                    foreach (var generated in generatedSources)
                    {
                        source.Append(generated.Item1);
                        source.Append("\r\n");
                    }

                    // Once https://github.com/dotnet/roslyn/issues/61326 is resolved, we can avoid the ToString() here.
                    context.AddSource("JSExports.g.cs", source.ToString());
                });

        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            ContainingSyntaxContext containingSyntaxContext,
            BlockSyntax wrapperStatements, string wrapperName)
        {

            MemberDeclarationSyntax wrappperMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(wrapperName))
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.UnsafeKeyword) }))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.DebuggerNonUserCodeAttribute))))))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier(Constants.ArgumentsBuffer)).WithType(PointerType(ParseTypeName(Constants.JSMarshalerArgumentGlobal))))))
                .WithBody(wrapperStatements);

            MemberDeclarationSyntax toPrint = containingSyntaxContext.WrapMembersInContainingSyntaxWithUnsafeModifier(wrappperMethod);

            return toPrint;
        }

        private static JSExportData? ProcessJSExportAttribute(AttributeData attrData)
        {
            // Found the JSExport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            return new JSExportData();
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Get any attributes of interest on the method
            AttributeData? jsExportAttr = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == Constants.JSExportAttribute)
                {
                    jsExportAttr = attr;
                }
            }

            Debug.Assert(jsExportAttr is not null);

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            // Process the JSExport attribute
            JSExportData? jsExportData = ProcessJSExportAttribute(jsExportAttr!);

            if (jsExportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(jsExportAttr!, "Invalid syntax");
                jsExportData = new JSExportData();
            }

            // Create the stub.
            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers, SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            return new IncrementalStubGenerationContext(
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                locations,
                jsExportData,
                new SequenceEqualImmutableArray<DiagnosticInfo>(generatorDiagnostics.Diagnostics.ToImmutableArray()));
        }

        private static NamespaceDeclarationSyntax GenerateRegSource(
            ImmutableArray<(StatementSyntax Registration, AttributeListSyntax Attribute)> methods, string assemblyName)
        {
            const string generatedNamespace = "System.Runtime.InteropServices.JavaScript";
            const string initializerClass = "__GeneratedInitializer";
            const string initializerName = "__Register_";
            const string trimmingPreserveName = "__TrimmingPreserve_";

            if (methods.IsEmpty) return NamespaceDeclaration(IdentifierName(generatedNamespace));

            var registerStatements = new List<StatementSyntax>();
            registerStatements.AddRange(GenerateJSExportArchitectureCheck());

            var attributes = new List<AttributeListSyntax>();
            foreach (var m in methods)
            {
                registerStatements.Add(m.Registration);
                attributes.Add(m.Attribute);
            }

            FieldDeclarationSyntax field = FieldDeclaration(VariableDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                            .WithVariables(SingletonSeparatedList(
                                VariableDeclarator(Identifier("initialized")))))
                            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                                Attribute(IdentifierName(Constants.ThreadStaticGlobal))))));

            MemberDeclarationSyntax method = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(initializerName))
                            .WithAttributeLists(List(attributes))
                            .WithModifiers(TokenList(new[] { Token(SyntaxKind.StaticKeyword) }))
                            .WithBody(Block(registerStatements));

            // HACK: protect the code from trimming with DynamicDependency attached to a ModuleInitializer
            MemberDeclarationSyntax initializerMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(trimmingPreserveName))
                            .WithAttributeLists(
                                SingletonList<AttributeListSyntax>(
                                    AttributeList(
                                        SeparatedList<AttributeSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Attribute(
                                                    IdentifierName(Constants.ModuleInitializerAttributeGlobal)),
                                                Token(SyntaxKind.CommaToken),
                                                Attribute(
                                                    IdentifierName(Constants.DynamicDependencyAttributeGlobal))
                                                .WithArgumentList(
                                                    AttributeArgumentList(
                                                        SeparatedList<AttributeArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                                AttributeArgument(
                                                                    BinaryExpression(
                                                                        SyntaxKind.BitwiseOrExpression,
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(Constants.DynamicallyAccessedMemberTypesGlobal),
                                                                            IdentifierName("PublicMethods")),
                                                                        MemberAccessExpression(
                                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                                            IdentifierName(Constants.DynamicallyAccessedMemberTypesGlobal),
                                                                            IdentifierName("NonPublicMethods")))),
                                                                Token(SyntaxKind.CommaToken),
                                                                AttributeArgument(
                                                                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal($"{generatedNamespace}.{initializerClass}"))
                                                                ),
                                                                Token(SyntaxKind.CommaToken),
                                                                AttributeArgument(
                                                                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(assemblyName))
                                                                )
                                                            })))}))))
                            .WithModifiers(TokenList(new[] {
                                Token(SyntaxKind.StaticKeyword),
                                Token(SyntaxKind.InternalKeyword)
                            }))
                            .WithBody(Block());

            var ns = NamespaceDeclaration(IdentifierName(generatedNamespace))
                        .WithMembers(
                            SingletonList<MemberDeclarationSyntax>(
                                ClassDeclaration(initializerClass)
                                .WithModifiers(TokenList(new SyntaxToken[]{
                                    Token(SyntaxKind.UnsafeKeyword)}))
                                .WithMembers(List(new[] { field, initializerMethod, method }))
                                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                                    Attribute(IdentifierName(Constants.CompilerGeneratedAttributeGlobal)))
                                )))));

            return ns;
        }

        private static StatementSyntax[] GenerateJSExportArchitectureCheck()
        {
            return [
                IfStatement(
                    BinaryExpression(SyntaxKind.LogicalOrExpression,
                        IdentifierName("initialized"),
                        BinaryExpression(SyntaxKind.NotEqualsExpression,
                            IdentifierName(Constants.OSArchitectureGlobal),
                            IdentifierName(Constants.ArchitectureWasmGlobal))),
                    ReturnStatement()),
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("initialized"),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))),
            ];
        }

        private static (MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<DiagnosticInfo>) GenerateSource(
            IncrementalStubGenerationContext incrementalContext)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), incrementalContext.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            // Generate stub code
            ImmutableArray<TypePositionInfo> signatureElements = incrementalContext.SignatureContext.SignatureContext.ElementTypeInformation;

            ImmutableArray<TypePositionInfo> allElements = signatureElements
                .Add(new TypePositionInfo(
                        new ReferenceTypeInfo(Constants.ExceptionGlobal, Constants.ExceptionGlobal),
                        new JSMarshallingInfo(NoMarshallingInfo.Instance, new JSSimpleTypeInfo(KnownManagedType.Exception, ParseTypeName(Constants.ExceptionGlobal)))
                        {
                            JSType = System.Runtime.InteropServices.JavaScript.JSTypeFlags.Error,
                        })
                {
                    InstanceIdentifier = Constants.ArgumentException,
                    ManagedIndex = TypePositionInfo.ExceptionIndex,
                    NativeIndex = signatureElements.Length, // Insert at the end of the argument list
                    RefKind = RefKind.Out, // We'll treat it as a separate out parameter.
                });

            for (int i = 0; i < allElements.Length; i++)
            {
                if (allElements[i].IsNativeReturnPosition && allElements[i].ManagedType != SpecialTypeInfo.Void)
                {
                    // The runtime may partially initialize the native return value.
                    // To preserve this information, we must pass the native return value as an out parameter.
                    allElements = allElements.SetItem(i, allElements[i] with
                    {
                        ManagedIndex = TypePositionInfo.ReturnIndex,
                        NativeIndex = allElements.Length, // Insert at the end of the argument list
                        RefKind = RefKind.Out, // We'll treat it as a separate out parameter.
                    });
                }
            }

            var stubGenerator = new UnmanagedToManagedStubGenerator(
                allElements,
                diagnostics,
                new CompositeMarshallingGeneratorResolver(
                    new NoSpanAndTaskMixingResolver(),
                    new JSGeneratorResolver()));

            var wrapperName = "__Wrapper_" + incrementalContext.StubMethodSyntaxTemplate.Identifier + "_" + incrementalContext.SignatureContext.TypesHash;

            const string innerWrapperName = "__Stub";

            BlockSyntax wrapperToInnerStubBlock = Block(
                CreateWrapperToInnerStubCall(signatureElements, innerWrapperName),
                GenerateInnerLocalFunction(incrementalContext, innerWrapperName, stubGenerator));

            StatementSyntax registration = GenerateJSExportRegistration(incrementalContext.SignatureContext);
            AttributeListSyntax registrationAttribute = AttributeList(SingletonSeparatedList(Attribute(IdentifierName(Constants.DynamicDependencyAttributeGlobal))
                    .WithArgumentList(AttributeArgumentList(SeparatedList(new[]{
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(wrapperName))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(incrementalContext.SignatureContext.StubTypeFullName))),
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(incrementalContext.SignatureContext.AssemblyName))),
                    }
                    )))));

            return (PrintGeneratedSource(incrementalContext.ContainingSyntaxContext, wrapperToInnerStubBlock, wrapperName),
                registration, registrationAttribute,
                incrementalContext.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static ExpressionStatementSyntax CreateWrapperToInnerStubCall(ImmutableArray<TypePositionInfo> signatureElements, string innerWrapperName)
        {
            List<ArgumentSyntax> arguments = [];
            bool hasReturn = true;
            foreach (var nativeArg in signatureElements.Where(e => e.NativeIndex != TypePositionInfo.UnsetIndex).OrderBy(e => e.NativeIndex))
            {
                if (nativeArg.IsNativeReturnPosition)
                {
                    if (nativeArg.ManagedType == SpecialTypeInfo.Void)
                    {
                        hasReturn = false;
                    }
                    continue;
                }
                arguments.Add(
                    Argument(
                        ElementAccessExpression(
                            IdentifierName(Constants.ArgumentsBuffer),
                            BracketedArgumentList(SingletonSeparatedList(Argument(
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(nativeArg.NativeIndex + 2))))))));
            }

            arguments.Add(Argument(IdentifierName(Constants.ArgumentsBuffer)));

            if (hasReturn)
            {
                arguments.Add(
                    Argument(
                        BinaryExpression(
                            SyntaxKind.AddExpression,
                            IdentifierName(Constants.ArgumentsBuffer),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))));
            }

            return ExpressionStatement(
                InvocationExpression(IdentifierName(innerWrapperName))
                        .WithArgumentList(ArgumentList(SeparatedList(arguments))));
        }

        private static LocalFunctionStatementSyntax GenerateInnerLocalFunction(IncrementalStubGenerationContext context, string innerFunctionName, UnmanagedToManagedStubGenerator stubGenerator)
        {
            var (parameters, returnType, _) = stubGenerator.GenerateAbiMethodSignatureData();
            return LocalFunctionStatement(
                returnType,
                innerFunctionName)
                .WithBody(stubGenerator.GenerateStubBody(IdentifierName(context.SignatureContext.MethodName)))
                .WithParameterList(parameters)
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.DebuggerNonUserCodeAttribute))))));
        }

        private static ExpressionStatementSyntax GenerateJSExportRegistration(JSSignatureContext context)
        {
            var signatureArgs = new List<ArgumentSyntax>
            {
                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(context.QualifiedMethodName))),
                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(context.TypesHash))),
                SignatureBindingHelpers.CreateSignaturesArgument(context.SignatureContext.ElementTypeInformation, StubCodeContext.DefaultNativeToManagedStub)
            };

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName(Constants.BindCSFunctionMethod)))
                .WithArgumentList(ArgumentList(SeparatedList(signatureArgs))));
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || (methodSyntax.Body is null && methodSyntax.ExpressionBody is null)
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidExportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidExportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
                }
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return Diagnostic.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }
    }
}
