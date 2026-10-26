// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

// Port together with RunReadyToRunCompiler to dotnet/sdk.
internal static class Crossgen2Cache
{
    private static readonly SearchValues<char> s_unsupportedPathCharacters = SearchValues.Create("\"\r\n*?");

    internal static StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal static void Report(TaskLoggingHelper log, string message)
        => log.LogMessage(MessageImportance.Normal, $"Crossgen2 cache {message}");

    internal static bool IsCacheFailure(Exception ex)
        => ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or BadImageFormatException or FormatException;

    internal static string InputPath(string path)
    {
        // These strings are embedded in a response file, not passed as an argv array.
        if (!Path.IsPathFullyQualified(path) || path.AsSpan().ContainsAny(s_unsupportedPathCharacters) ||
            char.IsWhiteSpace(path[^1]))
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

    internal static InvocationDescription DescribeInvocation(string[] compilerDependencies, string input, ITaskItem[] references,
        ITaskItem[]? profiles, string[] outputs, string response, string command, string diagnosticPolicy, string workingDirectory)
    {
        var inputs = new HashSet<string>(PathComparer);
        var files = new List<string>();
#pragma warning disable IL3000 // MSBuild tasks are loaded from assemblies on disk.
        string taskAssemblyPath = typeof(RunReadyToRunCompiler).Assembly.Location;
#pragma warning restore IL3000
        if (string.IsNullOrEmpty(taskAssemblyPath))
        {
            throw new InvalidDataException("The task assembly has no on-disk location.");
        }
        AddFile(taskAssemblyPath);
        foreach (string dependency in compilerDependencies)
        {
            AddFile(dependency);
        }
        AddAssembly(input);
        foreach (ITaskItem reference in references)
        {
            AddAssembly(reference.ItemSpec);
        }
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
        return new InvocationDescription(InvocationInputs.FromFiles(files.ToArray()), new[]
        {
            Path.GetFullPath(workingDirectory),
            Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT") ?? "",
            response ?? "",
            command ?? "",
            diagnosticPolicy
        }, InvocationOutputs.FromFiles(outputs));

        void AddFile(string path)
        {
            path = InputPath(path);
            inputs.Add(path);
            files.Add(path);
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
                    AddSymbols(Path.GetFullPath(pdb, Path.GetFullPath(workingDirectory)), identity);
                }
                AddSymbols(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileName(pdb)), identity);
            }
        }

        void AddSymbols(string path, BlobContentId identity)
        {
            AddFile(path);
            try
            {
                using FileStream file = File.OpenRead(path);
                Crossgen2CacheInvocation.ValidateInputSymbols(file, identity);
            }
            catch (FileNotFoundException)
            {
                // Missing symbol candidates are declared so their absence participates in the key.
            }
            catch (DirectoryNotFoundException)
            {
                // Missing symbol candidates are declared so their absence participates in the key.
            }
        }
    }
}
