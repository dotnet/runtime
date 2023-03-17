// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class UnmanagedToManagedStubGenerator
    {
        private const string ReturnIdentifier = "__retVal";
        private const string InvokeSucceededIdentifier = "__invokeSucceeded";

        private readonly BoundGenerators _marshallers;

        private readonly NativeToManagedStubCodeContext _context;

        public UnmanagedToManagedStubGenerator(
            TargetFramework targetFramework,
            Version targetFrameworkVersion,
            ImmutableArray<TypePositionInfo> argTypes,
            Action<TypePositionInfo, MarshallingNotSupportedException> marshallingNotSupportedCallback,
            IMarshallingGeneratorFactory generatorFactory)
        {
            _context = new NativeToManagedStubCodeContext(targetFramework, targetFrameworkVersion, ReturnIdentifier, ReturnIdentifier);
            _marshallers = BoundGenerators.Create(argTypes, generatorFactory, _context, new Forwarder(), out var bindingFailures);

            foreach (var failure in bindingFailures)
            {
                marshallingNotSupportedCallback(failure.Info, failure.Exception);
            }

            if (_marshallers.NativeReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.NativeReturnMarshaller.TypeInfo, _context))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                _context = new NativeToManagedStubCodeContext(targetFramework, targetFrameworkVersion, ReturnIdentifier, $"{ReturnIdentifier}{StubCodeContext.GeneratedNativeIdentifierSuffix}");
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
        public BlockSyntax GenerateStubBody(ExpressionSyntax methodToInvoke)
        {
            List<StatementSyntax> setupStatements = new();
            GeneratedStatements statements = GeneratedStatements.Create(
                _marshallers,
                _context,
                methodToInvoke);
            bool shouldInitializeVariables =
                !statements.GuaranteedUnmarshal.IsEmpty
                || !statements.Cleanup.IsEmpty
                || !statements.ManagedExceptionCatchClauses.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForUnmanagedToManaged(_marshallers, _context, shouldInitializeVariables);

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                setupStatements.Add(MarshallerHelpers.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            List<StatementSyntax> tryStatements = new();
            tryStatements.AddRange(statements.Unmarshal);

            tryStatements.Add(statements.InvokeStatement);

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.PinnedMarshal);
            tryStatements.AddRange(statements.Marshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new();
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal)));
            }

            SyntaxList<CatchClauseSyntax> catchClauses = List(statements.ManagedExceptionCatchClauses);

            finallyStatements.AddRange(statements.Cleanup);
            if (finallyStatements.Count > 0)
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
