// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private readonly string _root;
    private readonly TaskLoggingHelper _log;

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
                if (string.IsNullOrEmpty(home) || !Path.IsPathRooted(home))
                {
                    home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (string.IsNullOrEmpty(home) || !Path.IsPathRooted(home))
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
        if (!Path.IsPathRooted(path) || path.IndexOfAny(new[] { '"', '\r', '\n', '*', '?' }) >= 0)
        {
            throw new InvalidDataException($"Unsupported response-file path: '{path}'.");
        }
        return Path.GetFullPath(path);
    }

    internal string ComputeKey(string tool, string jit, string input, ITaskItem[] references,
        ITaskItem[]? profiles, string[] outputs, string response, string command, string diagnosticPolicy, string workingDirectory)
    {
        tool = InputPath(tool);
        string toolDirectory = Path.GetDirectoryName(tool)!;
        if (Path.GetFileName(tool) != "crossgen2" || (File.GetAttributes(tool) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("The compiler must be a directly deployed crossgen2 executable, not a launcher or symbolic link.");
        }
        using (FileStream stream = File.OpenRead(tool))
        {
            if (stream.ReadByte() != 0x7f || stream.ReadByte() != 'E' || stream.ReadByte() != 'L' || stream.ReadByte() != 'F')
            {
                throw new InvalidDataException("Only a directly executed native ELF crossgen2 deployment is supported.");
            }
        }
        foreach (string extension in new[] { ".dll", ".deps.json", ".runtimeconfig.json", ".runtimeconfig.dev.json" })
        {
            if (Exists(Path.ChangeExtension(tool, extension)))
            {
                throw new InvalidDataException("Managed/apphost crossgen2 deployments are not supported.");
            }
        }

        using var data = new MemoryStream();
        using var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true);
        var inputs = new HashSet<string>(StringComparer.Ordinal);
        writer.Write("crossgen2-task-cache-v1");
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
            string rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _root : _root + Path.DirectorySeparatorChar;
            if (output == _root.TrimEnd(Path.DirectorySeparatorChar) || output.StartsWith(rootPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Outputs must be outside the cache.");
            }
        }
        AddFile(typeof(RunReadyToRunCompiler).Assembly.Location);
        AddFile(tool);
        AddFile(string.IsNullOrEmpty(jit) ? Path.Combine(toolDirectory, "libclrjit_unix_x64_x64.so") : jit);
        AddFile(Path.Combine(toolDirectory, "libjitinterface_x64.so"));
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
        return Hash(data);

        void AddFile(string path, bool optional = false)
        {
            path = InputPath(path);
            inputs.Add(path);
            writer.Write(path);
            try
            {
                using FileStream file = File.OpenRead(path);
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
                string pdb = pe.ReadCodeViewDebugDirectoryData(entry).Path;
                if (Path.IsPathRooted(pdb))
                {
                    AddFile(pdb, optional: true);
                }
                AddFile(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileName(pdb)), optional: true);
            }
        }
    }

    internal bool TryRestore(string key, string[] outputs, DateTime timestamp, out List<Diagnostic> diagnostics)
    {
        diagnostics = new List<Diagnostic>();
        string entry = Path.Combine(_root, "v1", key);
        if (!Directory.Exists(entry))
        {
            Report($"miss: {key}");
            return false;
        }
        try
        {
            string manifest = Path.Combine(entry, "manifest");
            using (FileStream file = File.OpenRead(manifest))
            {
                if (Hash(file) != File.ReadAllText(Path.Combine(entry, "manifest.sha256")))
                {
                    throw new InvalidDataException("Cached manifest checksum mismatch.");
                }
            }
            using var reader = new BinaryReader(File.OpenRead(manifest), Encoding.UTF8);
            if (reader.ReadInt32() != 1 || reader.ReadInt32() != outputs.Length)
            {
                throw new InvalidDataException("Invalid cache manifest.");
            }
            var present = new bool[outputs.Length];
            for (int i = 0; i < outputs.Length; i++)
            {
                present[i] = reader.ReadBoolean();
                if (present[i])
                {
                    string expected = reader.ReadString();
                    using FileStream file = File.OpenRead(Path.Combine(entry, i.ToString(CultureInfo.InvariantCulture)));
                    if (Hash(file) != expected)
                    {
                        throw new InvalidDataException("Cached output checksum mismatch.");
                    }
                }
            }
            if (!present[0])
            {
                throw new InvalidDataException("The native image is missing.");
            }
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
                diagnostics.Add(new Diagnostic(reader.ReadString(), importance));
            }
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                throw new InvalidDataException("Trailing cache manifest data.");
            }
            for (int i = 0; i < outputs.Length; i++)
            {
                if (present[i])
                {
                    File.Copy(Path.Combine(entry, i.ToString(CultureInfo.InvariantCulture)), outputs[i], overwrite: true);
                    File.SetLastWriteTimeUtc(outputs[i], timestamp);
                }
                else
                {
                    File.Delete(outputs[i]);
                }
            }
            Report($"hit: {key}");
            return true;
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            Report($"restore failed; running compiler: {ex.Message}");
            diagnostics.Clear();
            return false;
        }
    }

    internal void Store(string key, string[] outputs, List<Diagnostic> diagnostics)
    {
        string parent = Path.Combine(_root, "v1");
        string entry = Path.Combine(parent, key);
        string? staging = null;
        try
        {
            if (Directory.Exists(entry))
            {
                return;
            }
            Directory.CreateDirectory(parent);
            staging = Path.Combine(parent, key + "." + Guid.NewGuid().ToString("N") + ".tmp");
            Directory.CreateDirectory(staging);
            using (var writer = new BinaryWriter(File.Create(Path.Combine(staging, "manifest")), Encoding.UTF8))
            {
                writer.Write(1);
                writer.Write(outputs.Length);
                for (int i = 0; i < outputs.Length; i++)
                {
                    string copy = Path.Combine(staging, i.ToString(CultureInfo.InvariantCulture));
                    bool present;
                    try
                    {
                        File.Copy(outputs[i], copy);
                        present = true;
                    }
                    catch (FileNotFoundException) when (i != 0)
                    {
                        present = false;
                    }
                    writer.Write(present);
                    if (present)
                    {
                        using FileStream file = File.OpenRead(copy);
                        writer.Write(Hash(file));
                    }
                }
                writer.Write(diagnostics.Count);
                foreach (Diagnostic diagnostic in diagnostics)
                {
                    writer.Write((int)diagnostic.Importance);
                    writer.Write(diagnostic.Text);
                }
            }
            using (FileStream file = File.OpenRead(Path.Combine(staging, "manifest")))
            {
                File.WriteAllText(Path.Combine(staging, "manifest.sha256"), Hash(file));
            }
            // Complete immutable entries become visible in one cache-local rename.
            // A racing publisher may win; never overwrite its entry.
            Directory.Move(staging, entry);
            staging = null;
            Report($"stored: {key}");
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            Report($"store skipped: {ex.Message}");
        }
        finally
        {
            if (staging is not null)
            {
                try
                {
                    Directory.Delete(staging, recursive: true);
                }
                catch (Exception ex) when (IsCacheFailure(ex))
                {
                    Report($"staging cleanup failed: {ex.Message}");
                }
            }
        }
    }

    private static string Hash(Stream stream)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private static bool Exists(string path)
    {
        try
        {
            File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }
}
