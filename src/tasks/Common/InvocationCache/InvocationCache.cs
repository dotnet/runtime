// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

internal readonly struct InvocationCacheRequest
{
    internal string Key { get; }
    internal InvocationOutputs Outputs { get; }

    internal InvocationCacheRequest(string key, InvocationOutputs outputs)
    {
        Key = key;
        Outputs = outputs;
    }
}

internal sealed partial class InvocationCache
{
    private readonly TaskLoggingHelper _log;
    private readonly string _toolName;
    private readonly MessageImportance _importance;

    internal string CacheDirectory { get; }

    internal InvocationCache(string cacheDirectory, TaskLoggingHelper log, string toolName,
        MessageImportance importance = MessageImportance.Low)
    {
        CacheDirectory = cacheDirectory;
        _log = log;
        _toolName = toolName;
        _importance = importance;
    }

    internal static int ExecuteCached(TaskLoggingHelper log, InvocationCacheConfiguration configuration,
        Func<InvocationDescription?> describeInvocation, Func<int> executeNormally)
        => TryExecuteCached(log, configuration, describeInvocation, executeNormally, out int exitCode)
            ? exitCode : executeNormally();

    private static bool TryExecuteCached(TaskLoggingHelper log, InvocationCacheConfiguration configuration,
        Func<InvocationDescription?> describeInvocation, Func<int> executeNormally, out int exitCode)
    {
        exitCode = 0;
        InvocationCache? cache = TryCreateFromEnvironment(log, configuration);
        if (cache is null)
            return false;

        InvocationDescription? invocation = describeInvocation();
        if (invocation is null)
            return false;

        cache.ValidateOutputs(invocation.Outputs);

        DateTime outputTimestampUtc = DateTime.UtcNow;
        if (!TryComputeKey(log, configuration.ToolName, invocation, out string key))
            return false;

        exitCode = ExecutePrepared(cache, outputTimestampUtc,
            new InvocationCacheRequest(key, invocation.Outputs), executeNormally);
        return true;
    }

    internal static int ExecutePrepared(InvocationCache? cache, DateTime outputTimestampUtc,
        InvocationCacheRequest? request,
        Func<int> execute)
    {
        if (cache is null || request is null)
            return execute();

        string key = request.Value.Key;
        InvocationOutputs outputs = request.Value.Outputs;
        if (cache.TryRestore(key, outputs, outputTimestampUtc))
            return 0;

        int exitCode = execute();
        if (exitCode == 0 && !cache._log.HasLoggedErrors)
            cache.Store(key, outputs);

        return exitCode;
    }

    internal bool TryRestore(string inputHash, InvocationOutputs outputs, DateTime outputTimestampUtc)
    {
        string entryDirectory = GetEntryDirectory(inputHash);
        ValidateOutputs(outputs);
        if (outputTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The output timestamp must be UTC.", nameof(outputTimestampUtc));

        if (!Directory.Exists(entryDirectory))
        {
            _log.LogMessage(_importance, $"{_toolName} cache miss: {inputHash}");
            return false;
        }

        try
        {
            List<(string Name, bool Present)> files = ReadManifest(entryDirectory, outputs);

            // Check the entire entry before modifying outputs. Copies can still fail if the cache is wiped concurrently.
            if (outputs.DirectoryPath is not null)
            {
                if (Directory.Exists(outputs.DirectoryPath))
                    Directory.Delete(outputs.DirectoryPath, recursive: true);
                Directory.CreateDirectory(outputs.DirectoryPath);
            }
            for (int i = 0; i < files.Count; i++)
            {
                string destination = GetOutputPath(outputs, files[i].Name, i);
                if (files[i].Present)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(Path.Combine(entryDirectory, "outputs", files[i].Name), destination, overwrite: true);
                    File.SetLastWriteTimeUtc(destination, outputTimestampUtc);
                }
                else
                {
                    DeleteOutputFile(destination);
                }
            }

            UpdateLastUsed(entryDirectory);
            _log.LogMessage(_importance, $"{_toolName} cache hit: {inputHash}");
            return true;
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(_importance, $"{_toolName} cache restore failed for '{inputHash}'; running {_toolName} normally: {ex.Message}");
            return false;
        }
    }

