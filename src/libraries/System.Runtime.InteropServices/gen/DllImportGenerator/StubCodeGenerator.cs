using System;
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

        public override bool PinningSupported => true;

        public override bool StackSpaceUsable => true;

        /// <summary>
        /// Identifier for managed return value
        /// </summary>
        public const string ReturnIdentifier = "__retVal";

        /// <summary>
        /// Identifier for native return value
        /// </summary>
        /// <remarks>Same as the managed identifier by default</remarks>
        public string ReturnNativeIdentifier { get; private set; } = ReturnIdentifier;

        private const string InvokeReturnIdentifier = "__invokeRetVal";

        /// <summary>
        /// Generate an identifier for the native return value and update the context with the new value
        /// </summary>
        /// <returns>Identifier for the native return value</returns>
        public void GenerateReturnNativeIdentifier()
        {
            if (CurrentStage != Stage.Setup)
                throw new InvalidOperationException();

            // Update the native identifier for the return value
            ReturnNativeIdentifier = $"{ReturnIdentifier}{GeneratedNativeIdentifierSuffix}";
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            if (info.IsManagedReturnPosition && !info.IsNativeReturnPosition)
            {
                return (ReturnIdentifier, ReturnNativeIdentifier);
            }
            else if (!info.IsManagedReturnPosition && info.IsNativeReturnPosition)
            {
                return (InvokeReturnIdentifier, InvokeReturnIdentifier);
            }
            else if (info.IsManagedReturnPosition && info.IsNativeReturnPosition)
            {
                return (ReturnIdentifier, ReturnNativeIdentifier);
            }
            else
            {
                // If the info isn't in either the managed or native return position,
                // then we can use the base implementation since we have an identifier name provided
                // in the original metadata.
                return base.GetIdentifiers(info);
            }
        }

        public static (BlockSyntax Code, MethodDeclarationSyntax DllImport) GenerateSyntax(
            IMethodSymbol stubMethod,
            IEnumerable<TypePositionInfo> paramsTypeInfo,
            TypePositionInfo retTypeInfo)
        {
            Debug.Assert(retTypeInfo.IsNativeReturnPosition);
            
            var context = new StubCodeGenerator(Stage.Setup);

            string dllImportName = stubMethod.Name + "__PInvoke__";
            var paramMarshallers = paramsTypeInfo.Select(p => GetMarshalInfo(p, context)).ToList();
            var retMarshaller = GetMarshalInfo(retTypeInfo, context);

            var statements = new List<StatementSyntax>();

            if (retMarshaller.Generator.UsesNativeIdentifier(retTypeInfo, context))
            {
                context.GenerateReturnNativeIdentifier();
            }

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
                Stage.KeepAlive,
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

        private static (TypePositionInfo TypeInfo, IMarshallingGenerator Generator) GetMarshalInfo(TypePositionInfo info, StubCodeContext context)
        {
            IMarshallingGenerator generator;
            if (!MarshallingGenerators.TryCreate(info, context, out generator))
            {
                // [TODO] Report warning
            }

            return (info, generator);
        }
    }
}
