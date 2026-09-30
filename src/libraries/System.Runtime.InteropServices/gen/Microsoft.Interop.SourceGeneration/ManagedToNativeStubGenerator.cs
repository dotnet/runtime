// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    /// <summary>
    /// Generates the body of a managed-to-unmanaged stub independently of how the native target is declared.
    /// </summary>
    public sealed class ManagedToNativeStubGenerator
    {
        public bool NoMarshallingRequired { get; }

        public bool HasForwardedTypes { get; }

        private const string ReturnIdentifier = "__retVal";
        private const string LastErrorIdentifier = "__lastError";
        private const string InvokeSucceededIdentifier = "__invokeSucceeded";
        private const string ErrorValueCapturedIdentifier = "__errorValueCaptured";

        // This maps to S_OK for Windows HRESULT semantics and zero for POSIX errno semantics.
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

            TypePositionInfo? errorHandlingInfo = argTypes.FirstOrDefault(static info => info.IsErrorHandlingPosition);
            TypePositionInfo? errorHandlingOverlappedPosition = errorHandlingInfo is null
                ? null
                : argTypes.FirstOrDefault(info => !info.IsErrorHandlingPosition && info.NativeIndex == errorHandlingInfo.NativeIndex);

            if (_marshallers.ManagedReturnMarshaller.UsesNativeIdentifier)
            {
                _context = new DefaultIdentifierContext(
                    ReturnIdentifier,
                    $"{ReturnIdentifier}{StubIdentifierContext.GeneratedNativeIdentifierSuffix}",
                    MarshalDirection.ManagedToUnmanaged,
                    errorHandlingOverlappedPosition)
                {
                    CodeEmitOptions = codeEmitOptions
                };
            }
            else
            {
                _context = new DefaultIdentifierContext(
                    ReturnIdentifier,
                    ReturnIdentifier,
                    MarshalDirection.ManagedToUnmanaged,
                    errorHandlingOverlappedPosition)
                {
                    CodeEmitOptions = codeEmitOptions
                };
            }

            bool noMarshallingNeeded = true;
            bool hasErrorHandler = false;

            foreach (IBoundMarshallingGenerator generator in _marshallers.SignatureMarshallers)
            {
                hasErrorHandler |= generator.TypeInfo.IsErrorHandlingPosition;
                noMarshallingNeeded &= (generator.IsBlittable() && !generator.TypeInfo.IsByRef) || generator.IsForwarder();
                HasForwardedTypes |= generator.IsForwarder() && generator is { TypeInfo.ManagedType: not SpecialTypeInfo { SpecialType: Microsoft.CodeAnalysis.SpecialType.System_Void } };
            }

            NoMarshallingRequired = !setLastError
                && !hasErrorHandler
                && _marshallers.ManagedNativeSameReturn
                && noMarshallingNeeded;
        }

        public string GetNativeIdentifier(TypePositionInfo info)
        {
            return _context.GetIdentifiers(info).native;
        }

        /// <summary>Generates the complete, braced method body in an unsafe context.</summary>
        /// <param name="targetIdentifier">The function, function pointer, or delegate to invoke.</param>
        /// <returns>The method body.</returns>
        public string GenerateStubBody(string targetIdentifier)
        {
            var writer = new IndentedTextWriter();
            GenerateStubBody(writer, targetIdentifier);
            return writer.ToString();
        }

        /// <summary>Writes the complete, braced method body in an unsafe context.</summary>
        /// <param name="writer">The destination writer.</param>
        /// <param name="targetIdentifier">The function, function pointer, or delegate to invoke.</param>
        public void GenerateStubBody(IndentedTextWriter writer, string targetIdentifier)
        {
            using (writer.WriteBlock())
            {
                GenerateStubStatements(writer, targetIdentifier);
            }
        }

        /// <summary>Writes stub statements into a caller-owned block without adding enclosing braces.</summary>
        /// <param name="writer">The destination writer.</param>
        /// <param name="targetIdentifier">The function, function pointer, or delegate to invoke.</param>
        public void GenerateStubStatements(IndentedTextWriter writer, string targetIdentifier)
        {
            GeneratedStatements statements = GeneratedStatements.Create(_marshallers, StubCodeContext.DefaultManagedToNativeStub, _context, targetIdentifier);
            bool shouldInitializeVariables = statements.GuaranteedUnmarshal.Length != 0 || statements.CleanupCallerAllocated.Length != 0 || statements.CleanupCalleeAllocated.Length != 0;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForManagedToUnmanaged(_marshallers, _context, shouldInitializeVariables);
            bool trackInvokeSucceeded = statements.GuaranteedUnmarshal.Length != 0 || statements.CleanupCalleeAllocated.Length != 0;
            bool trackErrorCaptured = statements.ErrorCleanupCalleeAllocated.Length != 0;
            bool hasFinally = trackInvokeSucceeded || trackErrorCaptured || statements.CleanupCallerAllocated.Length != 0;

            if (_setLastError)
            {
                writer.WriteLine($"int {LastErrorIdentifier};");
            }
            if (trackInvokeSucceeded)
            {
                writer.WriteLine($"bool {InvokeSucceededIdentifier} = default;");
            }
            if (trackErrorCaptured)
            {
                writer.WriteLine($"bool {ErrorValueCapturedIdentifier} = default;");
            }

            writer.Write(declarations.Initializations);
            writer.Write(declarations.Variables);
            writer.Write(statements.Setup);

            if (hasFinally)
            {
                writer.WriteLine("try");
                using (writer.WriteBlock())
                {
                    WriteTryStatements();
                }
                writer.WriteLine("finally");
                using (writer.WriteBlock())
                {
                    if (trackErrorCaptured)
                    {
                        writer.WriteLine($"if ({ErrorValueCapturedIdentifier})");
                        using (writer.WriteBlock())
                        {
                            writer.Write(statements.ErrorCleanupCalleeAllocated);
                        }
                    }
                    if (trackInvokeSucceeded)
                    {
                        writer.WriteLine($"if ({InvokeSucceededIdentifier})");
                        using (writer.WriteBlock())
                        {
                            writer.Write(statements.GuaranteedUnmarshal);
                            writer.Write(statements.CleanupCalleeAllocated);
                        }
                    }
                    writer.Write(statements.CleanupCallerAllocated);
                }
            }
            else
            {
                WriteTryStatements();
            }

            if (_setLastError)
            {
                writer.WriteLine(MarshallerHelpers.CreateSetLastPInvokeErrorStatement(LastErrorIdentifier));
            }
            if (!_marshallers.IsManagedVoidReturn)
            {
                writer.WriteLine($"return {_context.GetIdentifiers(_marshallers.ManagedReturnMarshaller.TypeInfo).managed};");
            }

            void WriteTryStatements()
            {
                writer.Write(statements.Marshal);
                writer.Write(statements.Pin);
                using (writer.WriteBlock())
                {
                    writer.Write(statements.PinnedMarshal);
                    if (_setLastError)
                    {
                        writer.WriteLine(MarshallerHelpers.CreateClearLastSystemErrorStatement(SuccessErrorCode));
                    }
                    writer.Write(statements.InvokeStatement);
                    if (_setLastError)
                    {
                        writer.WriteLine(MarshallerHelpers.CreateGetLastSystemErrorStatement(LastErrorIdentifier));
                    }
                }

                writer.Write(statements.NotifyForSuccessfulInvoke);
                if (_setLastError && (statements.ErrorUnmarshalCapture.Length != 0 || statements.ErrorUnmarshal.Length != 0))
                {
                    writer.WriteLine(MarshallerHelpers.CreateSetLastPInvokeErrorStatement(LastErrorIdentifier));
                }

                writer.Write(statements.ErrorUnmarshalCapture);
                if (trackErrorCaptured)
                {
                    writer.WriteLine($"{ErrorValueCapturedIdentifier} = true;");
                }
                writer.Write(statements.ErrorUnmarshal);

                if (trackInvokeSucceeded)
                {
                    writer.WriteLine($"{InvokeSucceededIdentifier} = true;");
                }
                writer.Write(statements.Unmarshal);
            }
        }

        public GeneratedMethodSignature GenerateTargetMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
