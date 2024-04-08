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
            ImmutableArray<TypePositionInfo> argTypes,
            JSImportData attributeData,
            JSSignatureContext signatureContext,
            GeneratorDiagnosticsBag diagnosticsBag,
            IMarshallingGeneratorResolver generatorResolver)
        {
            _signatureContext = signatureContext;
            ManagedToNativeStubCodeContext innerContext = new ManagedToNativeStubCodeContext(ReturnIdentifier, ReturnIdentifier)
            {
                CodeEmitOptions = new(SkipInit: true)
            };
            _context = new JSImportCodeContext(attributeData, innerContext);
            _marshallers = BoundGenerators.Create(argTypes, generatorResolver, _context, new EmptyJSGenerator(), out var bindingFailures);

            diagnosticsBag.ReportGeneratorDiagnostics(bindingFailures);

            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, null))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                innerContext = new ManagedToNativeStubCodeContext(ReturnIdentifier, ReturnNativeIdentifier)
                {
                    CodeEmitOptions = new(SkipInit: true)
                };
                _context = new JSImportCodeContext(attributeData, innerContext);
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

        public BlockSyntax GenerateJSImportBody()
        {
            StatementSyntax invoke = InvokeSyntax();
            GeneratedStatements statements = GeneratedStatements.Create(_marshallers, _context);
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.CleanupCallerAllocated.IsEmpty || !statements.CleanupCalleeAllocated.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToUnmanaged(_marshallers, _context, shouldInitializeVariables);

            var setupStatements = new List<StatementSyntax>();
            BindSyntax(setupStatements);
            SetupSyntax(setupStatements);

            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                setupStatements.Add(SyntaxFactoryExtensions.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(statements.Marshal);
            tryStatements.AddRange(statements.PinnedMarshal);

            tryStatements.Add(invoke);
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.Unmarshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal.Concat(statements.CleanupCalleeAllocated))));
            }

            finallyStatements.AddRange(statements.CleanupCallerAllocated);
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

        private ExpressionStatementSyntax InvokeSyntax()
        {
            return ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(Constants.JSFunctionSignatureGlobal), IdentifierName("InvokeJS")))
                .WithArgumentList(ArgumentList(SeparatedList(new[]{
                    Argument(IdentifierName(_signatureContext.BindingName)),
                    Argument(IdentifierName(Constants.ArgumentsBuffer))}))));
        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
