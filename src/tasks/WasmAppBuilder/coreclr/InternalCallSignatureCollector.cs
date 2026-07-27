// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

//
// Scans assemblies for methods marked with MethodImplAttributes.InternalCall
// and generates portable entry point signatures for the interpreter-to-native thunks.
//
internal sealed class InternalCallSignatureCollector
{
    private readonly HashSet<string> _signatures = new();
    private readonly LogAdapter _log;

    public InternalCallSignatureCollector(LogAdapter log) => _log = log;

    public void ScanAssembly(Assembly asm)
    {
        foreach (Type type in asm.GetTypes())
            ScanType(type);
    }

    public IEnumerable<string> GetSignatures() => _signatures;

    private void ScanType(Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if ((method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0)
                continue;

            try
            {
                string? signature = SignatureMapper.MethodToSignature(method, _log, includeThis: true);
                if (signature is null)
                {
                    _log.Warning("WASM0001", $"Could not generate signature for InternalCall method '{type.FullName}::{method.Name}'");
                    continue;
                }

                signature += "p";

                if (_signatures.Add(signature))
                    _log.LogMessage(MessageImportance.Low, $"Adding InternalCall signature {signature} for method '{type.FullName}.{method.Name}'");
            }
            catch (Exception ex) when (ex is not LogAsErrorException)
            {
                _log.Warning("WASM0001", $"Could not get signature for InternalCall method '{type.FullName}::{method.Name}' because '{ex.Message}'");
            }
        }
    }
}
