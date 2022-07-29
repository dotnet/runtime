// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSExportCodeGenerator : JSCodeGenerator
    {
        private readonly BoundGenerators _marshallers;

        private readonly JSExportCodeContext _context;
        private readonly JSSignatureContext _signatureContext;

        public JSExportCodeGenerator(
            StubEnvironment environment,
            ImmutableArray<TypePositionInfo> argTypes,
            JSExportData attributeData,
            JSSignatureContext signatureContext,
            Action<TypePositionInfo, MarshallingNotSupportedException> marshallingNotSupportedCallback,
            IMarshallingGeneratorFactory generatorFactory)
        {
            _signatureContext = signatureContext;
            ManagedToNativeStubCodeContext innerContext = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, ReturnIdentifier);
            _context = new JSExportCodeContext(attributeData, innerContext);
            _marshallers = new BoundGenerators(argTypes, CreateGenerator);
            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, null))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                innerContext = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, ReturnNativeIdentifier);
                _context = new JSExportCodeContext(attributeData, innerContext);
                _marshallers = new BoundGenerators(argTypes, CreateGenerator);
            }

            // validate task + span mix
            if (_marshallers.ManagedReturnMarshaller.TypeInfo.ManagedType is JSTaskTypeInfo)
            {
                BoundGenerator spanArg = _marshallers.AllMarshallers.FirstOrDefault(m => m.TypeInfo.ManagedType is JSSpanTypeInfo);
                if (spanArg != default)
                {
                    marshallingNotSupportedCallback(spanArg.TypeInfo, new MarshallingNotSupportedException(spanArg.TypeInfo, _context)
                    {
                        NotSupportedDetails = SR.SpanAndTaskNotSupported
                    });
                }
            }

            IMarshallingGenerator CreateGenerator(TypePositionInfo p)
            {
                try
                {
                    return generatorFactory.Create(p, _context);
                }
                catch (MarshallingNotSupportedException e)
                {
                    marshallingNotSupportedCallback(p, e);
                    return new EmptyJSGenerator();
                }
            }
        }

        public BlockSyntax GenerateJSExportBody()
        {
            StatementSyntax invoke = InvokeSyntax();
            JSGeneratedStatements statements = JSGeneratedStatements.Create(_marshallers, _context, invoke);
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.Cleanup.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToNative(_marshallers, _context, shouldInitializeVariables);

            var setupStatements = new List<StatementSyntax>();
            SetupSyntax(setupStatements);

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                setupStatements.Add(MarshallerHelpers.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(statements.Unmarshal);

            tryStatements.AddRange(statements.InvokeStatements);

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.Marshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal)));
            }

            finallyStatements.AddRange(statements.Cleanup);
            if (finallyStatements.Count > 0)
            {
                allStatements.Add(
                    TryStatement(Block(tryStatements), default, FinallyClause(Block(finallyStatements))));
            }
            else
            {
                allStatements.AddRange(tryStatements);
            }

            return Block(allStatements);
        }

        public BlockSyntax GenerateJSExportRegistration()
        {
            var registrationStatements = new List<StatementSyntax>();
            registrationStatements.Add(IfStatement(
                BinaryExpression(SyntaxKind.NotEqualsExpression,
                    IdentifierName(Constants.OSArchitectureGlobal),
                    IdentifierName(Constants.ArchitectureWasmGlobal)),
                ReturnStatement()));

            var signatureArgs = new List<ArgumentSyntax>();

            signatureArgs.Add(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(_signatureContext.QualifiedMethodName))));
            signatureArgs.Add(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_signatureContext.TypesHash))));

            signatureArgs.Add(CreateSignaturesSyntax());

            registrationStatements.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName(Constants.BindCSFunctionMethod)))
                .WithArgumentList(ArgumentList(SeparatedList(signatureArgs)))));

            return Block(List(registrationStatements));
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

        private StatementSyntax InvokeSyntax()
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
                statements.AddRange(_marshallers.ManagedReturnMarshaller.Generator.Generate(_marshallers.ManagedReturnMarshaller.TypeInfo, _context with { CurrentStage = StubCodeContext.Stage.Marshal }));
            }
            return TryStatement(SingletonList(CatchClause()
                        .WithDeclaration(CatchDeclaration(IdentifierName(Constants.ExceptionGlobal)).WithIdentifier(Identifier("ex")))
                        .WithBlock(Block(SingletonList<StatementSyntax>(
                            ExpressionStatement(InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(Constants.ArgumentException), IdentifierName(Constants.ToJSMethod)))
                            .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("ex")))))))))))
                .WithBlock(Block(statements));

        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData();
        }
    }
}
