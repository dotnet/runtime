// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
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
        public ImmutableArray<StatementSyntax> PinnedMarshal { get; init; }
        public StatementSyntax InvokeStatement { get; init; }
        public ImmutableArray<StatementSyntax> Unmarshal { get; init; }
        public ImmutableArray<StatementSyntax> NotifyForSuccessfulInvoke { get; init; }
        public ImmutableArray<StatementSyntax> GuaranteedUnmarshal { get; init; }
        public ImmutableArray<StatementSyntax> Cleanup { get; init; }

        public ImmutableArray<CatchClauseSyntax> ManagedExceptionCatchClauses { get; init; }

        public static GeneratedStatements Create(BoundGenerators marshallers, StubCodeContext context)
        {
            return new GeneratedStatements
            {
                Setup = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Setup }),
                Marshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Marshal }),
                Pin = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Pin }).Cast<FixedStatementSyntax>().ToImmutableArray(),
                PinnedMarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.PinnedMarshal }),
                InvokeStatement = EmptyStatement(),
                Unmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.UnmarshalCapture })
                            .AddRange(GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Unmarshal })),
                NotifyForSuccessfulInvoke = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.NotifyForSuccessfulInvoke }),
                GuaranteedUnmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.GuaranteedUnmarshal }),
                Cleanup = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubCodeContext.Stage.Cleanup }),
                ManagedExceptionCatchClauses = GenerateCatchClauseForManagedException(marshallers, context)
            };
        }
        public static GeneratedStatements Create(BoundGenerators marshallers, StubCodeContext context, ExpressionSyntax expressionToInvoke)
        {
            GeneratedStatements statements = Create(marshallers, context);

            if (context.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                return statements with
                {
                    InvokeStatement = GenerateStatementForNativeInvoke(marshallers, context with { CurrentStage = StubCodeContext.Stage.Invoke }, expressionToInvoke)
                };
            }
            else if (context.Direction == MarshalDirection.UnmanagedToManaged)
            {
                return statements with
                {
                    InvokeStatement = GenerateStatementForManagedInvoke(marshallers, context with { CurrentStage = StubCodeContext.Stage.Invoke }, expressionToInvoke)
                };
            }
            else
            {
                throw new ArgumentException("Direction must be ManagedToUnmanaged or UnmanagedToManaged");
            }
        }

        private static ImmutableArray<StatementSyntax> GenerateStatementsForStubContext(BoundGenerators marshallers, StubCodeContext context)
        {
            ImmutableArray<StatementSyntax>.Builder statementsToUpdate = ImmutableArray.CreateBuilder<StatementSyntax>();
            foreach (BoundGenerator marshaller in marshallers.SignatureMarshallers)
            {
                statementsToUpdate.AddRange(marshaller.Generator.Generate(marshaller.TypeInfo, context));
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

        private static ExpressionStatementSyntax GenerateStatementForNativeInvoke(BoundGenerators marshallers, StubCodeContext context, ExpressionSyntax expressionToInvoke)
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

            var (managed, native) = context.GetIdentifiers(marshallers.NativeReturnMarshaller.TypeInfo);

            string targetIdentifier = marshallers.NativeReturnMarshaller.Generator.UsesNativeIdentifier(marshallers.NativeReturnMarshaller.TypeInfo, context)
                ? native
                : managed;

            return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(targetIdentifier),
                        invoke));
        }


        private static ExpressionStatementSyntax GenerateStatementForManagedInvoke(BoundGenerators marshallers, StubCodeContext context, ExpressionSyntax expressionToInvoke)
        {
            if (context.CurrentStage != StubCodeContext.Stage.Invoke)
            {
                throw new ArgumentException("CurrentStage must be Invoke");
            }
            InvocationExpressionSyntax invoke = InvocationExpression(expressionToInvoke);
            // Generate code for each parameter for the current stage
            foreach (BoundGenerator marshaller in marshallers.ManagedParameterMarshallers)
            {
                // Get arguments for invocation
                ArgumentSyntax argSyntax = marshaller.Generator.AsManagedArgument(marshaller.TypeInfo, context);
                invoke = invoke.AddArgumentListArguments(argSyntax);
            }
            // Assign to return value if necessary
            if (marshallers.ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void)
            {
                return ExpressionStatement(invoke);
            }

            return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(context.GetIdentifiers(marshallers.ManagedReturnMarshaller.TypeInfo).managed),
                        invoke));
        }

        private static ImmutableArray<CatchClauseSyntax> GenerateCatchClauseForManagedException(BoundGenerators marshallers, StubCodeContext context)
        {
            if (!marshallers.HasManagedExceptionMarshaller)
            {
                return ImmutableArray<CatchClauseSyntax>.Empty;
            }
            ImmutableArray<StatementSyntax>.Builder catchClauseBuilder = ImmutableArray.CreateBuilder<StatementSyntax>();

            BoundGenerator managedExceptionMarshaller = marshallers.ManagedExceptionMarshaller;

            var (managed, _) = context.GetIdentifiers(managedExceptionMarshaller.TypeInfo);

            catchClauseBuilder.AddRange(
                managedExceptionMarshaller.Generator.Generate(
                    managedExceptionMarshaller.TypeInfo, context with { CurrentStage = StubCodeContext.Stage.Marshal }));
            catchClauseBuilder.AddRange(
                managedExceptionMarshaller.Generator.Generate(
                    managedExceptionMarshaller.TypeInfo, context with { CurrentStage = StubCodeContext.Stage.PinnedMarshal }));
            return ImmutableArray.Create(
                CatchClause(
                    CatchDeclaration(ParseTypeName(TypeNames.System_Exception), Identifier(managed)),
                    filter: null,
                    Block(List(catchClauseBuilder))));
        }

        private static SyntaxTriviaList GenerateStageTrivia(StubCodeContext.Stage stage)
        {
            string comment = stage switch
            {
                StubCodeContext.Stage.Setup => "Perform required setup.",
                StubCodeContext.Stage.Marshal => "Convert managed data to native data.",
                StubCodeContext.Stage.Pin => "Pin data in preparation for calling the P/Invoke.",
                StubCodeContext.Stage.PinnedMarshal => "Convert managed data to native data that requires the managed data to be pinned.",
                StubCodeContext.Stage.Invoke => "Call the P/Invoke.",
                StubCodeContext.Stage.UnmarshalCapture => "Capture the native data into marshaller instances in case conversion to managed data throws an exception.",
                StubCodeContext.Stage.Unmarshal => "Convert native data to managed data.",
                StubCodeContext.Stage.Cleanup => "Perform required cleanup.",
                StubCodeContext.Stage.NotifyForSuccessfulInvoke => "Keep alive any managed objects that need to stay alive across the call.",
                StubCodeContext.Stage.GuaranteedUnmarshal => "Convert native data to managed data even in the case of an exception during the non-cleanup phases.",
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };

            // Comment separating each stage
            return TriviaList(Comment($"// {stage} - {comment}"));
        }
    }
}
