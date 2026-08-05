// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

/// <summary>
/// Resolves wasm ABI encodings by asking crossgen2, running in its <c>--wasm-abi-query</c> mode.
/// </summary>
/// <remarks>
/// The generated helpers have to agree with compiled code exactly - a struct whose size is off by one
/// produces a call that reads the wrong stack slots at runtime - so the sizes come from the compiler's
/// own type system rather than from reflection, which has no field layout engine.
///
/// crossgen2 answers rather than a purpose-built tool so that there is exactly one implementation of
/// the wasm lowering rules and one type system configuration. Query mode never loads the JIT, so the
/// crossgen2 used here does not have to be the wasm-targeting one; the target is selected by the
/// <c>--targetos</c> and <c>--targetarch</c> arguments.
///
/// It runs out of process because this task also runs under .NET Framework MSBuild, which cannot load
/// a netcoreapp type system assembly. Loading the assembly closure is the expensive part, so the
/// process is started once and reused for every query.
/// </remarks>
internal sealed class WasmAbiTypeResolver : IWasmAbiTypeResolver, IDisposable
{
    private readonly string _dotnetHostPath;
    private readonly string _crossgen2Path;
    private readonly string _targetOS;
    private readonly IReadOnlyList<string> _assemblies;
    private readonly LogAdapter _log;
    private readonly Dictionary<(string Assembly, int Token), string> _typeCache = new();
    private readonly Dictionary<(string Assembly, int Token, WasmLoweringFlags Flags), string> _methodCache = new();

    private Process? _process;
    private string? _responseFilePath;
    private readonly StringBuilder _stderr = new();

    public WasmAbiTypeResolver(string dotnetHostPath, string crossgen2Path, string targetOS, IReadOnlyList<string> assemblies, LogAdapter log)
    {
        _dotnetHostPath = dotnetHostPath;
        _crossgen2Path = crossgen2Path;
        _targetOS = targetOS;
        _assemblies = assemblies;
        _log = log;
    }

    public string GetAbiToken(Type type)
    {
        if (type.IsConstructedGenericType || type.IsGenericParameter || type.ContainsGenericParameters)
        {
            throw new LogAsErrorException(
                $"Cannot compute the wasm ABI encoding of generic type '{type.FullName ?? type.Name}'. " +
                "Generic types are not addressable by metadata token, so the size of an instantiation cannot be resolved.");
        }

        string assemblyName = type.Module.Assembly.GetName().Name
            ?? throw new LogAsErrorException($"Type '{type.FullName ?? type.Name}' comes from an assembly with no simple name.");
        int metadataToken = type.MetadataToken;

        var key = (assemblyName, metadataToken);
        if (_typeCache.TryGetValue(key, out string? cached))
            return cached;

        string reply = Query($"t {assemblyName} 0x{metadataToken:x8}");
        if (reply[0] == '!')
        {
            throw new LogAsErrorException(
                $"Could not compute the wasm ABI encoding of '{type.FullName ?? type.Name}': {reply.Substring(1)}");
        }

        _typeCache[key] = reply;
        return reply;
    }

    public string GetMethodSignature(MethodInfo method, WasmLoweringFlags flags)
    {
        string assemblyName = method.Module.Assembly.GetName().Name
            ?? throw new LogAsErrorException($"Method '{method.Name}' comes from an assembly with no simple name.");
        int metadataToken = method.MetadataToken;

        var key = (assemblyName, metadataToken, flags);
        if (_methodCache.TryGetValue(key, out string? cached))
            return cached;

        string reply = Query($"m {assemblyName} 0x{metadataToken:x8} {(int)flags}");
        if (reply[0] == '!')
        {
            throw new LogAsErrorException(
                $"Could not compute the wasm signature of '{method.DeclaringType?.FullName}::{method.Name}': {reply.Substring(1)}");
        }

        _methodCache[key] = reply;
        return reply;
    }

