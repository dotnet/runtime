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
        public ImmutableArray<StatementSyntax> CleanupCallerAllocated { get; init; }
        public ImmutableArray<StatementSyntax> CleanupCalleeAllocated { get; init; }

        public ImmutableArray<CatchClauseSyntax> ManagedExceptionCatchClauses { get; init; }

        public static GeneratedStatements Create(BoundGenerators marshallers, StubIdentifierContext context)
        {
            return new GeneratedStatements
            {
                Setup = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Setup }),
                Marshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Marshal }),
                Pin = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Pin }).Cast<FixedStatementSyntax>().ToImmutableArray(),
                PinnedMarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.PinnedMarshal }),
                InvokeStatement = EmptyStatement(),
                Unmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.UnmarshalCapture })
                            .AddRange(GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Unmarshal })),
                NotifyForSuccessfulInvoke = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.NotifyForSuccessfulInvoke }),
                GuaranteedUnmarshal = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.GuaranteedUnmarshal }),
                CleanupCallerAllocated = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.CleanupCallerAllocated }),
                CleanupCalleeAllocated = GenerateStatementsForStubContext(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.CleanupCalleeAllocated }),
                ManagedExceptionCatchClauses = GenerateCatchClauseForManagedException(marshallers, context)
            };
        }
        public static GeneratedStatements Create(BoundGenerators marshallers, StubIdentifierContext context, ExpressionSyntax expressionToInvoke)
        {
            GeneratedStatements statements = Create(marshallers, context);

            if (context.CodeContext.Direction == MarshalDirection.ManagedToUnmanaged)
            {
                return statements with
                {
                    InvokeStatement = GenerateStatementForNativeInvoke(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Invoke }, expressionToInvoke)
                };
            }
            else if (context.CodeContext.Direction == MarshalDirection.UnmanagedToManaged)
            {
                return statements with
                {
                    InvokeStatement = GenerateStatementForManagedInvoke(marshallers, context with { CurrentStage = StubIdentifierContext.Stage.Invoke }, expressionToInvoke)
                };
            }
            else
            {
                throw new ArgumentException("Direction must be ManagedToUnmanaged or UnmanagedToManaged");
            }
        }

        private static ImmutableArray<StatementSyntax> GenerateStatementsForStubContext(BoundGenerators marshallers, StubIdentifierContext context)
        {
            ImmutableArray<StatementSyntax>.Builder statementsToUpdate = ImmutableArray.CreateBuilder<StatementSyntax>();
            foreach (IBoundMarshallingGenerator marshaller in marshallers.SignatureMarshallers)
            {
                statementsToUpdate.AddRange(marshaller.Generate(context));
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

        private static ExpressionStatementSyntax GenerateStatementForNativeInvoke(BoundGenerators marshallers, StubIdentifierContext context, ExpressionSyntax expressionToInvoke)
        {
            if (context.CurrentStage != StubIdentifierContext.Stage.Invoke)
            {
                throw new ArgumentException("CurrentStage must be Invoke");
            }
            InvocationExpressionSyntax invoke = InvocationExpression(expressionToInvoke);
            // Generate code for each parameter for the current stage
            foreach (IBoundMarshallingGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                // Get arguments for invocation
                ArgumentSyntax argSyntax = marshaller.AsArgument(context);
                invoke = invoke.AddArgumentListArguments(argSyntax);
            }
            // Assign to return value if necessary
            if (marshallers.NativeReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void)
            {
                return ExpressionStatement(invoke);
            }

            var (managed, native) = context.GetIdentifiers(marshallers.NativeReturnMarshaller.TypeInfo);

            string targetIdentifier = marshallers.NativeReturnMarshaller.UsesNativeIdentifier(context.CodeContext)
                ? native
                : managed;

            return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(targetIdentifier),
                        invoke));
        }


        private static ExpressionStatementSyntax GenerateStatementForManagedInvoke(BoundGenerators marshallers, StubIdentifierContext context, ExpressionSyntax expressionToInvoke)
        {
            if (context.CurrentStage != StubIdentifierContext.Stage.Invoke)
            {
                throw new ArgumentException("CurrentStage must be Invoke");
            }
            InvocationExpressionSyntax invoke = InvocationExpression(expressionToInvoke);
            // Generate code for each parameter for the current stage
            foreach (IBoundMarshallingGenerator marshaller in marshallers.ManagedParameterMarshallers)
            {
                // Get arguments for invocation
                ArgumentSyntax argSyntax = marshaller.AsManagedArgument(context);
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

        private static ImmutableArray<CatchClauseSyntax> GenerateCatchClauseForManagedException(BoundGenerators marshallers, StubIdentifierContext context)
        {
            if (!marshallers.HasManagedExceptionMarshaller)
            {
                return ImmutableArray<CatchClauseSyntax>.Empty;
            }
            ImmutableArray<StatementSyntax>.Builder catchClauseBuilder = ImmutableArray.CreateBuilder<StatementSyntax>();

            IBoundMarshallingGenerator managedExceptionMarshaller = marshallers.ManagedExceptionMarshaller;

            var (managed, _) = context.GetIdentifiers(managedExceptionMarshaller.TypeInfo);

            catchClauseBuilder.AddRange(
                managedExceptionMarshaller.Generate(context with { CurrentStage = StubIdentifierContext.Stage.Marshal }));
            catchClauseBuilder.AddRange(
                managedExceptionMarshaller.Generate(context with { CurrentStage = StubIdentifierContext.Stage.PinnedMarshal }));
            return ImmutableArray.Create(
                CatchClause(
                    CatchDeclaration(TypeSyntaxes.System_Exception, Identifier(managed)),
                    filter: null,
                    Block(List(catchClauseBuilder))));
        }

        private static SyntaxTriviaList GenerateStageTrivia(StubIdentifierContext.Stage stage)
        {
            string comment = stage switch
            {
                StubIdentifierContext.Stage.Setup => "Perform required setup.",
                StubIdentifierContext.Stage.Marshal => "Convert managed data to native data.",
                StubIdentifierContext.Stage.Pin => "Pin data in preparation for calling the P/Invoke.",
                StubIdentifierContext.Stage.PinnedMarshal => "Convert managed data to native data that requires the managed data to be pinned.",
                StubIdentifierContext.Stage.Invoke => "Call the P/Invoke.",
                StubIdentifierContext.Stage.UnmarshalCapture => "Capture the native data into marshaller instances in case conversion to managed data throws an exception.",
                StubIdentifierContext.Stage.Unmarshal => "Convert native data to managed data.",
                StubIdentifierContext.Stage.CleanupCallerAllocated => "Perform cleanup of caller allocated resources.",
                StubIdentifierContext.Stage.CleanupCalleeAllocated => "Perform cleanup of callee allocated resources.",
                StubIdentifierContext.Stage.NotifyForSuccessfulInvoke => "Keep alive any managed objects that need to stay alive across the call.",
                StubIdentifierContext.Stage.GuaranteedUnmarshal => "Convert native data to managed data even in the case of an exception during the non-cleanup phases.",
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };

            // Comment separating each stage
            return TriviaList(Comment($"// {stage} - {comment}"));
        }
    }
}
