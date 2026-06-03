// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class UnmanagedToManagedStubGenerator
    {
        private const string ReturnIdentifier = "__retVal";

        private readonly BoundGenerators _marshallers;

        private readonly StubIdentifierContext _context;

        public UnmanagedToManagedStubGenerator(
            ImmutableArray<TypePositionInfo> argTypes,
            GeneratorDiagnosticsBag diagnosticsBag,
            IMarshallingGeneratorResolver generatorResolver)
        {
            _marshallers = BoundGenerators.Create(argTypes, generatorResolver, StubCodeContext.DefaultNativeToManagedStub, new Forwarder(), out var bindingDiagnostics);

            diagnosticsBag.ReportGeneratorDiagnostics(bindingDiagnostics);

            if (_marshallers.NativeReturnMarshaller.UsesNativeIdentifier)
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                _context = new DefaultIdentifierContext(ReturnIdentifier, $"{ReturnIdentifier}{StubIdentifierContext.GeneratedNativeIdentifierSuffix}", MarshalDirection.UnmanagedToManaged);
            }
            else
            {
                _context = new DefaultIdentifierContext(ReturnIdentifier, ReturnIdentifier, MarshalDirection.UnmanagedToManaged);
            }
        }

        /// <summary>
        /// Generate the method body of the unmanaged-to-managed ComWrappers-based method stub.
        /// </summary>
        /// <param name="methodToInvoke">Name of the method on the managed type to invoke</param>
        /// <returns>Method body of the stub</returns>
        /// <remarks>
        /// The generated code assumes it will be in an unsafe context.
        /// </remarks>
        public BlockSyntax GenerateStubBodyForMethod(ExpressionSyntax methodToInvoke)
        {
            GeneratedStatements statements = GeneratedStatements.Create(
                _marshallers,
                StubCodeContext.DefaultNativeToManagedStub,
                _context, methodToInvoke);
            return BuildBodyFromStatements(statements);
        }

        /// <summary>
        /// Generate the method body of the unmanaged-to-managed ComWrappers-based stub for a property
        /// accessor. The natural <c>methodToInvoke(args)</c> shape doesn't apply — for a getter the
        /// body assigns the property read into the return slot (<c>retVal = propertyAccess</c>) and
        /// for a setter it assigns the value parameter into the property (<c>propertyAccess = value</c>).
        /// </summary>
        /// <param name="propertyAccess">Member-access expression naming the managed-side property
        /// (e.g., <c>@this.Foo</c>).</param>
        /// <param name="isSetter">True if this stub is the property setter; false for the getter.</param>
        public BlockSyntax GenerateStubBodyForProperty(ExpressionSyntax propertyAccess, bool isSetter)
        {
            GeneratedStatements statements = GeneratedStatements.CreateForProperty(
                _marshallers,
                _context,
                propertyAccess,
                isSetter);
            return BuildBodyFromStatements(statements);
        }

        /// <summary>
        /// Generate the method body of the unmanaged-to-managed ComWrappers-based stub for an indexer
        /// accessor. Wraps the supplied <paramref name="instance"/> in an
        /// <see cref="ElementAccessExpressionSyntax"/> built from the marshalled identifiers for the
        /// index parameters (which are all managed parameters for an indexer getter, and all-but-the-last
        /// managed parameter for an indexer setter — the trailing entry is the implicit
        /// <c>value</c>). The resulting access expression is then handled by the same property-stub
        /// pipeline.
        /// </summary>
        /// <param name="instance">Expression naming the managed-side target instance (e.g.,
        /// <c>@this</c>); the bracketed argument list is appended to this expression.</param>
        /// <param name="isSetter">True if this stub is the indexer setter; false for the getter.</param>
        public BlockSyntax GenerateStubBodyForIndexer(ExpressionSyntax instance, bool isSetter)
        {
            var managedParameterMarshallers = _marshallers.ManagedParameterMarshallers;
            int indexCount = isSetter ? managedParameterMarshallers.Length - 1 : managedParameterMarshallers.Length;
            var argBuilder = ImmutableArray.CreateBuilder<ArgumentSyntax>(indexCount);
            for (int i = 0; i < indexCount; i++)
            {
                argBuilder.Add(managedParameterMarshallers[i].AsManagedArgument(_context));
            }

            ExpressionSyntax elementAccess = ElementAccessExpression(
                instance,
                BracketedArgumentList(SeparatedList(argBuilder.MoveToImmutable())));

            return GenerateStubBodyForProperty(elementAccess, isSetter);
        }

        private BlockSyntax BuildBodyFromStatements(GeneratedStatements statements)
        {
            Debug.Assert(statements.CleanupCalleeAllocated.IsEmpty);

            bool shouldInitializeVariables =
                !statements.GuaranteedUnmarshal.IsEmpty
                || !statements.CleanupCallerAllocated.IsEmpty
                || !statements.ManagedExceptionCatchClauses.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForUnmanagedToManaged(_marshallers, _context, shouldInitializeVariables);

            List<StatementSyntax> setupStatements =
            [
                .. declarations.Initializations,
                .. declarations.Variables,
                .. statements.Setup,
            ];

            List<StatementSyntax> tryStatements =
            [
                .. statements.GuaranteedUnmarshal,
                .. statements.Unmarshal,
                statements.InvokeStatement,
                .. statements.NotifyForSuccessfulInvoke,
                .. statements.Marshal,
                .. statements.PinnedMarshal,
            ];

            List<StatementSyntax> allStatements = setupStatements;

            SyntaxList<CatchClauseSyntax> catchClauses = List(statements.ManagedExceptionCatchClauses);

            ImmutableArray<StatementSyntax> finallyStatements = statements.CleanupCallerAllocated;
            if (finallyStatements.Length > 0)
            {
                allStatements.Add(
                    TryStatement(Block(tryStatements), catchClauses, FinallyClause(Block(finallyStatements))));
            }
            else if (catchClauses.Count > 0)
            {
                allStatements.Add(
                    TryStatement(Block(tryStatements), catchClauses, @finally: null));
            }
            else
            {
                allStatements.AddRange(tryStatements);
            }

            // Return
            if (!_marshallers.IsUnmanagedVoidReturn)
                allStatements.Add(ReturnStatement(IdentifierName(_context.GetIdentifiers(_marshallers.NativeReturnMarshaller.TypeInfo).native)));

            return Block(allStatements);
        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateAbiMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
