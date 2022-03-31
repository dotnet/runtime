// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace JavaScript.MarshalerGenerator
{
    internal sealed class JSImportMethodGenerator : CommonJSMethodGenerator
    {
        public string BindingName;

        public JSImportMethodGenerator(MethodDeclarationSyntax methodSyntax,
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
            BoundFunctionName = jsAttrData.ConstructorArguments[0].Value.ToString();
            TypeSyntax = methodSyntax.Parent as TypeDeclarationSyntax;
            ParemeterSignatures = new JSMarshalerSig[MethodSymbol.Parameters.Length];
            prolog = new StringBuilder();

            uint hash = 17;
            unchecked
            {
                foreach (var param in MethodSymbol.Parameters)
                {
                    hash = hash * 31 + (uint)param.Type.Name.GetHashCode();
                }
            }
            BindingName = "__Binding_" + MethodName + "_" + hash;
        }


        public string GenerateWrapper()
        {
            NamespaceDeclarationSyntax namespaceSyntax = MethodSymbol.ContainingType.ContainingNamespace.AsNamespace();
            TypeDeclarationSyntax typeSyntax = CreateTypeDeclarationWithoutTrivia(TypeSyntax);

            IEnumerable<ParameterSyntax> parametersWithTypes = MethodSymbol.Parameters.Select(p => Parameter(Identifier(p.Name)).WithType(p.Type.AsTypeSyntax()));
            IEnumerable<StatementSyntax> statements = new[] { BindSyntax() }
                .Union(AllocationSyntax())
                .Union(InitSyntax())
                .Union(ConvertSyntax())
                .Union(CallSyntax())
                .Union(AfterSyntax())
                .Union(ReturnSyntax())
                ;
            MemberDeclarationSyntax wrappperMethod = MethodDeclaration(ReturnTypeSyntax, Identifier(MethodName))
                    .WithModifiers(MethodSyntax.Modifiers)
                    .WithParameterList(ParameterList(SeparatedList(parametersWithTypes)))
                    .WithBody(Block(List(statements)));
            MemberDeclarationSyntax bindingField = BindingField();

            CompilationUnitSyntax syntax = CompilationUnit()
                .WithMembers(SingletonList<MemberDeclarationSyntax>(namespaceSyntax
                    .WithMembers(SingletonList<MemberDeclarationSyntax>(typeSyntax
                        .WithMembers(List(new[] { bindingField, wrappperMethod })
                )))));

            return syntax.NormalizeWhitespace().ToFullString();
        }

        private MemberDeclarationSyntax BindingField()
        {
            return FieldDeclaration(VariableDeclaration(IdentifierName(Constants.JavaScriptMarshalerSignatureGlobal))
                .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(BindingName)))))
                .AddModifiers(Token(SyntaxKind.StaticKeyword))
                ;
        }

        private StatementSyntax BindSyntax()
        {


            var bindingParameters =
                (new ArgumentSyntax[] {
                    // name
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(BoundFunctionName))),
                    // marshallers
                    CreateMarshallersSyntax(),
                    // return type
                    Argument(TypeOfExpression(ReturnType.AsTypeSyntax())),
                })
                // parameter types
                .Union(MethodSymbol.Parameters.Select(p => Argument(TypeOfExpression(p.Type.AsTypeSyntax()))));

            return IfStatement(BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(BindingName), LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            Block(SingletonList<StatementSyntax>(
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(BindingName),
                                            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("BindJSFunction")))
                                            .WithArgumentList(ArgumentList(SeparatedList(bindingParameters))))))));
        }

        private IEnumerable<StatementSyntax> AllocationSyntax()
        {
            yield return LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier("var")))
                .WithVariables(SeparatedList(new[]{VariableDeclarator(Identifier("__excMessage"))
                .WithInitializer(EqualsValueClause(DefaultExpression(PredefinedType(Token(SyntaxKind.StringKeyword))))) })));
            if (!IsVoidMethod) yield return LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier("var")))
                .WithVariables(SeparatedList(new[]{VariableDeclarator(Identifier("__resRoot"))
                .WithInitializer(EqualsValueClause(DefaultExpression(PredefinedType(Token(SyntaxKind.ObjectKeyword))))) })));
            yield return LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier("var")))
                .WithVariables(SeparatedList(new[]{VariableDeclarator(Identifier("__buffer"))
                .WithInitializer(EqualsValueClause(
                    StackAllocArrayCreationExpression(ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(BindingName), IdentifierName("TotalBufferLength"))))))))) })));
            yield return LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier("var")))
                .WithVariables(SeparatedList(new[]{VariableDeclarator(Identifier("__args"))
                .WithInitializer(EqualsValueClause(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("CreateArguments")))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName("__buffer"))))))) })));
            for (int i = 0; i < ParemeterSignatures.Length; i++)
            {
                if (ParemeterSignatures[i].NeedsCast)
                {
                    yield return LocalDeclarationStatement(VariableDeclaration(IdentifierName(Identifier("var")))
                        .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("___"+MethodSymbol.Parameters[i].Name))
                        .WithInitializer(EqualsValueClause(CastExpression(ParemeterSignatures[i].MarshaledType.AsTypeSyntax(), IdentifierName(MethodSymbol.Parameters[i].Name)))))));
                }
            }
        }

        private IEnumerable<StatementSyntax> InitSyntax()
        {
            if (IsVoidMethod) yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("InitVoid")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                        Argument(IdentifierName("__excMessage")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                        Argument(IdentifierName("__args"))}))));

            else yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("InitResult")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                        Argument(IdentifierName("__excMessage")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                        Argument(IdentifierName("__resRoot")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                        Argument(IdentifierName("__args")),
                        Argument(IdentifierName(BindingName))}))));

            for (int i = 0; i < MethodSymbol.Parameters.Length; i++)
            {
                IParameterSymbol arg = MethodSymbol.Parameters[i];
                yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("InitArgument")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                                                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i+1))),
                                                    Argument(IdentifierName(arg.Name)).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                                    Argument(IdentifierName("__args")),
                                                    Argument(IdentifierName(BindingName))}))));
            }
        }

        private IEnumerable<StatementSyntax> ConvertSyntax()
        {
            for (int i = 0; i < MethodSymbol.Parameters.Length; i++)
            {
                IParameterSymbol arg = MethodSymbol.Parameters[i];
                yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    ParemeterSignatures[i].MarshalerType.AsTypeSyntax(), IdentifierName(ParemeterSignatures[i].ToJsMethod)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName((ParemeterSignatures[i].NeedsCast ? "___" : "")+ arg.Name)).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                            Argument(ElementAccessExpression(IdentifierName("__args"))
                                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i+1)))))))}))));
            }
        }

        private IEnumerable<StatementSyntax> AfterSyntax()
        {
            for (int i = 0; i < MethodSymbol.Parameters.Length; i++)
            {
                if (ParemeterSignatures[i].AfterToJsMethod != null)
                {
                    IParameterSymbol arg = MethodSymbol.Parameters[i];
                    yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    ParemeterSignatures[i].MarshalerType.AsTypeSyntax(), IdentifierName(ParemeterSignatures[i].AfterToJsMethod)))
                        .WithArgumentList(ArgumentList(SeparatedList(new[]{
                            Argument(IdentifierName((ParemeterSignatures[i].NeedsCast ? "___" : "")+ arg.Name)).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                            Argument(ElementAccessExpression(IdentifierName("__args"))
                                .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i+1)))))))}))));
                }
            }
        }

        private IEnumerable<StatementSyntax> CallSyntax()
        {
            yield return ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(Constants.JavaScriptMarshalGlobal), IdentifierName("InvokeBoundJSFunction")))
                    .WithArgumentList(ArgumentList(SeparatedList(new[]{
                        Argument(IdentifierName(BindingName)),
                        Argument(IdentifierName("__args"))}))));
        }

        private IEnumerable<StatementSyntax> ReturnSyntax()
        {
            if (!IsVoidMethod)
            {
                ExpressionSyntax invocation = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                ReturnSignature.MarshalerType.AsTypeSyntax(), IdentifierName(ReturnSignature.ToManagedMethod)))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("__args"), IdentifierName("Result"))))));

                if (ReturnSignature.NeedsCast)
                {
                    invocation = CastExpression(MethodSymbol.ReturnType.AsTypeSyntax(), invocation);
                }

                yield return ReturnStatement(invocation);
            }
        }
    }
}
