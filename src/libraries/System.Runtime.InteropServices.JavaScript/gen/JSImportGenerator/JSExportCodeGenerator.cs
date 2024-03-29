// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSExportCodeGenerator : JSCodeGenerator
    {
        private readonly BoundGenerators _marshallers;

        private readonly JSExportCodeContext _context;
        private readonly JSSignatureContext _signatureContext;

        public JSExportCodeGenerator(
            ImmutableArray<TypePositionInfo> argTypes,
            JSExportData attributeData,
            JSSignatureContext signatureContext,
            GeneratorDiagnosticsBag diagnosticsBag,
            IMarshallingGeneratorResolver generatorResolver)
        {
            _signatureContext = signatureContext;
            NativeToManagedStubCodeContext innerContext = new NativeToManagedStubCodeContext(ReturnIdentifier, ReturnIdentifier)
            {
                CodeEmitOptions = new(SkipInit: true)
            };
            _context = new JSExportCodeContext(attributeData, innerContext);

            _marshallers = BoundGenerators.Create(argTypes, generatorResolver, _context, new EmptyJSGenerator(), out var bindingFailures);

            diagnosticsBag.ReportGeneratorDiagnostics(bindingFailures);

            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, null))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                innerContext = new NativeToManagedStubCodeContext(ReturnIdentifier, ReturnNativeIdentifier)
                {
                    CodeEmitOptions = new(SkipInit: true)
                };
                _context = new JSExportCodeContext(attributeData, innerContext);
            }

            // validate task + span mix
            if (_marshallers.ManagedReturnMarshaller.TypeInfo.MarshallingAttributeInfo is JSMarshallingInfo(_, JSTaskTypeInfo))
            {
                BoundGenerator spanArg = _marshallers.SignatureMarshallers.FirstOrDefault(m => m.TypeInfo.MarshallingAttributeInfo is JSMarshallingInfo(_, JSSpanTypeInfo));
                if (spanArg != default)
                {
                    diagnosticsBag.ReportGeneratorDiagnostic(new GeneratorDiagnostic.NotSupported(spanArg.TypeInfo, _context)
                    {
                        NotSupportedDetails = SR.SpanAndTaskNotSupported
                    });
                }
            }
        }

        public BlockSyntax GenerateJSExportBody()
        {
            List<StatementSyntax> invoke = InvokeSyntax();
            GeneratedStatements statements = GeneratedStatements.Create(_marshallers, _context);
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.CleanupCallerAllocated.IsEmpty || !statements.CleanupCalleeAllocated.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForUnmanagedToManaged(_marshallers, _context, shouldInitializeVariables);

            var setupStatements = new List<StatementSyntax>();
            SetupSyntax(setupStatements);

            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                setupStatements.Add(SyntaxFactoryExtensions.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(statements.Unmarshal);

            tryStatements.AddRange(invoke);

            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.PinnedMarshal);
            tryStatements.AddRange(statements.Marshal);

            List<StatementSyntax> allStatements = setupStatements;

            // Wrap unmarshall, invocation and return value marshalling in try-catch.
            // In case of exception, marshal exception instead of return value.
            var tryInvokeAndMarshal = TryStatement(SingletonList(CatchClause()
                        .WithDeclaration(CatchDeclaration(IdentifierName(Constants.ExceptionGlobal)).WithIdentifier(Identifier("ex")))
                        .WithBlock(Block(SingletonList<StatementSyntax>(
                            ExpressionStatement(InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(Constants.ArgumentException), IdentifierName(Constants.ToJSMethod)))
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("ex")))))))))))
                .WithBlock(Block(tryStatements));

            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal.Concat(statements.CleanupCalleeAllocated))));
            }

            finallyStatements.AddRange(statements.CleanupCallerAllocated);

            if (finallyStatements.Count > 0)
            {
                tryInvokeAndMarshal = TryStatement(Block(tryInvokeAndMarshal), default, FinallyClause(Block(finallyStatements)));
            }

            allStatements.Add(tryInvokeAndMarshal);

            return Block(allStatements);
        }

        public static StatementSyntax[] GenerateJSExportArchitectureCheck()
        {
            return new StatementSyntax[]{
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
            };
        }

        public StatementSyntax GenerateJSExportRegistration()
        {
            var signatureArgs = new List<ArgumentSyntax>
            {
                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(_signatureContext.QualifiedMethodName))),
                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_signatureContext.TypesHash))),
                CreateSignaturesSyntax()
            };

            return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName(Constants.BindCSFunctionMethod)))
                .WithArgumentList(ArgumentList(SeparatedList(signatureArgs))));
        }

        private ArgumentSyntax CreateSignaturesSyntax()
        {
            var types = ((IJSMarshallingGenerator)_marshallers.ManagedReturnMarshaller.Generator).GenerateBind(_marshallers.ManagedReturnMarshaller.TypeInfo, _context)
                .Concat(_marshallers.NativeParameterMarshallers.SelectMany(p => ((IJSMarshallingGenerator)p.Generator).GenerateBind(p.TypeInfo, _context)));

            return Argument(ArrayCreationExpression(ArrayType(IdentifierName(Constants.JSMarshalerTypeGlobal))
                .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())))))
                .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList(types))));
        }

        private void SetupSyntax(List<StatementSyntax> statementsToUpdate)
        {
            foreach (BoundGenerator marshaller in _marshallers.NativeParameterMarshallers)
            {
                statementsToUpdate.Add(LocalDeclarationStatement(VariableDeclaration(marshaller.TypeInfo.ManagedType.Syntax)
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(marshaller.TypeInfo.InstanceIdentifier)))));
            }

            statementsToUpdate.Add(LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Constants.ArgumentException))
                .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))))))));

            statementsToUpdate.Add(LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Constants.ArgumentReturn))
                .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))))))))))));
        }

        private List<StatementSyntax> InvokeSyntax()
        {
            var statements = new List<StatementSyntax>();
            var arguments = new List<ArgumentSyntax>();

            // Generate code for each parameter for the current stage
            foreach (BoundGenerator marshaller in _marshallers.NativeParameterMarshallers)
            {
                // convert arguments for invocation
                statements.AddRange(marshaller.Generator.Generate(marshaller.TypeInfo, _context));
                arguments.Add(Argument(IdentifierName(marshaller.TypeInfo.InstanceIdentifier)));
            }

            if (_marshallers.IsManagedVoidReturn)
            {
                statements.Add(ExpressionStatement(InvocationExpression(IdentifierName(_signatureContext.MethodName))
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)))));
            }
            else
            {
                ExpressionSyntax invocation = InvocationExpression(IdentifierName(_signatureContext.MethodName))
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)));

                (string _, string nativeIdentifier) = _context.GetIdentifiers(_marshallers.ManagedReturnMarshaller.TypeInfo);

                ExpressionStatementSyntax statement = ExpressionStatement(AssignmentExpression(
                     SyntaxKind.SimpleAssignmentExpression,
                     IdentifierName(nativeIdentifier), invocation));

                statements.Add(statement);
            }
            return statements;

        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
