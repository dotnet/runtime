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
    /// <summary>
    /// Base code generator for generating the body of a source-generated P/Invoke and providing customization for how to invoke/define the native method.
    /// </summary>
    /// <remarks>
    /// This type enables multiple code generators for P/Invoke-style marshalling
    /// to reuse the same basic method body, but with different designs of how to emit the target native method.
    /// This enables users to write code generators that work with slightly different semantics.
    /// For example, the source generator for [LibraryImport] emits the target P/Invoke as
    /// a local function inside the generated stub body.
    /// However, other managed-to-native code generators using a P/Invoke style might want to define
    /// the target DllImport outside of the stub as a static non-local function or as a function pointer field.
    /// This refactoring allows the code generator to have control over where the target method is declared
    /// and how it is declared.
    /// </remarks>
    internal sealed class PInvokeStubCodeGenerator
    {
        public bool SupportsTargetFramework { get; }

        public bool StubIsBasicForwarder { get; }

        /// <summary>
        /// Identifier for managed return value
        /// </summary>
        private const string ReturnIdentifier = "__retVal";
        private const string LastErrorIdentifier = "__lastError";
        private const string InvokeSucceededIdentifier = "__invokeSucceeded";

        // Error code representing success. This maps to S_OK for Windows HRESULT semantics and 0 for POSIX errno semantics.
        private const int SuccessErrorCode = 0;

        private readonly bool _setLastError;
        private readonly BoundGenerators _marshallers;

        private readonly ManagedToNativeStubCodeContext _context;

        public PInvokeStubCodeGenerator(
            StubEnvironment environment,
            ImmutableArray<TypePositionInfo> argTypes,
            bool setLastError,
            Action<TypePositionInfo, MarshallingNotSupportedException> marshallingNotSupportedCallback,
            IMarshallingGeneratorFactory generatorFactory)
        {
            _setLastError = setLastError;

            // Support for SetLastError logic requires .NET 6+. Initialize the
            // supports target framework value with this value.
            if (_setLastError)
            {
                SupportsTargetFramework = environment.TargetFramework == TargetFramework.Net
                    && environment.TargetFrameworkVersion.Major >= 6;
            }
            else
            {
                SupportsTargetFramework = true;
            }

            _context = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, ReturnIdentifier);
            _marshallers = new BoundGenerators(argTypes, CreateGenerator);

            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, _context))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                _context = new ManagedToNativeStubCodeContext(environment, ReturnIdentifier, $"{ReturnIdentifier}{StubCodeContext.GeneratedNativeIdentifierSuffix}");
            }

            bool noMarshallingNeeded = true;

            foreach (BoundGenerator generator in _marshallers.AllMarshallers)
            {
                // Check if marshalling info and generator support the current target framework.
                SupportsTargetFramework &= generator.TypeInfo.MarshallingAttributeInfo is not MissingSupportMarshallingInfo
                    && generator.Generator.IsSupported(environment.TargetFramework, environment.TargetFrameworkVersion);

                // Check if generator is either blittable or just a forwarder.
                noMarshallingNeeded &= generator is { Generator: BlittableMarshaller, TypeInfo.IsByRef: false }
                        or { Generator: Forwarder };
            }

            StubIsBasicForwarder = !setLastError
                && _marshallers.ManagedNativeSameReturn // If the managed return has native return position, then it's the return for both.
                && noMarshallingNeeded;

            IMarshallingGenerator CreateGenerator(TypePositionInfo p)
            {
                try
                {
                    return generatorFactory.Create(p, _context);
                }
                catch (MarshallingNotSupportedException e)
                {
                    marshallingNotSupportedCallback(p, e);
                    return new Forwarder();
                }
            }
        }

        /// <summary>
        /// Generate the method body of the p/invoke stub.
        /// </summary>
        /// <param name="dllImportName">Name of the target DllImport function to invoke</param>
        /// <returns>Method body of the p/invoke stub</returns>
        /// <remarks>
        /// The generated code assumes it will be in an unsafe context.
        /// </remarks>
        public BlockSyntax GeneratePInvokeBody(string dllImportName)
        {
            GeneratedStatements statements = GeneratedStatements.Create(_marshallers, _context, IdentifierName(dllImportName));
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.Cleanup.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToNative(_marshallers, _context, shouldInitializeVariables);

            var setupStatements = new List<StatementSyntax>();

            if (_setLastError)
            {
                // Declare variable for last error
                setupStatements.Add(MarshallerHelpers.Declare(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    LastErrorIdentifier,
                    initializeToDefault: false));
            }

            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                setupStatements.Add(MarshallerHelpers.Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(statements.Marshal);

            BlockSyntax fixedBlock = Block(statements.PinnedMarshal);
            if (_setLastError)
            {
                StatementSyntax clearLastError = MarshallerHelpers.CreateClearLastSystemErrorStatement(SuccessErrorCode);

                StatementSyntax getLastError = MarshallerHelpers.CreateGetLastSystemErrorStatement(LastErrorIdentifier);

                fixedBlock = fixedBlock.AddStatements(clearLastError, statements.InvokeStatement, getLastError);
            }
            else
            {
                fixedBlock = fixedBlock.AddStatements(statements.InvokeStatement);
            }
            tryStatements.Add(statements.Pin.NestFixedStatements(fixedBlock));
            // <invokeSucceeded> = true;
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);
            tryStatements.AddRange(statements.Unmarshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
            if (!statements.GuaranteedUnmarshal.IsEmpty)
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal)));
            }

            finallyStatements.AddRange(statements.Cleanup);
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

            if (_setLastError)
            {
                // Marshal.SetLastPInvokeError(<lastError>);
                allStatements.Add(MarshallerHelpers.CreateSetLastPInvokeErrorStatement(LastErrorIdentifier));
            }

            // Return
            if (!_marshallers.IsManagedVoidReturn)
                allStatements.Add(ReturnStatement(IdentifierName(_context.GetIdentifiers(_marshallers.ManagedReturnMarshaller.TypeInfo).managed)));

            return Block(allStatements);
        }

        public (ParameterListSyntax ParameterList, TypeSyntax ReturnType, AttributeListSyntax? ReturnTypeAttributes) GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData();
        }
    }
}
