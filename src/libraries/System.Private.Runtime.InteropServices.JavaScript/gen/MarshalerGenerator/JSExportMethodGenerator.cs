// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JavaScript.MarshalerGenerator
{
    internal sealed class JSExportMethodGenerator : CommonJSMethodGenerator
    {
        public string WrapperName;
        public string RegistrationName;
        public int Hash;

        public JSExportMethodGenerator(MethodDeclarationSyntax methodSyntax,
            AttributeSyntax attributeSyntax,
            IMethodSymbol methodSymbol,
            IMethodSymbol attributeSymbol,
            AttributeData jsAttrData)
        {
            MethodSyntax = methodSyntax;
            MethodSymbol = methodSymbol;

            AttributeSymbol = attributeSymbol;
            AttributeSyntax = attributeSyntax;
            JSAttributeData = jsAttrData;
            BoundFunctionName = jsAttrData.ConstructorArguments.Length > 0
                ? jsAttrData.ConstructorArguments[0].Value.ToString()
                : null;
            TypeSyntax = methodSyntax.Parent as TypeDeclarationSyntax;
            ParemeterSignatures = new JSMarshalerSig[MethodSymbol.Parameters.Length];
            prolog = new StringBuilder();

            int hash = 17;
            unchecked
            {
                foreach (var param in MethodSymbol.Parameters)
                {
                    hash = hash * 31 + param.Type.Name.GetHashCode();
                }
            }
            Hash = Math.Abs(hash);
            WrapperName = "__Wrapper_" + MethodName + "_" + Hash;
            RegistrationName = "__Register_" + MethodName + "_" + Hash;
        }

        public string GenerateWrapper()
        {
            NamespaceDeclarationSyntax namespaceSyntax = MethodSymbol.ContainingType.ContainingNamespace.AsNamespace();
            TypeDeclarationSyntax typeSyntax = CreateTypeDeclarationWithoutTrivia(TypeSyntax);

            IEnumerable<StatementSyntax> wrapperStatements = WrapperSyntax();
            IEnumerable<StatementSyntax> registerStatements = RegistrationSyntax();

            MemberDeclarationSyntax wrappperMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(WrapperName))
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword) }))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("buffer")).WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))))))
                .WithBody(Block(List(wrapperStatements)));

            MemberDeclarationSyntax registerMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(RegistrationName))
                .WithAttributeLists(List(new AttributeListSyntax[]{
                    AttributeList(SingletonSeparatedList(Attribute(IdentifierName(Constants.ModuleInitializerAttributeGlobal)))),
                    AttributeList(SingletonSeparatedList(Attribute(IdentifierName(Constants.DynamicDependencyAttributeGlobal))
                    .WithArgumentList(AttributeArgumentList(SeparatedList(new[]{
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(WrapperName))),
                        AttributeArgument(TypeOfExpression(MethodSymbol.ContainingType.AsTypeSyntax()))}
                    )))))}))
                .WithModifiers(TokenList(new[] { Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword) }))
                .WithBody(Block(registerStatements));

            CompilationUnitSyntax syntax = CompilationUnit()
                .WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceSyntax
                    .WithMembers(SingletonList<MemberDeclarationSyntax>(typeSyntax
                        .WithMembers(List(new[] { wrappperMethod, registerMethod })
                )))));

            return syntax.NormalizeWhitespace().ToFullString();
        }

        private IEnumerable<StatementSyntax> RegistrationSyntax()
        {
            var fullyQualifiedName = $"[{AssemblyName}]{MethodSymbol.ContainingType.ToDisplayString()}:{MethodName}";
            var signatureArgs = new List<ArgumentSyntax>();
            signatureArgs.Add(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fullyQualifiedName))));
            signatureArgs.Add(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(Hash))));
            signatureArgs.Add(Argument(BoundFunctionName != null
                        ? LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(BoundFunctionName))
                        : LiteralExpression(SyntaxKind.NullLiteralExpression)));
            signatureArgs.Add(CreateMarshallersSyntax());
            signatureArgs.Add(Argument(TypeOfExpression(ReturnType.AsTypeSyntax())));
            signatureArgs.AddRange(MethodSymbol.Parameters.Select(p => Argument(TypeOfExpression(p.Type.AsTypeSyntax()))));

            yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JavaScriptMarshal), IdentifierName("BindCSFunction")))
                .WithArgumentList(ArgumentList(SeparatedList(signatureArgs))));
        }

        public IEnumerable<StatementSyntax> WrapperSyntax()
        {
            yield return LocalDeclarationStatement(VariableDeclaration(
                    IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("__args"))
                    .WithInitializer(EqualsValueClause(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(Constants.JavaScriptMarshal), IdentifierName("CreateArguments")))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName("buffer"))))))))));

            var statements=new List<StatementSyntax>();
            var arguments=new List<ArgumentSyntax>();

            for (int i = 0; i < MethodSymbol.Parameters.Length; i++)
            {
                IParameterSymbol arg = MethodSymbol.Parameters[i];
                ExpressionSyntax invocation = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    ParemeterSignatures[i].MarshalerType.AsTypeSyntax(), IdentifierName(ParemeterSignatures[i].ToManagedMethod)))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(ElementAccessExpression(IdentifierName("__args"))
                    .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i + 1))))))))));

                if (ParemeterSignatures[i].NeedsCast)
                {
                    invocation = CastExpression(MethodSymbol.Parameters[i].Type.AsTypeSyntax(), invocation);
                }

                statements.Add(LocalDeclarationStatement(VariableDeclaration(
                    IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(arg.Name))
                .WithInitializer(EqualsValueClause(invocation))))));

                arguments.Add(Argument(IdentifierName(arg.Name)));
            }
            if (IsVoidMethod)
            {
                statements.Add(ExpressionStatement(InvocationExpression(IdentifierName(MethodName))
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)))));
            }
            else
            {
                ExpressionSyntax invocation = InvocationExpression(IdentifierName(MethodName))
                    .WithArgumentList(ArgumentList(SeparatedList(arguments)));
                if (ReturnSignature.NeedsCast)
                {
                    invocation = CastExpression(ReturnSignature.MarshaledType.AsTypeSyntax(), invocation);
                }
                statements.Add(LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("__res"))
                    .WithInitializer(EqualsValueClause(invocation))))));

                statements.Add(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        ReturnSignature.MarshalerType.AsTypeSyntax(), IdentifierName(ReturnSignature.ToJsMethod)))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                    Argument(IdentifierName("__res")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                    Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("__args"), IdentifierName("Result")))})))));
            }

            yield return TryStatement(SingletonList(CatchClause()
                .WithDeclaration(CatchDeclaration(IdentifierName("Exception")).WithIdentifier(Identifier("__ex")))
                .WithBlock(Block(SingletonList<StatementSyntax>(
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(Constants.JavaScriptMarshal), IdentifierName("MarshalExceptionToJs")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                        Argument(IdentifierName("__ex")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                        Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("__args"), IdentifierName("Exception")))})))))))))
                .WithBlock(Block(statements));
        }
    }
}
