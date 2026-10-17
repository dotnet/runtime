// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

// Port together with RunReadyToRunCompiler to dotnet/sdk.
internal sealed class Crossgen2Cache
{
    private static readonly SearchValues<char> s_unsupportedPathCharacters = SearchValues.Create("\"\r\n*?");
    private readonly string _root;
    private readonly TaskLoggingHelper _log;
    private readonly InvocationCache _storage;

    internal static StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal readonly struct Diagnostic
    {
        internal string Text { get; }
        internal MessageImportance Importance { get; }

        internal Diagnostic(string text, MessageImportance importance)
        {
            Text = text;
            Importance = importance;
        }
    }

    private Crossgen2Cache(string root, TaskLoggingHelper log)
    {
        _root = root;
        _log = log;
        _storage = new InvocationCache(root, log, "Crossgen2", MessageImportance.Normal, versionDirectory: "v2");
    }

    internal static Crossgen2Cache? FromEnvironment(TaskLoggingHelper log)
    {
        string? setting = Environment.GetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE");
        if (string.IsNullOrEmpty(setting))
        {
            return null;
        }
        if (!bool.TryParse(setting, out bool enabled))
        {
            log.LogMessage(MessageImportance.Normal, "Crossgen2 cache bypass: CROSSGEN2_EXPERIMENTAL_CACHE must be true or false.");
            return null;
        }
        if (!enabled)
        {
            return null;
        }

        try
        {
            string? root = Environment.GetEnvironmentVariable("CROSSGEN2_EXPERIMENTAL_CACHE_PATH");
            if (string.IsNullOrEmpty(root))
            {
                string? home = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (string.IsNullOrEmpty(home) || !Path.IsPathFullyQualified(home))
                {
                    home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (string.IsNullOrEmpty(home) || !Path.IsPathFullyQualified(home))
                    {
                        throw new InvalidDataException("No absolute user cache location is available.");
                    }
                    home = Path.Combine(home, ".cache");
                }
                root = Path.Combine(home, "crossgen2");
            }
            root = Path.GetFullPath(root);
            if (File.Exists(root))
            {
                throw new InvalidDataException($"Cache location is a file: '{root}'.");
            }
            return new Crossgen2Cache(root, log);
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            log.LogMessage(MessageImportance.Normal, $"Crossgen2 cache bypass: {ex.Message}");
            return null;
        }
    }

    internal void Report(string message) => _log.LogMessage(MessageImportance.Normal, $"Crossgen2 cache {message}");

    internal static bool IsCacheFailure(Exception ex)
        => ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or BadImageFormatException or FormatException;

