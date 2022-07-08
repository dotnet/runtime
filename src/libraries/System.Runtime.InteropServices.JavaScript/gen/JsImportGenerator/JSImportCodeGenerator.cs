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
    internal abstract class JSCodeGenerator
    {
        public const string ReturnIdentifier = "__retVal";
        public const string ReturnNativeIdentifier = $"{ReturnIdentifier}{StubCodeContext.GeneratedNativeIdentifierSuffix}";
        public const string InvokeSucceededIdentifier = "__invokeSucceeded";
    }

    internal sealed class JSImportCodeGenerator : JSCodeGenerator
    {
        private readonly BoundGenerators _marshallers;

        private readonly JSImportCodeContext _context;
        private readonly JSSignatureContext _signatureContext;

        public JSImportCodeGenerator(
            StubEnvironment environment,
            ImmutableArray<TypePositionInfo> argTypes,
            JSImportData attributeData,
            JSSignatureContext signatureContext,
            Action<TypePositionInfo, MarshallingNotSupportedException> marshallingNotSupportedCallback,
            IMarshallingGeneratorFactory generatorFactory)
        {
            _signatureContext = signatureContext;
            ManagedToNativeStubCodeContext innerContext = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, ReturnIdentifier);
            _context = new JSImportCodeContext(attributeData, innerContext);
            _marshallers = new BoundGenerators(argTypes, CreateGenerator);
            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, null))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                innerContext = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, ReturnNativeIdentifier);
                _context = new JSImportCodeContext(attributeData, innerContext);
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

        public BlockSyntax GenerateJSImportBody()
        {
            StatementSyntax invoke = InvokeSyntax();
            JSGeneratedStatements statements = JSGeneratedStatements.Create(_marshallers, _context, invoke);
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.Cleanup.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToNative(_marshallers, _context, shouldInitializeVariables);

            var setupStatements = new List<StatementSyntax>();
            BindSyntax(setupStatements);
            SetupSyntax(setupStatements);

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                setupStatements.Add(MarshallerHelpers.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(statements.Marshal);

            tryStatements.AddRange(statements.InvokeStatements);
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.Unmarshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal)));
            }

            finallyStatements.AddRange(statements.Cleanup);
            if (finallyStatements.Count > 0)
            {
                // Add try-finally block if there are any statements in the finally block
                allStatements.Add(
                    TryStatement(Block(tryStatements), default, FinallyClause(Block(finallyStatements))));
            }
            else
            {
                allStatements.AddRange(tryStatements);
            }

            // Return
            if (!_marshallers.IsManagedVoidReturn)
                allStatements.Add(ReturnStatement(IdentifierName(_context.GetIdentifiers(_marshallers.ManagedReturnMarshaller.TypeInfo).managed)));

            return Block(allStatements);
        }

        private void BindSyntax(List<StatementSyntax> statementsToUpdate)
        {
            var bindingParameters =
                (new ArgumentSyntax[] {
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(_context.AttributeData.FunctionName))),
                    Argument(
                        _context.AttributeData.ModuleName == null
                        ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                        : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(_context.AttributeData.ModuleName))),
                    CreateSignaturesSyntax(),
                });

            statementsToUpdate.Add(IfStatement(BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(_signatureContext.BindingName), LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            Block(SingletonList<StatementSyntax>(
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(_signatureContext.BindingName),
                                            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName(Constants.BindJSFunctionMethod)))
                                            .WithArgumentList(ArgumentList(SeparatedList(bindingParameters)))))))));
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
            statementsToUpdate.Add(LocalDeclarationStatement(
                VariableDeclaration(GenericName(Identifier(Constants.SpanGlobal)).WithTypeArgumentList(
                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(Constants.JSMarshalerArgumentGlobal)))))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Constants.ArgumentsBuffer))
                .WithInitializer(EqualsValueClause(StackAllocArrayCreationExpression(ArrayType(IdentifierName(Constants.JSMarshalerArgumentGlobal))
                .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2 + _marshallers.NativeParameterMarshallers.Length)))))))))))));

            statementsToUpdate.Add(LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Constants.ArgumentException))
                .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))))))));

            statementsToUpdate.Add(ExpressionStatement(
                InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.ArgumentException), IdentifierName("Initialize")))));

            statementsToUpdate.Add(LocalDeclarationStatement(VariableDeclaration(RefType(IdentifierName(Constants.JSMarshalerArgumentGlobal)))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(Constants.ArgumentReturn))
                .WithInitializer(EqualsValueClause(RefExpression(ElementAccessExpression(IdentifierName(Constants.ArgumentsBuffer))
                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))))))))))));

            statementsToUpdate.Add(ExpressionStatement(
                InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.ArgumentReturn), IdentifierName("Initialize")))));
        }

        private StatementSyntax InvokeSyntax()
        {
            return ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName("InvokeJS")))
                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                    Argument(IdentifierName(_signatureContext.BindingName)),
                    Argument(IdentifierName(Constants.ArgumentsBuffer))}))));
        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData();
        }
    }
}
