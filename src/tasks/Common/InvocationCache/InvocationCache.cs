// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

internal sealed partial class InvocationCache
{
    private const int FormatVersion = 1;
    private readonly TaskLoggingHelper _log;
    private readonly string _toolName;
    private static readonly StringComparison s_pathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    internal string CacheDirectory { get; }

    internal InvocationCache(string cacheDirectory, TaskLoggingHelper log, string toolName)
    {
        CacheDirectory = cacheDirectory;
        _log = log;
        _toolName = toolName;
    }

    internal bool TryRestore(string inputHash, string outputDirectory, DateTime outputTimestampUtc)
    {
        string entryDirectory = GetEntryDirectory(inputHash);
        outputDirectory = ValidateOutputDirectory(outputDirectory);
        if (outputTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The output timestamp must be UTC.", nameof(outputTimestampUtc));

        if (!Directory.Exists(entryDirectory))
        {
            _log.LogMessage(MessageImportance.Low, $"{_toolName} cache miss: {inputHash}");
            return false;
        }

        try
        {
            List<string> files = ReadManifest(entryDirectory);

            // Check the entire entry before modifying outputs. Copies can still fail if the cache is wiped concurrently.
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);

            Directory.CreateDirectory(outputDirectory);
            foreach (string relativePath in files)
            {
                string destination = Path.Combine(outputDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(entryDirectory, "outputs", relativePath), destination, overwrite: true);
                File.SetLastWriteTimeUtc(destination, outputTimestampUtc);
            }

            UpdateLastUsed(entryDirectory);
            _log.LogMessage(MessageImportance.Low, $"{_toolName} cache hit: {inputHash}");
            return true;
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(MessageImportance.Low, $"{_toolName} cache restore failed for '{inputHash}'; running {_toolName} normally: {ex.Message}");
            return false;
        }
    }

    internal void Store(string inputHash, string outputDirectory)
    {
        string entryDirectory = GetEntryDirectory(inputHash);
        outputDirectory = ValidateOutputDirectory(outputDirectory);
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
                _log.LogMessage(MessageImportance.Low, $"{_toolName} cache writer lock was abandoned: {inputHash}");
            }

            if (!acquired)
            {
                _log.LogMessage(MessageImportance.Low, $"{_toolName} cache store skipped; another writer owns '{inputHash}'.");
                return;
            }

            try
            {
                if (Directory.Exists(entryDirectory))
                {
                    _log.LogMessage(MessageImportance.Low, $"{_toolName} cache store skipped; entry '{inputHash}' already exists.");
                    return;
                }

                var files = new List<string>();
                CollectOutputFiles(outputDirectory, "", files);
                files.Sort(StringComparer.Ordinal);

                string parentDirectory = Path.GetDirectoryName(entryDirectory)!;
                Directory.CreateDirectory(parentDirectory);
                stagingDirectory = Path.Combine(parentDirectory, inputHash + "." + Guid.NewGuid().ToString("N") + ".tmp");
                Directory.CreateDirectory(stagingDirectory);

                using (var manifest = new BinaryWriter(File.Create(Path.Combine(stagingDirectory, "manifest"))))
                {
                    manifest.Write(FormatVersion);
                    manifest.Write(files.Count);
                    foreach (string relativePath in files)
                    {
                        string cachedFile = Path.Combine(stagingDirectory, "outputs", relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(cachedFile)!);
                        File.Copy(Path.Combine(outputDirectory, relativePath), cachedFile);
                        manifest.Write(relativePath);
                        manifest.Write(new FileInfo(cachedFile).Length);
                        manifest.Write(HashFile(cachedFile));
                    }
                }

                InvocationCacheEntry.WriteLastUsed(Path.Combine(stagingDirectory, InvocationCacheEntry.LastUsedFileName));
                Directory.Move(stagingDirectory, entryDirectory);
                stagingDirectory = null;
                _log.LogMessage(MessageImportance.Low, $"{_toolName} cache stored: {inputHash}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(MessageImportance.Low, $"{_toolName} cache store failed for '{inputHash}': {ex.Message}");
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
                    _log.LogMessage(MessageImportance.Low, $"{_toolName} cache staging cleanup failed for '{stagingDirectory}': {ex.Message}");
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
            _log.LogMessage(MessageImportance.Low, $"{_toolName} cache last-used update failed for '{entryDirectory}': {ex.Message}");
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

    private string ValidateOutputDirectory(string outputDirectory)
    {
        string outputPath = Path.GetFullPath(outputDirectory);
        string cachePrefix = CacheDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string outputPrefix = outputPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (cachePrefix.StartsWith(outputPrefix, s_pathComparison) || outputPrefix.StartsWith(cachePrefix, s_pathComparison))
            throw new ArgumentException("The cache and output directories must not contain each other.", nameof(outputDirectory));

        return outputPath;
    }

    private static void CollectOutputFiles(string directory, string relativeDirectory, List<string> files)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            string relativePath = Path.Combine(relativeDirectory, Path.GetFileName(path));
            if (Directory.Exists(path))
                CollectOutputFiles(path, relativePath, files);
            else
                files.Add(relativePath);
        }
    }

    private List<string> ReadManifest(string entryDirectory)
    {
        using var manifest = new BinaryReader(File.OpenRead(Path.Combine(entryDirectory, "manifest")));
        if (manifest.ReadInt32() != FormatVersion)
            throw new InvalidDataException($"Unsupported {_toolName} cache format.");

        int fileCount = manifest.ReadInt32();
        if (fileCount < 0 || fileCount > manifest.BaseStream.Length - manifest.BaseStream.Position)
            throw new InvalidDataException($"Invalid {_toolName} cache file count.");

        var files = new List<string>();
        var paths = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string outputsDirectory = Path.Combine(entryDirectory, "outputs");
        for (int i = 0; i < fileCount; i++)
        {
            string relativePath = manifest.ReadString();
            ValidateRelativePath(relativePath);
            if (!paths.Add(relativePath))
                throw new InvalidDataException($"Invalid {_toolName} cache output inventory.");

            long length = manifest.ReadInt64();
            byte[] expectedHash = manifest.ReadBytes(32);
            string cachedFile = Path.Combine(outputsDirectory, relativePath);

            if (expectedHash.Length != 32 || new FileInfo(cachedFile).Length != length ||
                !expectedHash.SequenceEqual(HashFile(cachedFile)))
                throw new InvalidDataException($"Invalid {_toolName} cache output: '{relativePath}'.");

            files.Add(relativePath);
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
