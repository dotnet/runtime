// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop.JavaScript
{
    [Generator]
    public sealed class JSImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            JSSignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            JSImportData JSImportData,
            SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all methods adorned with JSImportAttribute
            var attributedMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.JSImportAttribute,
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

                return ImmutableArray.Create(DiagnosticInfo.Create(GeneratorDiagnostics.JSImportRequiresAllowUnsafeBlocks, null));
            }));

            IncrementalValuesProvider<(MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>)> generateSingleStub = methodsToGenerate
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
                .WithComparer(Comparers.GeneratedSyntax)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterDiagnostics(generateSingleStub.SelectMany((stubInfo, ct) => stubInfo.Item2));

            context.RegisterConcatenatedSyntaxOutputs(generateSingleStub.Select((data, ct) => data.Item1), "JSImports.g.cs");
        }

        private static SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        private static MemberDeclarationSyntax PrintGeneratedSource(
            ContainingSyntax userDeclaredMethod,
            JSSignatureContext stub,
            ContainingSyntaxContext containingSyntaxContext,
            BlockSyntax stubCode)
        {
            // Create stub function
            MethodDeclarationSyntax stubMethod = MethodDeclaration(stub.SignatureContext.StubReturnType, userDeclaredMethod.Identifier)
                .AddAttributeLists(stub.SignatureContext.AdditionalAttributes.ToArray())
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.DebuggerNonUserCodeAttribute))))))
                .WithModifiers(StripTriviaFromModifiers(userDeclaredMethod.Modifiers))
                .WithParameterList(ParameterList(SeparatedList(stub.SignatureContext.StubParameters)))
                .WithBody(stubCode);

            FieldDeclarationSyntax sigField = FieldDeclaration(VariableDeclaration(IdentifierName(Constants.JSFunctionSignatureGlobal))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(stub.BindingName)))))
                .AddModifiers(Token(SyntaxKind.StaticKeyword));

            MemberDeclarationSyntax toPrint = containingSyntaxContext.WrapMembersInContainingSyntaxWithUnsafeModifier(stubMethod, sigField);
            return toPrint;
        }

        private static JSImportData? ProcessJSImportAttribute(AttributeData attrData)
        {
            // Found the JSImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 1)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), null);
            }
            if (attrData.ConstructorArguments.Length == 2)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), attrData.ConstructorArguments[1].Value!.ToString());
            }
            return null;
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Get any attributes of interest on the method
            AttributeData? jsImportAttr = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == Constants.JSImportAttribute)
                {
                    jsImportAttr = attr;
                }
            }

            Debug.Assert(jsImportAttr is not null);

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            // Process the JSImport attribute
            JSImportData? jsImportData = ProcessJSImportAttribute(jsImportAttr!);

            if (jsImportData is null)
            {
                generatorDiagnostics.ReportConfigurationNotSupported(jsImportAttr!, "Invalid syntax");
                jsImportData = new JSImportData("INVALID_CSHARP_SYNTAX", null);
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
                jsImportData,
                new SequenceEqualImmutableArray<DiagnosticInfo>(generatorDiagnostics.Diagnostics.ToImmutableArray()));
        }

        private static (MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateSource(
            IncrementalStubGenerationContext incrementalContext)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), incrementalContext.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));

            // We need to add the implicit exception and return arguments to the signature and ensure they are initialized before we start to do any marshalling.
            const int NumImplicitArguments = 2;

            ImmutableArray<TypePositionInfo> originalElementInfo = incrementalContext.SignatureContext.SignatureContext.ElementTypeInformation;

            ImmutableArray<TypePositionInfo>.Builder typeInfoBuilder = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElementInfo.Length + NumImplicitArguments);

            TypePositionInfo nativeOnlyParameterTemplate = new TypePositionInfo(
                SpecialTypeInfo.Void,
                new JSMarshallingInfo(
                    NoMarshallingInfo.Instance,
                    new JSInvalidTypeInfo()))
            {
                ManagedIndex = TypePositionInfo.UnsetIndex,
            };

            typeInfoBuilder.Add(
                // Add the exception argument
                nativeOnlyParameterTemplate with
                {
                    InstanceIdentifier = Constants.ArgumentException,
                    NativeIndex = 0,
                });

            typeInfoBuilder.Add(
                // Add the incoming return argument
                nativeOnlyParameterTemplate with
                {
                    InstanceIdentifier = Constants.ArgumentReturn,
                    NativeIndex = 1,
                });

            bool hasReturn = false;

            foreach (var info in originalElementInfo)
            {
                TypePositionInfo updatedInfo = info with
                {
                    MarshallingAttributeInfo = info.MarshallingAttributeInfo is JSMarshallingInfo jsInfo
                        ? jsInfo.AddElementDependencies([typeInfoBuilder[0], typeInfoBuilder[1]])
                        : info.MarshallingAttributeInfo,
                };

                if (info.IsManagedReturnPosition)
                {
                    hasReturn = info.ManagedType != SpecialTypeInfo.Void;
                }

                if (info.IsNativeReturnPosition)
                {
                    typeInfoBuilder.Add(updatedInfo);
                }
                else
                {
                    typeInfoBuilder.Add(updatedInfo with
                    {
                        NativeIndex = updatedInfo.NativeIndex + NumImplicitArguments
                    });
                }
            }

            ImmutableArray<TypePositionInfo> finalElementInfo = typeInfoBuilder.ToImmutable();

            // Generate stub code
            var stubGenerator = new ManagedToNativeStubGenerator(
                finalElementInfo,
                setLastError: false,
                diagnostics,
                new CompositeMarshallingGeneratorResolver(
                    new NoSpanAndTaskMixingResolver(),
                    new JSGeneratorResolver()),
                new CodeEmitOptions(SkipInit: true));

            const string LocalFunctionName = "__InvokeJSFunction";

            BlockSyntax code = stubGenerator.GenerateStubBody(LocalFunctionName);

            StatementSyntax bindStatement = GenerateBindSyntax(
                incrementalContext.JSImportData,
                incrementalContext.SignatureContext,
                SignatureBindingHelpers.CreateSignaturesArgument(incrementalContext.SignatureContext.SignatureContext.ElementTypeInformation, StubCodeContext.DefaultManagedToNativeStub));

            LocalFunctionStatementSyntax localFunction = GenerateInvokeFunction(LocalFunctionName, incrementalContext.SignatureContext, stubGenerator, hasReturn);

            return (PrintGeneratedSource(incrementalContext.StubMethodSyntaxTemplate, incrementalContext.SignatureContext, incrementalContext.ContainingSyntaxContext, Block(bindStatement, code, localFunction)), incrementalContext.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static IfStatementSyntax GenerateBindSyntax(JSImportData jsImportData, JSSignatureContext signatureContext, ArgumentSyntax signaturesArgument)
        {
            var bindingParameters =
                (new ArgumentSyntax[] {
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(jsImportData.FunctionName))),
                        Argument(
                            jsImportData.ModuleName == null
                            ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                            : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(jsImportData.ModuleName))),
                        signaturesArgument,
                });

            return IfStatement(BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(signatureContext.BindingName), LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            Block(SingletonList<StatementSyntax>(
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(signatureContext.BindingName),
                                            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName(Constants.BindJSFunctionMethod)))
                                            .WithArgumentList(ArgumentList(SeparatedList(bindingParameters))))))));
        }

        private static LocalFunctionStatementSyntax GenerateInvokeFunction(string functionName, JSSignatureContext signatureContext, ManagedToNativeStubGenerator stubGenerator, bool hasReturn)
        {
            var (parameters, returnType, _) = stubGenerator.GenerateTargetMethodSignatureData();
            TypeSyntax jsMarshalerArgument = ParseTypeName(Constants.JSMarshalerArgumentGlobal);

            CollectionExpressionSyntax argumentsBuffer = CollectionExpression(
                SeparatedList<CollectionElementSyntax>(
                    parameters.Parameters
                        .Select(p => ExpressionElement(IdentifierName(p.Identifier)))));

            List<StatementSyntax> statements = [];

            if (hasReturn)
            {
                statements.AddRange([
                    Declare(
                        SpanOf(jsMarshalerArgument),
                        Constants.ArgumentsBuffer,
                        argumentsBuffer),
                    MethodInvocationStatement(
                        IdentifierName(Constants.JSFunctionSignatureGlobal),
                        IdentifierName("InvokeJS"),
                        Argument(IdentifierName(signatureContext.BindingName)),
                        Argument(IdentifierName(Constants.ArgumentsBuffer))),
                    ReturnStatement(
                    ElementAccessExpression(
                    IdentifierName(Constants.ArgumentsBuffer),
                    BracketedArgumentList(SingletonSeparatedList(Argument(
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))))))
                ]);
            }
            else
            {
                statements.Add(
                    MethodInvocationStatement(
                        IdentifierName(Constants.JSFunctionSignatureGlobal),
                        IdentifierName("InvokeJS"),
                        Argument(IdentifierName(signatureContext.BindingName)),
                        Argument(argumentsBuffer)));
            }

            return LocalFunctionStatement(
                hasReturn ? jsMarshalerArgument : PredefinedType(Token(SyntaxKind.VoidKeyword)),
                functionName)
                .WithBody(Block(statements))
                .WithParameterList(parameters)
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    Attribute(IdentifierName(Constants.DebuggerNonUserCodeAttribute))))));
        }

        private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return Diagnostic.Create(GeneratorDiagnostics.InvalidImportAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = methodSyntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, typeDecl.Identifier);
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