    internal static string InputPath(string path)
    {
        // These strings are embedded in a response file, not passed as an argv array.
        if (!Path.IsPathFullyQualified(path) || path.AsSpan().ContainsAny(s_unsupportedPathCharacters))
        {
            throw new InvalidDataException($"Unsupported response-file path: '{path}'.");
        }
        string fullPath = Path.GetFullPath(path);
        if (OperatingSystem.IsWindows())
        {
            string relative = path.Substring(Path.GetPathRoot(path)!.Length).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal) ||
                fullPath.StartsWith(@"\\.\", StringComparison.Ordinal) ||
                relative.Contains(':') ||
                relative.Split(Path.DirectorySeparatorChar).Any(part => part is not ("." or "..") && (part.EndsWith('.') || part.EndsWith(' '))))
            {
                throw new InvalidDataException($"Unsupported Windows path alias: '{path}'.");
            }
        }
        return fullPath;
    }

    internal static string ComputeKey(string[] compilerDependencies, string input, ITaskItem[] references,
        ITaskItem[]? profiles, string[] outputs, string response, string command, string diagnosticPolicy, string workingDirectory)
    {
        using var data = new MemoryStream();
        using var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true);
        var inputs = new HashSet<string>(PathComparer);
        writer.Write("crossgen2-task-cache-v3");
        writer.Write(Path.GetFullPath(workingDirectory));
        writer.Write(Directory.GetCurrentDirectory());
        writer.Write(RuntimeInformation.OSDescription);
        writer.Write(RuntimeInformation.ProcessArchitecture.ToString());
        writer.Write(CultureInfo.CurrentCulture.Name);
        writer.Write(CultureInfo.CurrentUICulture.Name);
        writer.Write(Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") ?? "");
        writer.Write(response ?? "");
        writer.Write(command ?? "");
        writer.Write(diagnosticPolicy);
        foreach (string output in outputs)
        {
            writer.Write(output);
        }
#pragma warning disable IL3000 // MSBuild tasks are loaded from assemblies on disk.
        string taskAssemblyPath = typeof(RunReadyToRunCompiler).Assembly.Location;
#pragma warning restore IL3000
        if (string.IsNullOrEmpty(taskAssemblyPath))
        {
            throw new InvalidDataException("The task assembly has no on-disk location.");
        }
        AddFile(taskAssemblyPath);
        writer.Write(compilerDependencies.Length);
        foreach (string dependency in compilerDependencies)
        {
            AddFile(dependency);
        }
        AddAssembly(input);
        writer.Write(references.Length);
        foreach (ITaskItem reference in references)
        {
            AddAssembly(reference.ItemSpec);
        }
        writer.Write(profiles?.Length ?? 0);
        if (profiles is not null)
        {
            foreach (ITaskItem profile in profiles)
            {
                AddFile(profile.ItemSpec);
            }
        }
        if (outputs.Any(inputs.Contains))
        {
            throw new InvalidDataException("An output aliases a compiler input.");
        }
        writer.Flush();
        data.Position = 0;
        return Hash(data).ToLowerInvariant();

        void AddFile(string path, bool optional = false, BlobContentId? symbolIdentity = null)
        {
            path = InputPath(path);
            inputs.Add(path);
            writer.Write(path);
            try
            {
                using FileStream file = File.OpenRead(path);
                if (symbolIdentity.HasValue)
                {
                    Crossgen2CacheInvocation.ValidateInputSymbols(file, symbolIdentity.Value);
                }
                writer.Write(true);
                writer.Write(Hash(file));
            }
            catch (FileNotFoundException) when (optional)
            {
                writer.Write(false);
            }
            catch (DirectoryNotFoundException) when (optional)
            {
                writer.Write(false);
            }
        }

        void AddAssembly(string path)
        {
            path = InputPath(path);
            AddFile(path);
            using FileStream stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                throw new InvalidDataException($"Compiler assembly input has no managed metadata: '{path}'.");
            }
            MetadataReader metadata = pe.GetMetadataReader();
            if (metadata.AssemblyFiles.Count != 0)
            {
                throw new InvalidDataException($"Multi-file assemblies are not supported: '{path}'.");
            }
            // The compiler tries rooted CodeView paths before candidates beside the PE.
            // Include absence too, so creating a previously missing PDB invalidates the key.
            DebugDirectoryEntry[] symbols = pe.ReadDebugDirectory().Where(e => e.Type == DebugDirectoryEntryType.CodeView).ToArray();
            writer.Write(symbols.Length);
            foreach (DebugDirectoryEntry entry in symbols)
            {
                CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(entry);
                string pdb = codeView.Path;
                var identity = new BlobContentId(codeView.Guid, entry.Stamp);
                if (Path.IsPathRooted(pdb))
                {
                    if (!Path.IsPathFullyQualified(pdb) && Path.GetPathRoot(pdb)!.Length != 1)
                    {
                        throw new InvalidDataException($"Drive-relative CodeView path is not supported: '{pdb}'.");
                    }
                    // Unix-style CodeView paths in Windows assemblies are rooted on the compiler's current drive.
                    AddFile(Path.GetFullPath(pdb, Path.GetFullPath(workingDirectory)), optional: true, symbolIdentity: identity);
                }
                AddFile(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileName(pdb)), optional: true, symbolIdentity: identity);
            }
        }
    }

    internal bool TryRestore(string key, InvocationCacheOutputs outputs, DateTime timestamp, out List<Diagnostic> diagnostics)
    {
        var restoredDiagnostics = new List<Diagnostic>();
        bool restored = _storage.TryRestore(key, outputs, timestamp, reader =>
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > reader.BaseStream.Length - reader.BaseStream.Position)
            {
                throw new InvalidDataException("Invalid diagnostic count.");
            }
            for (int i = 0; i < count; i++)
            {
                var importance = (MessageImportance)reader.ReadInt32();
                if (importance is not (MessageImportance.High or MessageImportance.Normal or MessageImportance.Low))
                {
                    throw new InvalidDataException("Invalid diagnostic importance.");
                }
                restoredDiagnostics.Add(new Diagnostic(reader.ReadString(), importance));
            }
        });
        if (!restored)
        {
            restoredDiagnostics.Clear();
        }
        diagnostics = restoredDiagnostics;
        return restored;
    }

    internal void Store(string key, InvocationCacheOutputs outputs, List<Diagnostic> diagnostics)
    {
        _storage.Store(key, outputs, writer =>
        {
            writer.Write(diagnostics.Count);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                writer.Write((int)diagnostic.Importance);
                writer.Write(diagnostic.Text);
            }
        });
    }

    internal void CleanOutputs(InvocationCacheOutputs outputs) => _storage.CleanOutputs(outputs);

    internal InvocationCacheOutputs DeclareOutputs(string[] paths)
    {
        var files = new InvocationCacheFile[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            files[i] = new InvocationCacheFile(paths[i], required: i == 0);
        }
        InvocationCacheOutputs outputs = InvocationCacheOutputs.FromFiles(files);
        outputs.Validate(_root);
        return outputs;
    }

    private static string Hash(Stream stream)
    {
        using SHA256 sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream));
    }
}
