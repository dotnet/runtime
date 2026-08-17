// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// Reports a condition that should fail the build, with a message that is complete on its own.
    /// </summary>
    internal sealed class LogAsErrorException(string message) : Exception(message);

    /// <summary>
    /// Emits generator diagnostics in the canonical MSBuild format, so that a build driving
    /// crossgen2 through Exec still reports them with their codes.
    /// </summary>
    internal sealed class WasmInteropLogger(Logger logger)
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
    internal sealed class WasmInternalCallSignatureCollector(WasmInteropLogger log)
    {
        private readonly HashSet<string> _signatures = [];

        public IEnumerable<string> Signatures => _signatures;

        public void ScanType(EcmaType type)
        {
            foreach (MethodDesc method in type.GetMethods())
            {
                if (!method.IsInternalCall)
                    continue;

                // An uninstantiated generic has no single signature to generate a thunk from, because
                // its parameters stand for whatever the instantiation supplies.
                if (method.HasInstantiation || method.OwningType.HasInstantiation)
                {
                    log.Warning("WASM0001", $"Skipping generic InternalCall method '{type}::{method.Name.ToString()}', which has no single signature");
                    continue;
                }

                try
                {
                    // A managed signature: the lowering adds the 'T' for an instance method and the
                    // trailing 'p' for the portable entry point parameter.
                    string signature = WasmInteropSignature.GetMethodSignature(method, includeThis: true);
                    if (_signatures.Add(signature))
                        log.Verbose($"Adding InternalCall signature {signature} for method '{type}.{method.Name.ToString()}'");
                }
                catch (Exception ex) when (ex is not LogAsErrorException)
                {
                    log.Warning("WASM0001", $"Could not get signature for InternalCall method '{type}::{method.Name.ToString()}' because '{ex.Message}'");
                }
            }
        }
    }
}
