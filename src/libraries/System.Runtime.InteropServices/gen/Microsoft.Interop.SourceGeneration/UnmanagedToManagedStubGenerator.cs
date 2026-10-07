// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;

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
                _context = new DefaultIdentifierContext(ReturnIdentifier, $"{ReturnIdentifier}{StubIdentifierContext.GeneratedNativeIdentifierSuffix}", MarshalDirection.UnmanagedToManaged);
            }
            else
            {
                _context = new DefaultIdentifierContext(ReturnIdentifier, ReturnIdentifier, MarshalDirection.UnmanagedToManaged);
            }
        }

        /// <summary>Generates the braced body of an unmanaged-to-managed method stub.</summary>
        /// <param name="methodToInvoke">The managed method access expression.</param>
        /// <returns>The method body, which requires an unsafe context.</returns>
        public string GenerateStubBodyForMethod(string methodToInvoke)
        {
            GeneratedStatements statements = GeneratedStatements.Create(
                _marshallers,
                StubCodeContext.DefaultNativeToManagedStub,
                _context,
                methodToInvoke);
            return BuildBodyFromStatements(statements);
        }

        /// <summary>Generates a property accessor body.</summary>
        /// <param name="propertyAccess">The managed property access expression.</param>
        /// <param name="isSetter">True for a setter; false for a getter.</param>
        /// <returns>The accessor body.</returns>
        public string GenerateStubBodyForProperty(string propertyAccess, bool isSetter)
        {
            GeneratedStatements statements = GeneratedStatements.CreateForProperty(_marshallers, _context, propertyAccess, isSetter);
            return BuildBodyFromStatements(statements);
        }

        /// <summary>Generates an indexer accessor using the marshalled index arguments.</summary>
        /// <param name="instance">The managed target instance expression.</param>
        /// <param name="isSetter">True for a setter; false for a getter.</param>
        /// <returns>The accessor body.</returns>
        public string GenerateStubBodyForIndexer(string instance, bool isSetter)
        {
            ImmutableArray<IBoundMarshallingGenerator> parameters = _marshallers.ManagedParameterMarshallers;
            int indexCount = isSetter ? parameters.Length - 1 : parameters.Length;
            var arguments = new string[indexCount];
            for (int i = 0; i < indexCount; i++)
            {
                arguments[i] = parameters[i].AsManagedArgument(_context);
            }
            return GenerateStubBodyForProperty($"{instance}[{string.Join(", ", arguments)}]", isSetter);
        }

        private string BuildBodyFromStatements(GeneratedStatements statements)
        {
            Debug.Assert(statements.CleanupCalleeAllocated.Length == 0);
            bool shouldInitializeVariables = statements.GuaranteedUnmarshal.Length != 0
                || statements.CleanupCallerAllocated.Length != 0
                || statements.ManagedExceptionCatchClauses.Length != 0;
            VariableDeclarations declarations = VariableDeclarations.GenerateDeclarationsForUnmanagedToManaged(_marshallers, _context, shouldInitializeVariables);
            var writer = new IndentedTextWriter();
            using (writer.WriteBlock())
            {
                writer.Write(declarations.Initializations);
                writer.Write(declarations.Variables);
                writer.Write(statements.Setup);

                bool needsTry = statements.ManagedExceptionCatchClauses.Length != 0 || statements.CleanupCallerAllocated.Length != 0;
                if (needsTry)
                {
                    writer.WriteLine("try");
                    using (writer.WriteBlock())
                    {
                        WriteTryStatements();
                    }
                    writer.Write(statements.ManagedExceptionCatchClauses);
                    if (statements.CleanupCallerAllocated.Length != 0)
                    {
                        writer.WriteLine("finally");
                        using (writer.WriteBlock())
                        {
                            writer.Write(statements.CleanupCallerAllocated);
                        }
                    }
                }
                else
                {
                    WriteTryStatements();
                }

                if (!_marshallers.IsUnmanagedVoidReturn)
                {
                    writer.WriteLine($"return {_context.GetIdentifiers(_marshallers.NativeReturnMarshaller.TypeInfo).native};");
                }
            }
            return writer.ToString();

            void WriteTryStatements()
            {
                writer.Write(statements.ErrorUnmarshalCapture);
                writer.Write(statements.ErrorUnmarshal);
                writer.Write(statements.GuaranteedUnmarshal);
                writer.Write(statements.Unmarshal);
                writer.Write(statements.InvokeStatement);
                writer.Write(statements.NotifyForSuccessfulInvoke);
                writer.Write(statements.Marshal);
                writer.Write(statements.PinnedMarshal);
            }
        }

        public GeneratedMethodSignature GenerateAbiMethodSignatureData()
        {
            return _marshallers.GenerateTargetMethodSignatureData(_context);
        }
    }
}
