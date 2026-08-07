// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

public class ManagedToNativeGenerator : Task
{
    [Required]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    public string[] IgnoredPInvokeModules { get; set; } = Array.Empty<string>();

    [Required, NotNull]
    public string? PInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? ReversePInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? InterpToNativeOutputPath { get; set; }
    public string? CacheFilePath { get; set; }

    public bool IsLibraryMode { get; set; }

    // When true (default), a P/Invoke to a module that isn't statically linked, ignored,
    // [WasmImportLinkage], "*" or QCall produces a WASM0066 warning. Consumers that scan
    // untrimmed closures full of cross-platform interop (e.g. library-test bundles) set this
    // false so the expected "unresolved module, skip and throw-if-called" case is logged as a
    // message instead of a build-breaking (under warn-as-error) warning.
    public bool WarnOnUnresolvedPInvokeModules { get; set; } = true;

    public string TargetOS { get; set; } = "browser";

    /// <summary>
    /// Path to crossgen2, which is run in its <c>--wasm-abi-query</c> mode to compute struct sizes
    /// and ABI lowering. Reflection alone cannot compute field layout, and the answers have to be the
    /// ones the compiler itself would produce.
    /// </summary>
    /// <remarks>
    /// Query mode does not load the JIT, so this does not have to be a wasm-targeting crossgen2.
    /// </remarks>
    public string? Crossgen2Path { get; set; }

    /// <summary>
    /// Path to the dotnet host, used when <see cref="Crossgen2Path"/> points at an IL-only build of
    /// crossgen2 rather than an apphost.
    /// </summary>
    public string? DotNetHostPath { get; set; }

    private static readonly string[] s_knownTargetOSes = new[] { "browser", "wasi" };

    private string ResolveCrossgen2Path()
    {
        if (string.IsNullOrEmpty(Crossgen2Path))
        {
            throw new LogAsErrorException(
                "The Crossgen2Path task parameter is required: computing the wasm ABI struct sizes for the " +
                "generated helpers needs crossgen2's type system.");
        }

        return Crossgen2Path!;
    }

    private string ResolveDotNetHostPath()
    {
        if (!string.IsNullOrEmpty(DotNetHostPath))
            return DotNetHostPath!;

        string? fromEnvironment = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(fromEnvironment))
            return fromEnvironment!;

        // When MSBuild itself is running on the .NET host, reuse it rather than trusting PATH to
        // turn up a compatible one.
        try
        {
            string? currentProcess = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(currentProcess))
            {
                string name = Path.GetFileNameWithoutExtension(currentProcess);
                if (string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
                    return currentProcess!;
            }
        }
        catch (Exception)
        {
        }

        return "dotnet";
    }

    [Output]
    public string[]? FileWrites { get; private set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        if (PInvokeModules!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(PInvokeModules)} cannot be empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetOS))
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(TargetOS)} cannot be empty; expected one of: {string.Join(", ", s_knownTargetOSes)}");
            return false;
        }

        TargetOS = TargetOS.Trim().ToLowerInvariant();
        if (Array.IndexOf(s_knownTargetOSes, TargetOS) < 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(TargetOS)} '{TargetOS}' is not recognized; expected one of: {string.Join(", ", s_knownTargetOSes)}");
            return false;
        }

        try
        {
            var logAdapter = new LogAdapter(Log);
            ExecuteInternal(logAdapter);
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException e)
        {
            Log.LogError(e.Message);
            return false;
        }
    }

    private void ExecuteInternal(LogAdapter log)
    {
        Dictionary<string, string> _symbolNameFixups = new();
        List<string> managedAssemblies = FilterOutUnmanagedBinaries(Assemblies);

        using var abiTypeResolver = new WasmAbiTypeResolver(ResolveDotNetHostPath(), ResolveCrossgen2Path(), TargetOS, managedAssemblies, log);
        var signatureMapper = new SignatureMapper(log, abiTypeResolver);
        var pinvoke = new PInvokeTableGenerator(FixupSymbolName, log, IsLibraryMode, TargetOS, signatureMapper, WarnOnUnresolvedPInvokeModules);
        var internalCallCollector = new InternalCallSignatureCollector(log, signatureMapper);

        var resolver = new PathAssemblyResolver(managedAssemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (string asmPath in managedAssemblies)
        {
            log.LogMessage(MessageImportance.Low, $"Loading {asmPath} to scan for pinvokes and InternalCall methods");
            Assembly asm = mlc.LoadFromAssemblyPath(asmPath);
            pinvoke.ScanAssembly(asm);

            if (asmPath.Contains("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
            {
                // Only scan System.Private.CoreLib, as all used InternalCall methods should be defined there,
                // and scanning all assemblies can be expensive, and can trigger failures which should be avoided.
                // System.Private.CoreLib is tested such that this should never fail on that binary.
                internalCallCollector.ScanAssembly(asm);
            }
        }

        // Pregenerated signatures for commonly used shapes used by R2R code to reduce duplication in generated R2R binaries.
        // The signatures should be in the form of a string where the first character represents the return type and the
        // following characters represent the argument types. The type characters should match those used by the
        // SignatureMapper.CharToNativeType method.
        string[] pregeneratedInterpreterToNativeSignatures = Array.Empty<string>(); // Currently none, but can be added here as needed in the future.

        IEnumerable<string> cookies = pinvoke.Generate(PInvokeModules, IgnoredPInvokeModules, PInvokeOutputPath, ReversePInvokeOutputPath);
        cookies = cookies.Concat(internalCallCollector.GetSignatures());
        cookies = cookies.Concat(pregeneratedInterpreterToNativeSignatures);

        var m2n = new InterpToNativeGenerator(log);
        m2n.Generate(cookies, InterpToNativeOutputPath);

        if (!string.IsNullOrEmpty(CacheFilePath))
        {
            IEnumerable<string> cacheLines = PInvokeModules
                .Select(module => $"module:{module}")
                .Concat(IgnoredPInvokeModules.Select(module => $"ignored:{module}"));
            File.WriteAllLines(CacheFilePath, cacheLines, Encoding.UTF8);
        }

        List<string> fileWritesList = new() { PInvokeOutputPath, InterpToNativeOutputPath };
        if (!string.IsNullOrEmpty(CacheFilePath))
            fileWritesList.Add(CacheFilePath);

        FileWrites = fileWritesList.ToArray();

        string FixupSymbolName(string name)
        {
            if (_symbolNameFixups.TryGetValue(name, out string? fixedName))
                return fixedName;

            fixedName = Utils.FixupSymbolName(name);
            _symbolNameFixups[name] = fixedName;
            return fixedName;
        }
    }

    private List<string> FilterOutUnmanagedBinaries(string[] assemblies)
    {
        List<string> managedAssemblies = new(assemblies.Length);
        foreach (string asmPath in Assemblies)
        {
            if (!File.Exists(asmPath))
                throw new LogAsErrorException($"Cannot find assembly {asmPath}");

            try
            {
                if (!Utils.IsManagedAssembly(asmPath))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping unmanaged {asmPath}.");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Failed to read assembly {asmPath}: {ex}");
                throw new LogAsErrorException($"Failed to read assembly {asmPath}: {ex.Message}");
            }

            managedAssemblies.Add(asmPath);
        }

        return managedAssemblies;
    }
}
