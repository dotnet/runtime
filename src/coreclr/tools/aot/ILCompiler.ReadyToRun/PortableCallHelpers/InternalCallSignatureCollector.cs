// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.PortableCallHelpers
{
    /// <summary>
    /// Reports a condition that should fail the build, with a message that is complete on its own.
    /// </summary>
    internal sealed class LogAsErrorException(string message) : Exception(message);

    /// <summary>
    /// Emits generator diagnostics in the canonical MSBuild format, so that a build driving
    /// crossgen2 through Exec still reports them with their codes.
    /// </summary>
    internal sealed class InteropLogger(Logger logger)
    {
        private readonly HashSet<string> _reportedInfo = [];

        public void Warning(string code, string message)
            => logger.LogMessage($"crossgen2 : warning {code}: {message}");

        /// <summary>
        /// Reports an informational diagnostic once per distinct message, so that a type used by
        /// many signatures does not produce the same line repeatedly.
        /// </summary>
        public void InfoHigh(string code, string message)
        {
            if (_reportedInfo.Add($"{code}:{message}"))
                logger.LogMessage($"crossgen2 : message {code}: {message}");
        }

        public void Verbose(string message)
        {
            if (logger.IsVerbose)
                logger.LogMessage(message);
        }
    }

    /// <summary>
    /// Scans assemblies for methods marked with <c>MethodImplAttributes.InternalCall</c> and
    /// collects the portable entry point signatures the interpreter-to-native thunks are generated
    /// from.
    /// </summary>
    internal sealed class InternalCallSignatureCollector(InteropLogger log)
    {
        private readonly Dictionary<string, MethodDesc> _signatures = [];

        public IReadOnlyDictionary<string, MethodDesc> Signatures => _signatures;

        public void ScanType(EcmaType type)
        {
            foreach (MethodDesc method in type.GetMethods())
            {
                if (!method.IsInternalCall)
                    continue;

                // String constructors never reach a signature-derived thunk. They are compiled as static
                // factories ("String Ctor(args)", see WasmLowering.GetStringCtorActualSignature), and the
                // runtime special-cases them in both directions with hardcoded keys before it consults
                // this table: GetCookieForCalliSig and GetPortableEntryPointToInterpreterThunk in
                // src/coreclr/vm/wasm/helpers.cpp. Emitting the declared "void .ctor(this, args)" shape
                // here would only add entries nothing can look up.
                if (method.IsConstructor && method.OwningType.IsWellKnownType(WellKnownType.String))
                    continue;

                // A generic has no single signature to generate a thunk from, so the interpreter
                // would find none at call time - and a release build does not even assert on the
                // miss, it takes the null cookie. CoreLib declares no such method today.
                if (method.HasInstantiation || method.OwningType.HasInstantiation)
                {
                    throw new LogAsErrorException(
                        $"Generic InternalCall method '{type}::{method.Name.ToString()}' has no single signature to generate a thunk from.");
                }

                try
                {
                    // A managed signature: the lowering adds the 'T' for an instance method and the
                    // trailing 'p' for the portable entry point parameter.
                    string signature = InteropSignature.GetMethodSignature(method);
                    if (_signatures.TryAdd(signature, method))
                        log.Verbose($"Adding InternalCall signature {signature} for method '{type}.{method.Name.ToString()}'");
                }
                catch (Exception ex) when (ex is not LogAsErrorException)
                {
                    // Every non-generic InternalCall has a signature the lowering can describe, so a
                    // failure here is a bug in the generator rather than something the assembly did.
                    // Skipping it would silently drop a thunk and only fail once the interpreter tries
                    // to call the method.
                    throw new LogAsErrorException($"Could not get the signature for InternalCall method '{type}::{method.Name.ToString()}': {ex.Message}");
                }
            }
        }
    }
}
