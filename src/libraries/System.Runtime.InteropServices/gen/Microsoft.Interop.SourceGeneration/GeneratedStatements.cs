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

namespace Microsoft.Interop
{
    public struct GeneratedStatements
    {
        public ImmutableArray<StatementSyntax> Setup { get; init; }
        public ImmutableArray<StatementSyntax> Marshal { get; init; }
        public ImmutableArray<FixedStatementSyntax> Pin { get; init; }
        public StatementSyntax InvokeStatement { get; init; }
        public ImmutableArray<StatementSyntax> Unmarshal { get; init; }
        public ImmutableArray<StatementSyntax> KeepAlive { get; init; }
        public ImmutableArray<StatementSyntax> GuaranteedUnmarshal { get; init; }
        public ImmutableArray<StatementSyntax> Cleanup { get; init; }

        public static GeneratedStatements Create(BoundGenerators marshallers, StubCodeContext context, ExpressionSyntax expressionToInvoke)
        {
            return new GeneratedStatements
            {
                Setup = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Setup }),
                Marshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Marshal }),
                Pin = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Pin }).Cast<FixedStatementSyntax>().ToImmutableArray(),
                InvokeStatement = GenerateStatementForNativeInvoke(marshallers, context with { CurrentStage = StubCodeContext.Stage.Invoke }, expressionToInvoke),
                Unmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Unmarshal }),
                KeepAlive = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.KeepAlive }),
                GuaranteedUnmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.GuaranteedUnmarshal }),
                Cleanup = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Cleanup }),
            };
        }

        private static ImmutableArray<StatementSyntax> GenerateStatementsForStubContext(BoundGenerators marshallers, StubCodeContext context)
        {
            ImmutableArray<StatementSyntax>.Builder statementsToUpdate = ImmutableArray.CreateBuilder<StatementSyntax>();
            if (marshallers.NativeReturnMarshaller.TypeInfo.ManagedType != SpecialTypeInfo.Void && (context.CurrentStage is StubCodeContext.Stage.Setup or StubCodeContext.Stage.Cleanup))
            {
                IEnumerable<StatementSyntax> retStatements = marshallers.NativeReturnMarshaller.Generator.Generate(marshallers.NativeReturnMarshaller.TypeInfo, context);
                statementsToUpdate.AddRange(retStatements);
            }

            if (context.CurrentStage is StubCodeContext.Stage.Unmarshal or StubCodeContext.Stage.GuaranteedUnmarshal)
            {
                // For Unmarshal and GuaranteedUnmarshal stages, use the topologically sorted
                // marshaller list to generate the marshalling statements

                foreach (BoundGenerator marshaller in marshallers.AllMarshallers)
                {
                    statementsToUpdate.AddRange(marshaller.Generator.Generate(marshaller.TypeInfo, context));
                }
            }
            else
            {
                // Generate code for each parameter for the current stage in declaration order.
                foreach (BoundGenerator marshaller in marshallers.NativeParameterMarshallers)
                {
                    IEnumerable<StatementSyntax> generatedStatements = marshaller.Generator.Generate(marshaller.TypeInfo, context);
                    statementsToUpdate.AddRange(generatedStatements);
                }
            }

            if (statementsToUpdate.Count > 0)
            {
                // Comment separating each stage
                SyntaxTriviaList newLeadingTrivia = GenerateStageTrivia(context.CurrentStage);
                StatementSyntax firstStatementInStage = statementsToUpdate[0];
                newLeadingTrivia = newLeadingTrivia.AddRange(firstStatementInStage.GetLeadingTrivia());
                statementsToUpdate[0] = firstStatementInStage.WithLeadingTrivia(newLeadingTrivia);
            }
            return statementsToUpdate.ToImmutable();
        }

        private static StatementSyntax GenerateStatementForNativeInvoke(BoundGenerators marshallers, StubCodeContext context, ExpressionSyntax expressionToInvoke)
        {
            if (context.CurrentStage != StubCodeContext.Stage.Invoke)
            {
                throw new ArgumentException("CurrentStage must be Invoke");
            }
            InvocationExpressionSyntax invoke = InvocationExpression(expressionToInvoke);
            // Generate code for each parameter for the current stage
            foreach (BoundGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                // Get arguments for invocation
                ArgumentSyntax argSyntax = marshaller.Generator.AsArgument(marshaller.TypeInfo, context);
                invoke = invoke.AddArgumentListArguments(argSyntax);
            }
            // Assign to return value if necessary
            if (marshallers.NativeReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void)
            {
                return ExpressionStatement(invoke);
            }

            return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(context.GetIdentifiers(marshallers.NativeReturnMarshaller.TypeInfo).native),
                        invoke));
        }

        private static SyntaxTriviaList GenerateStageTrivia(StubCodeContext.Stage stage)
        {
            string comment = stage switch
            {
                StubCodeContext.Stage.Setup => "Perform required setup.",
                StubCodeContext.Stage.Marshal => "Convert managed data to native data.",
                StubCodeContext.Stage.Pin => "Pin data in preparation for calling the P/Invoke.",
                StubCodeContext.Stage.Invoke => "Call the P/Invoke.",
                StubCodeContext.Stage.Unmarshal => "Convert native data to managed data.",
                StubCodeContext.Stage.Cleanup => "Perform required cleanup.",
                StubCodeContext.Stage.KeepAlive => "Keep alive any managed objects that need to stay alive across the call.",
                StubCodeContext.Stage.GuaranteedUnmarshal => "Convert native data to managed data even in the case of an exception during the non-cleanup phases.",
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };

            // Comment separating each stage
            return TriviaList(Comment($"// {stage} - {comment}"));
        }
    }
}
