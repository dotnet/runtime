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
/// Resolves wasm ABI encodings by delegating to the ILCompiler.Wasm.Lowering tool, which shares its
/// lowering and field layout code with crossgen2.
/// </summary>
/// <remarks>
/// The generated helpers have to agree with compiled code exactly - a struct whose size is off by one
/// produces a call that reads the wrong stack slots at runtime - so the sizes come from the compiler's
/// own type system rather than from reflection, which has no field layout engine.
///
/// The tool runs out of process because this task also runs under .NET Framework MSBuild, which cannot
/// load a netcoreapp type system assembly. It is started once and reused for every query.
/// </remarks>
internal sealed class WasmAbiTypeResolver : IWasmAbiTypeResolver, IDisposable
{
    private readonly string _dotnetHostPath;
    private readonly string _toolPath;
    private readonly string _targetOS;
    private readonly IReadOnlyList<string> _assemblies;
    private readonly LogAdapter _log;
    private readonly Dictionary<(string Assembly, int Token), string> _cache = new();

    private Process? _process;
    private string? _responseFilePath;
    private readonly StringBuilder _stderr = new();

    public WasmAbiTypeResolver(string dotnetHostPath, string toolPath, string targetOS, IReadOnlyList<string> assemblies, LogAdapter log)
    {
        _dotnetHostPath = dotnetHostPath;
        _toolPath = toolPath;
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
        if (_cache.TryGetValue(key, out string? cached))
            return cached;

        string reply = Query($"{assemblyName} 0x{metadataToken:x8}");
        if (reply[0] == '!')
        {
            throw new LogAsErrorException(
                $"Could not compute the wasm ABI encoding of '{type.FullName ?? type.Name}': {reply.Substring(1)}");
        }

        _cache[key] = reply;
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
                $"The wasm signature resolver ('{_toolPath}') exited unexpectedly while resolving '{request}'. {ReadStandardError(process)}");
        }

        return reply;
    }

    private Process EnsureStarted()
    {
        if (_process is not null)
            return _process;

        if (!File.Exists(_toolPath))
        {
            throw new LogAsErrorException(
                $"The wasm signature resolver was not found at '{_toolPath}'. Set the SignatureResolverPath task parameter to the path of ILCompiler.Wasm.Lowering.dll.");
        }

        // A response file keeps the command line under the platform limit; the framework alone is
        // ~170 assemblies and an app can add many more.
        _responseFilePath = Path.GetTempFileName();
        File.WriteAllLines(_responseFilePath, _assemblies, Encoding.UTF8);

        var startInfo = new ProcessStartInfo(_dotnetHostPath)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // ProcessStartInfo.ArgumentList is not available on .NET Framework, which this task also
            // targets, so the command line is quoted by hand.
            Arguments = $"exec {Quote(_toolPath)} --targetos {_targetOS} {Quote("@" + _responseFilePath)}",
        };

        _log.LogMessage(MessageImportance.Low, $"Starting wasm signature resolver: {_dotnetHostPath} {startInfo.Arguments}");

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new LogAsErrorException($"Failed to start the wasm signature resolver '{_toolPath}'.");
        }
        catch (Exception ex) when (ex is not LogAsErrorException)
        {
            throw new LogAsErrorException($"Failed to start the wasm signature resolver '{_toolPath}': {ex.Message}");
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
                $"The wasm signature resolver '{_toolPath}' failed to load the assembly closure. {ReadStandardError(process)}");
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
                _log.LogMessage(MessageImportance.Low, $"Failed to shut down the wasm signature resolver: {ex.Message}");
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