    private string Query(string request)
    {
        Process process = EnsureStarted();
        process.StandardInput.WriteLine(request);
        process.StandardInput.Flush();

        string? reply = process.StandardOutput.ReadLine();
        if (reply is null)
        {
            throw new LogAsErrorException(
                $"crossgen2 ('{_crossgen2Path}') exited unexpectedly while resolving '{request}'. {ReadStandardError(process)}");
        }

        return reply;
    }

    private Process EnsureStarted()
    {
        if (_process is not null)
            return _process;

        if (!File.Exists(_crossgen2Path))
        {
            throw new LogAsErrorException(
                $"crossgen2 was not found at '{_crossgen2Path}'. It computes the wasm ABI struct sizes the generated " +
                "helpers need. Set the Crossgen2Path task parameter to its location.");
        }

        // A response file keeps the command line under the platform limit; the framework alone is
        // ~170 assemblies and an app can add many more.
        _responseFilePath = Path.GetTempFileName();
        File.WriteAllLines(_responseFilePath, _assemblies, Encoding.UTF8);

        // The assemblies are passed as crossgen2's positional inputs rather than as references so
        // that its "no input files" check is satisfied; query mode writes no image, so nothing is
        // compiled for them.
        string arguments = $"--wasm-abi-query --targetos {_targetOS} --targetarch wasm {Quote("@" + _responseFilePath)}";

        // crossgen2 normally ships as an apphost, but an IL-only build is run through the muxer.
        string executable = _crossgen2Path;
        if (_crossgen2Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            executable = _dotnetHostPath;
            arguments = $"exec {Quote(_crossgen2Path)} {arguments}";
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // ProcessStartInfo.ArgumentList is not available on .NET Framework, which this task also
            // targets, so the command line is quoted by hand.
            Arguments = arguments,
        };

        _log.LogMessage(MessageImportance.Low, $"Starting wasm ABI query: {executable} {arguments}");

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new LogAsErrorException($"Failed to start crossgen2 '{_crossgen2Path}'.");
        }
        catch (Exception ex) when (ex is not LogAsErrorException)
        {
            throw new LogAsErrorException($"Failed to start crossgen2 '{_crossgen2Path}': {ex.Message}");
        }

        // Take ownership before the handshake so a failure below still goes through Dispose. An
        // orphaned tool would hold open file handles on every assembly in the closure, which on
        // Windows blocks a subsequent build from overwriting them.
        _process = process;

        // stderr has to be drained continuously: the tool would otherwise block once the pipe
        // buffer filled, while this side blocks reading stdout.
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (_stderr)
                {
                    _stderr.AppendLine(e.Data);
                }
            }
        };
        process.BeginErrorReadLine();

        string? ready = process.StandardOutput.ReadLine();
        if (ready != "ready")
        {
            throw new LogAsErrorException(
                $"crossgen2 '{_crossgen2Path}' failed to load the assembly closure. {ReadStandardError(process)}");
        }

        return process;
    }

    private static readonly char[] s_charsNeedingQuotes = new[] { ' ', '"', '\t' };

    private static string Quote(string argument)
    {
        if (argument.Length > 0 && argument.IndexOfAny(s_charsNeedingQuotes) < 0)
            return argument;

        return "\"" + argument.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private string ReadStandardError(Process process)
    {
        // Give the asynchronous reader a moment to flush what the tool wrote before it died,
        // but never block the build waiting on a process that is still alive.
        try
        {
            process.WaitForExit(2000);
        }
        catch (Exception)
        {
        }

        lock (_stderr)
        {
            return _stderr.ToString().Trim();
        }
    }

    public void Dispose()
    {
        if (_process is not null)
        {
            try
            {
                // Closing stdin ends the tool's read loop, letting it exit on its own.
                _process.StandardInput.Close();
                if (!_process.WaitForExit(5000))
                    _process.Kill();
            }
            catch (Exception ex)
            {
                _log.LogMessage(MessageImportance.Low, $"Failed to shut down the wasm ABI query process: {ex.Message}");
            }

            _process.Dispose();
            _process = null;
        }

        if (_responseFilePath is not null)
        {
            try
            {
                File.Delete(_responseFilePath);
            }
            catch (IOException)
            {
            }

            _responseFilePath = null;
        }
    }
}