    internal void Store(string inputHash, InvocationOutputs outputs)
    {
        string entryDirectory = GetEntryDirectory(inputHash);
        ValidateOutputs(outputs);
        string? stagingDirectory = null;

        try
        {
            using var mutex = new Mutex(initiallyOwned: false, GetWriterMutexName(inputHash));
            bool acquired;
            try
            {
                acquired = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
                _log.LogMessage(_importance, $"{_toolName} cache writer lock was abandoned: {inputHash}");
            }

            if (!acquired)
            {
                _log.LogMessage(_importance, $"{_toolName} cache store skipped; another writer owns '{inputHash}'.");
                return;
            }

            try
            {
                if (Directory.Exists(entryDirectory))
                {
                    _log.LogMessage(_importance, $"{_toolName} cache store skipped; entry '{inputHash}' already exists.");
                    return;
                }

                List<string> files = GetFileNames(outputs);
                bool fileSet = outputs.DirectoryPath is null;

                string parentDirectory = Path.GetDirectoryName(entryDirectory)!;
                Directory.CreateDirectory(parentDirectory);
                stagingDirectory = Path.Combine(parentDirectory, inputHash + "." + Guid.NewGuid().ToString("N") + ".tmp");
                Directory.CreateDirectory(stagingDirectory);

                using (var manifest = new BinaryWriter(File.Create(Path.Combine(stagingDirectory, "manifest"))))
                {
                    manifest.Write(InvocationCacheEntry.FormatVersion);
                    manifest.Write(fileSet);
                    manifest.Write(files.Count);
                    for (int i = 0; i < files.Count; i++)
                    {
                        string relativePath = files[i];
                        string cachedFile = Path.Combine(stagingDirectory, "outputs", relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(cachedFile)!);
                        bool present = true;
                        try
                        {
                            File.Copy(GetOutputPath(outputs, relativePath, i), cachedFile);
                        }
                        catch (Exception ex) when (fileSet && ex is FileNotFoundException or DirectoryNotFoundException)
                        {
                            present = false;
                        }
                        manifest.Write(relativePath);
                        manifest.Write(present);
                        if (present)
                        {
                            manifest.Write(new FileInfo(cachedFile).Length);
                            manifest.Write(HashFile(cachedFile));
                        }
                    }
                }
                File.WriteAllBytes(Path.Combine(stagingDirectory, "manifest.sha256"), HashFile(Path.Combine(stagingDirectory, "manifest")));

                InvocationCacheEntry.WriteLastUsed(Path.Combine(stagingDirectory, InvocationCacheEntry.LastUsedFileName));
                Directory.Move(stagingDirectory, entryDirectory);
                stagingDirectory = null;
                _log.LogMessage(_importance, $"{_toolName} cache stored: {inputHash}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(_importance, $"{_toolName} cache store failed for '{inputHash}': {ex.Message}");
        }
        finally
        {
            if (stagingDirectory is not null)
            {
                try
                {
                    if (Directory.Exists(stagingDirectory))
                        Directory.Delete(stagingDirectory, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _log.LogMessage(_importance, $"{_toolName} cache staging cleanup failed for '{stagingDirectory}': {ex.Message}");
                }
            }
        }
    }

    private void UpdateLastUsed(string entryDirectory)
    {
        try
        {
            InvocationCacheEntry.WriteLastUsed(Path.Combine(entryDirectory, InvocationCacheEntry.LastUsedFileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.LogMessage(_importance, $"{_toolName} cache last-used update failed for '{entryDirectory}': {ex.Message}");
        }
    }

    private string GetEntryDirectory(string inputHash)
    {
        if (!InvocationCacheEntry.IsEntryName(inputHash))
            throw new ArgumentException("The input hash must be a lowercase SHA-256 hex string.", nameof(inputHash));

        return Path.Combine(CacheDirectory, InvocationCacheEntry.VersionDirectory, inputHash);
    }

    private string GetWriterMutexName(string inputHash)
    {
        string directory = CacheDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            directory = directory.ToUpperInvariant();

#if NET
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(directory + "\n" + inputHash));
        return @"Global\" + _toolName + "Cache-" + Convert.ToHexString(hash);
#else
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(directory + "\n" + inputHash));
        return @"Global\" + _toolName + "Cache-" + BitConverter.ToString(hash).Replace("-", "");
#endif
    }

    private List<(string Name, bool Present)> ReadManifest(string entryDirectory, InvocationOutputs outputs)
    {
        bool fileSet = outputs.DirectoryPath is null;
        if (!File.ReadAllBytes(Path.Combine(entryDirectory, "manifest.sha256")).SequenceEqual(HashFile(Path.Combine(entryDirectory, "manifest"))))
            throw new InvalidDataException("Cached manifest checksum mismatch.");

        using var manifest = new BinaryReader(File.OpenRead(Path.Combine(entryDirectory, "manifest")));
        if (manifest.ReadInt32() != InvocationCacheEntry.FormatVersion)
            throw new InvalidDataException($"Unsupported {_toolName} cache format.");
        if (manifest.ReadBoolean() != fileSet)
            throw new InvalidDataException($"Cached {_toolName} output contract does not match the invocation.");

        int fileCount = manifest.ReadInt32();
        if (fileCount < 0 || fileCount > manifest.BaseStream.Length - manifest.BaseStream.Position)
            throw new InvalidDataException($"Invalid {_toolName} cache file count.");
        if (fileSet && fileCount != outputs.Files.Length)
            throw new InvalidDataException($"Invalid {_toolName} cache output inventory.");

        var files = new List<(string Name, bool Present)>();
        var paths = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string outputsDirectory = Path.Combine(entryDirectory, "outputs");
        for (int i = 0; i < fileCount; i++)
        {
            string relativePath = manifest.ReadString();
            ValidateRelativePath(relativePath);
            if (!paths.Add(relativePath) || (fileSet && relativePath != i.ToString(CultureInfo.InvariantCulture)))
                throw new InvalidDataException($"Invalid {_toolName} cache output inventory.");

            bool present = manifest.ReadBoolean();
            if (!present && !fileSet)
                throw new InvalidDataException($"Directory inventory contains an absent {_toolName} cache file.");
            if (present)
            {
                long length = manifest.ReadInt64();
                byte[] expectedHash = manifest.ReadBytes(32);
                string cachedFile = Path.Combine(outputsDirectory, relativePath);
                if (expectedHash.Length != 32 || new FileInfo(cachedFile).Length != length ||
                    !expectedHash.SequenceEqual(HashFile(cachedFile)))
                    throw new InvalidDataException($"Invalid {_toolName} cache output: '{relativePath}'.");
            }
            files.Add((relativePath, present));
        }

        if (manifest.BaseStream.Position != manifest.BaseStream.Length)
            throw new InvalidDataException($"Unexpected data in {_toolName} cache manifest.");

        return files;
    }

    private void ValidateRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            throw new InvalidDataException($"Invalid {_toolName} cache output path.");

        foreach (string component in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (component is "" or "." or ".." || component.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && component[component.Length - 1] is '.' or ' '))
                throw new InvalidDataException($"Invalid {_toolName} cache output path.");
        }
    }

    private static byte[] HashFile(string path)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(path);
        return sha256.ComputeHash(stream);
    }

    private static bool IsCacheFailure(Exception ex) => ex is IOException or UnauthorizedAccessException or InvalidDataException or FormatException;
}
