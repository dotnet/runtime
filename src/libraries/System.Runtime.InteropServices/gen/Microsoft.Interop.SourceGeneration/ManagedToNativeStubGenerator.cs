﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    /// <summary>
    /// Base code generator for generating the body of a source-generated managed-to-unmanaged stub and providing customization for how to invoke/define the native method.
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
    public sealed class ManagedToNativeStubGenerator
    {
        public bool NoMarshallingRequired { get; }

        public bool HasForwardedTypes { get; }

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

        private readonly DefaultIdentifierContext _context;

        public ManagedToNativeStubGenerator(
            ImmutableArray<TypePositionInfo> argTypes,
            bool setLastError,
            GeneratorDiagnosticsBag diagnosticsBag,
            IMarshallingGeneratorResolver generatorResolver,
            CodeEmitOptions codeEmitOptions)
        {
            _setLastError = setLastError;

            _marshallers = BoundGenerators.Create(argTypes, generatorResolver, StubCodeContext.DefaultManagedToNativeStub, new Forwarder(), out var bindingDiagnostics);

            diagnosticsBag.ReportGeneratorDiagnostics(bindingDiagnostics);

            if (_marshallers.ManagedReturnMarshaller.UsesNativeIdentifier)
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                _context = new DefaultIdentifierContext(ReturnIdentifier, $"{ReturnIdentifier}{StubIdentifierContext.GeneratedNativeIdentifierSuffix}", MarshalDirection.ManagedToUnmanaged)
                {
                    CodeEmitOptions = codeEmitOptions
                };
            }
            else
            {
                _context = new DefaultIdentifierContext(ReturnIdentifier, ReturnIdentifier, MarshalDirection.ManagedToUnmanaged)
                {
                    CodeEmitOptions = codeEmitOptions
                };
            }

            bool noMarshallingNeeded = true;

            foreach (IBoundMarshallingGenerator generator in _marshallers.SignatureMarshallers)
            {
                // Check if generator is either blittable or just a forwarder.
                noMarshallingNeeded &= (generator.IsBlittable() && !generator.TypeInfo.IsByRef) || generator.IsForwarder();

                // Track if any generators are just forwarders - for types other than void, this indicates
                // types that can't be marshalled by source-generated code.
                HasForwardedTypes |= generator.IsForwarder() && generator is { TypeInfo.ManagedType: not SpecialTypeInfo { SpecialType: Microsoft.CodeAnalysis.SpecialType.System_Void } };
            }

            NoMarshallingRequired = !setLastError
                && _marshallers.ManagedNativeSameReturn
                && noMarshallingNeeded;
        }

        public string GetNativeIdentifier(TypePositionInfo info)
        {
            return _context.GetIdentifiers(info).native;
        }

        /// <summary>
        /// Generate the method body of the managed-to-unmanaged stub.
        /// </summary>
        /// <param name="targetIdentifier">Name of the target function, function pointer, or delegate to invoke</param>
        /// <returns>Method body of the managed-to-unmanaged stub</returns>
        /// <remarks>
        /// The generated code assumes it will be in an unsafe context.
        /// </remarks>
        public BlockSyntax GenerateStubBody(string targetIdentifier)
        {
            GeneratedStatements statements = GeneratedStatements.Create(_marshallers, StubCodeContext.DefaultManagedToNativeStub, _context, IdentifierName(targetIdentifier));
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.CleanupCallerAllocated.IsEmpty || !statements.CleanupCalleeAllocated.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToUnmanaged(_marshallers, _context, shouldInitializeVariables);

            List<StatementSyntax> setupStatements = [];

            if (_setLastError)
            {
                // Declare variable for last error
                setupStatements.Add(Declare(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    LastErrorIdentifier,
                    initializeToDefault: false));
            }

            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                setupStatements.Add(Declare(PredefinedType(Token(SyntaxKind.BoolKeyword)), InvokeSucceededIdentifier, initializeToDefault: true));
            }

            setupStatements.AddRange(declarations.Initializations);
            setupStatements.AddRange(declarations.Variables);
            setupStatements.AddRange(statements.Setup);

            List<StatementSyntax> tryStatements = [.. statements.Marshal];

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

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);

            // <invokeSucceeded> = true;
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            tryStatements.AddRange(statements.Unmarshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = [];
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                finallyStatements.Add(IfStatement(IdentifierName(InvokeSucceededIdentifier), Block(statements.GuaranteedUnmarshal.Concat(statements.CleanupCalleeAllocated))));
            }

            finallyStatements.AddRange(statements.CleanupCallerAllocated);
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
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
