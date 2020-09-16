using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class StubCodeGenerator : StubCodeContext
    {
        private StubCodeGenerator(Stage stage)
        {
            CurrentStage = stage;
        }

        public static (BlockSyntax Code, MethodDeclarationSyntax DllImport) GenerateSyntax(
            IMethodSymbol stubMethod,
            IEnumerable<TypePositionInfo> paramsTypeInfo,
            TypePositionInfo retTypeInfo)
        {
            Debug.Assert(retTypeInfo.IsNativeReturnPosition);

            string dllImportName = stubMethod.Name + "__PInvoke__";
            var paramMarshallers = paramsTypeInfo.Select(p => GetMarshalInfo(p)).ToList();
            var retMarshaller = GetMarshalInfo(retTypeInfo);

            var context = new StubCodeGenerator(Stage.Setup);
            var statements = new List<StatementSyntax>();

            foreach (var marshaller in paramMarshallers)
            {
                TypePositionInfo info = marshaller.TypeInfo;
                if (info.RefKind != RefKind.Out || info.IsManagedReturnPosition)
                    continue;

                // Assign out params to default
                statements.Add(ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(info.InstanceIdentifier),
                        LiteralExpression(
                            SyntaxKind.DefaultLiteralExpression,
                            Token(SyntaxKind.DefaultKeyword)))));
            }

            bool invokeReturnsVoid = retTypeInfo.ManagedType.SpecialType == SpecialType.System_Void;
            bool stubReturnsVoid = stubMethod.ReturnsVoid;

            // Stub return is not the same as invoke return
            if (!stubReturnsVoid && !retTypeInfo.IsManagedReturnPosition)
            {
                Debug.Assert(paramsTypeInfo.Any() && paramsTypeInfo.Last().IsManagedReturnPosition);

                // Declare variable for stub return value
                TypePositionInfo info = paramsTypeInfo.Last();
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        info.ManagedType.AsTypeSyntax(),
                        SingletonSeparatedList(
                            VariableDeclarator(context.GetIdentifiers(info).managed)))));
            }

            if (!invokeReturnsVoid)
            {
                // Declare variable for invoke return value
                statements.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        retTypeInfo.ManagedType.AsTypeSyntax(),
                        SingletonSeparatedList(
                            VariableDeclarator(context.GetIdentifiers(retTypeInfo).managed)))));
            }

            var stages = new Stage[]
            {
                Stage.Setup,
                Stage.Marshal,
                Stage.Pin,
                Stage.Invoke,
                Stage.Unmarshal,
                Stage.Cleanup
            };

            var invoke = InvocationExpression(IdentifierName(dllImportName));
            var fixedStatements = new List<FixedStatementSyntax>();
            foreach (var stage in stages)
            {
                int initialCount = statements.Count;
                context.CurrentStage = stage;

                if (!invokeReturnsVoid && (stage == Stage.Setup || stage == Stage.Unmarshal))
                {
                    // Handle setup and unmarshalling for return
                    var retStatements = retMarshaller.Generator.Generate(retMarshaller.TypeInfo, context);
                    statements.AddRange(retStatements);
                }

                // Generate code for each parameter for the current stage
                foreach (var marshaller in paramMarshallers)
                {
                    if (stage == Stage.Invoke)
                    {
                        // Get arguments for invocation
                        ArgumentSyntax argSyntax = marshaller.Generator.AsArgument(marshaller.TypeInfo, context);
                        invoke = invoke.AddArgumentListArguments(argSyntax);
                    }
                    else
                    {
                        var generatedStatements = marshaller.Generator.Generate(marshaller.TypeInfo, context);
                        if (stage == Stage.Pin)
                        {
                            // Collect all the fixed statements. These will be used in the Invoke stage.
                            foreach (var statement in generatedStatements)
                            {
                                if (statement is not FixedStatementSyntax fixedStatement)
                                    continue;

                                fixedStatements.Add(fixedStatement);
                            }
                        }
                        else
                        {
                            statements.AddRange(generatedStatements);
                        }
                    }
                }

                if (stage == Stage.Invoke)
                {
                    StatementSyntax invokeStatement;

                    // Assign to return value if necessary
                    if (invokeReturnsVoid)
                    {
                        invokeStatement = ExpressionStatement(invoke);
                    }
                    else
                    {
                        invokeStatement = ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(context.GetIdentifiers(retMarshaller.TypeInfo).native),
                                invoke));
                    }

                    // Nest invocation in fixed statements
                    if (fixedStatements.Any())
                    {
                        fixedStatements.Reverse();
                        invokeStatement = fixedStatements.First().WithStatement(Block(invokeStatement));
                        foreach (var fixedStatement in fixedStatements.Skip(1))
                        {
                            invokeStatement = fixedStatement.WithStatement(Block(invokeStatement));
                        }
                    }

                    statements.Add(invokeStatement);
                }

                if (statements.Count > initialCount)
                {
                    // Comment separating each stage
                    var newLeadingTrivia = TriviaList(
                        Comment($"//"),
                        Comment($"// {stage}"),
                        Comment($"//"));
                    var firstStatementInStage = statements[initialCount];
                    newLeadingTrivia = newLeadingTrivia.AddRange(firstStatementInStage.GetLeadingTrivia());
                    statements[initialCount] = firstStatementInStage.WithLeadingTrivia(newLeadingTrivia);
                }
            }

            // Return
            if (!stubReturnsVoid)
                statements.Add(ReturnStatement(IdentifierName(ReturnIdentifier)));

            // Wrap all statements in an unsafe block
            var codeBlock = Block(UnsafeStatement(Block(statements)));

            // Define P/Invoke declaration
            var dllImport = MethodDeclaration(retMarshaller.Generator.AsNativeType(retMarshaller.TypeInfo), dllImportName)
                .AddModifiers(
                    Token(SyntaxKind.ExternKeyword),
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            foreach (var marshaller in paramMarshallers)
            {
                ParameterSyntax paramSyntax = marshaller.Generator.AsParameter(marshaller.TypeInfo);
                dllImport = dllImport.AddParameterListParameters(paramSyntax);
            }

            return (codeBlock, dllImport);
        }

        private static (TypePositionInfo TypeInfo, IMarshallingGenerator Generator) GetMarshalInfo(TypePositionInfo info)
        {
            IMarshallingGenerator generator;
            if (!MarshallingGenerators.TryCreate(info, out generator))
            {
                // [TODO] Report warning
            }

            return (info, generator);
        }
    }
}
