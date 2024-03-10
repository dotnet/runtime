// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

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
    internal sealed class ManagedToNativeVTableMethodGenerator
    {
        private const string ReturnIdentifier = "__retVal";
        private const string LastErrorIdentifier = "__lastError";
        private const string InvokeSucceededIdentifier = "__invokeSucceeded";
        private const string NativeThisParameterIdentifier = "__this";
        private const string VirtualMethodTableIdentifier = $"__vtable{StubCodeContext.GeneratedNativeIdentifierSuffix}";

        // Error code representing success. This maps to S_OK for Windows HRESULT semantics and 0 for POSIX errno semantics.
        private const int SuccessErrorCode = 0;
        private readonly bool _setLastError;
        private readonly BoundGenerators _marshallers;

        private readonly ManagedToNativeStubCodeContext _context;

        public ManagedToNativeVTableMethodGenerator(
            ImmutableArray<TypePositionInfo> argTypes,
            bool setLastError,
            bool implicitThis,
            GeneratorDiagnosticsBag diagnosticsBag,
            IMarshallingGeneratorResolver generatorResolver)
        {
            _setLastError = setLastError;
            if (implicitThis)
            {
                ImmutableArray<TypePositionInfo>.Builder newArgTypes = ImmutableArray.CreateBuilder<TypePositionInfo>(argTypes.Length + 1);
                newArgTypes.Add(new TypePositionInfo(new PointerTypeInfo("void*", "void*", false), NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = NativeThisParameterIdentifier,
                    NativeIndex = 0
                });
                foreach (var arg in argTypes)
                {
                    newArgTypes.Add(arg with
                    {
                        NativeIndex = arg.NativeIndex switch
                        {
                            TypePositionInfo.UnsetIndex or TypePositionInfo.ReturnIndex => arg.NativeIndex,
                            int index => index + 1
                        }
                    });
                }
                argTypes = newArgTypes.ToImmutableArray();
            }

            _context = new ManagedToNativeStubCodeContext(ReturnIdentifier, ReturnIdentifier);
            _marshallers = BoundGenerators.Create(argTypes, generatorResolver, _context, new Forwarder(), out var bindingFailures);

            diagnosticsBag.ReportGeneratorDiagnostics(bindingFailures);

            if (_marshallers.ManagedReturnMarshaller.Generator.UsesNativeIdentifier(_marshallers.ManagedReturnMarshaller.TypeInfo, _context))
            {
                // If we need a different native return identifier, then recreate the context with the correct identifier before we generate any code.
                _context = new ManagedToNativeStubCodeContext(ReturnIdentifier, $"{ReturnIdentifier}{StubCodeContext.GeneratedNativeIdentifierSuffix}");
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
        public BlockSyntax GenerateStubBody(int index, ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv, TypeSyntax containingTypeName)
        {
            var setupStatements = new List<StatementSyntax>
            {
                // var (<thisParameter>, <virtualMethodTable>) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(<containingTypeName>));
                AssignmentStatement(
                        DeclarationExpression(
                            IdentifierName("var"),
                            ParenthesizedVariableDesignation(
                                SeparatedList<VariableDesignationSyntax>(
                                    new[]{
                                        SingleVariableDesignation(
                                            Identifier(NativeThisParameterIdentifier)),
                                        SingleVariableDesignation(
                                            Identifier(VirtualMethodTableIdentifier))}))),
                        MethodInvocation(
                                ParenthesizedExpression(
                                    CastExpression(
                                        TypeSyntaxes.IUnmanagedVirtualMethodTableProvider,
                                        ThisExpression())),
                                IdentifierName("GetVirtualMethodTableInfoForKey"),
                                Argument(TypeOfExpression(containingTypeName))))
            };

            GeneratedStatements statements = GeneratedStatements.Create(
                _marshallers,
                _context,
                CreateFunctionPointerExpression(
                    // <vtableDeclaration>[<index>]
                    IndexExpression(IdentifierName(VirtualMethodTableIdentifier), Argument(IntLiteral(index))),
                    callConv));
            bool shouldInitializeVariables = !statements.GuaranteedUnmarshal.IsEmpty || !statements.CleanupCallerAllocated.IsEmpty || !statements.CleanupCalleeAllocated.IsEmpty;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToUnmanaged(_marshallers, _context, shouldInitializeVariables);

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
            tryStatements.Add(statements.Pin.CastArray<FixedStatementSyntax>().NestFixedStatements(fixedBlock));

            tryStatements.AddRange(statements.NotifyForSuccessfulInvoke);

            // <invokeSucceeded> = true;
            if (!(statements.GuaranteedUnmarshal.IsEmpty && statements.CleanupCalleeAllocated.IsEmpty))
            {
                tryStatements.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(InvokeSucceededIdentifier),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression))));
            }

            // Keep the this object alive across the native call, similar to how we handle marshalling managed delegates.
            // We do this right after the NotifyForSuccessfulInvoke phase as that phase is where the delegate objects are kept alive.
            // If we ever move the "this" object handling out of this type, we'll move the handling to be emitted in that phase.
            // GC.KeepAlive(this);
            tryStatements.Add(
                MethodInvocationStatement(
                    TypeSyntaxes.System_GC,
                    IdentifierName("KeepAlive"),
                    Argument(ThisExpression())));

            tryStatements.AddRange(statements.Unmarshal);

            List<StatementSyntax> allStatements = setupStatements;
            List<StatementSyntax> finallyStatements = new List<StatementSyntax>();
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

            return Block(allStatements.Where(s => s is not EmptyStatementSyntax));
        }

        private ParenthesizedExpressionSyntax CreateFunctionPointerExpression(
            ExpressionSyntax untypedFunctionPointerExpression,
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv)
        {
            List<FunctionPointerParameterSyntax> functionPointerParameters = new();
            var (paramList, retType, _) = _marshallers.GenerateTargetMethodSignatureData(_context);
            functionPointerParameters.AddRange(paramList.Parameters.Select(p => FunctionPointerParameter(attributeLists: default, p.Modifiers, p.Type)));
            functionPointerParameters.Add(FunctionPointerParameter(retType));

            // ((delegate* unmanaged<...>)<untypedFunctionPointerExpression>)
            return ParenthesizedExpression(CastExpression(
                FunctionPointerType(
                    FunctionPointerCallingConvention(Token(SyntaxKind.UnmanagedKeyword), callConv.IsEmpty ? null : FunctionPointerUnmanagedCallingConventionList(SeparatedList(callConv))),
                    FunctionPointerParameterList(SeparatedList(functionPointerParameters))),
                untypedFunctionPointerExpression));
        }
    }
}
