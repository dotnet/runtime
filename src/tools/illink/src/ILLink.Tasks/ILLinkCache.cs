// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace ILLink.Tasks;

internal sealed class ILLinkCache
{
    private const int FormatVersion = 1;
    private readonly TaskLoggingHelper _log;
    private static readonly StringComparison s_pathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    internal string CacheDirectory { get; }

    private ILLinkCache(string cacheDirectory, TaskLoggingHelper log)
    {
        CacheDirectory = cacheDirectory;
        _log = log;
    }

    internal static ILLinkCache? TryCreateFromEnvironment(TaskLoggingHelper log)
    {
        string? setting = Environment.GetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE");
        if (string.IsNullOrEmpty(setting))
            return null;

        if (!bool.TryParse(setting, out bool enabled))
        {
            log.LogMessage(MessageImportance.Low, "ILLink caching is disabled because ILLINK_EXPERIMENTAL_CACHE must be 'true' or 'false'.");
            return null;
        }

        return enabled ? TryCreate(Environment.GetEnvironmentVariable("ILLINK_EXPERIMENTAL_CACHE_PATH"), log) : null;
    }

    internal static ILLinkCache? TryCreate(string? cacheDirectory, TaskLoggingHelper log)
    {
        if (string.IsNullOrEmpty(cacheDirectory))
            cacheDirectory = GetDefaultCacheDirectory();

        if (cacheDirectory is null)
        {
            log.LogMessage(MessageImportance.Low, "ILLink caching is disabled because no default cache directory is available.");
            return null;
        }

        try
        {
            return new ILLinkCache(Path.GetFullPath(cacheDirectory), log);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            log.LogMessage(MessageImportance.Low, $"ILLink caching is disabled because the cache directory '{cacheDirectory}' could not be resolved: {ex.Message}");
            return null;
        }
    }

    private static string? GetDefaultCacheDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
            return string.IsNullOrEmpty(localAppData) || !Path.IsPathRooted(localAppData)
                ? null
                : Path.Combine(localAppData, "illink");
        }

        bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        if (!isMacOS)
        {
            string? cacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrEmpty(cacheHome) && Path.IsPathRooted(cacheHome))
                return Path.Combine(cacheHome, "illink");
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrEmpty(home) || !Path.IsPathRooted(home))
            return null;

        return isMacOS
            ? Path.Combine(home, "Library", "Caches", "illink")
            : Path.Combine(home, ".cache", "illink");
    }

    internal bool TryRestore(string inputHash, string outputDirectory, DateTime outputTimestampUtc)
    {
        string entryDirectory = GetEntryDirectory(inputHash);
        outputDirectory = ValidateOutputDirectory(outputDirectory);
        if (outputTimestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The output timestamp must be UTC.", nameof(outputTimestampUtc));

        if (!Directory.Exists(entryDirectory))
        {
            _log.LogMessage(MessageImportance.Low, $"ILLink cache miss: {inputHash}");
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
            _log.LogMessage(MessageImportance.Low, $"ILLink cache hit: {inputHash}");
            return true;
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(MessageImportance.Low, $"ILLink cache restore failed for '{inputHash}'; running ILLink normally: {ex.Message}");
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
                _log.LogMessage(MessageImportance.Low, $"ILLink cache writer lock was abandoned: {inputHash}");
            }

            if (!acquired)
            {
                _log.LogMessage(MessageImportance.Low, $"ILLink cache store skipped; another writer owns '{inputHash}'.");
                return;
            }

            try
            {
                if (Directory.Exists(entryDirectory))
                {
                    _log.LogMessage(MessageImportance.Low, $"ILLink cache store skipped; entry '{inputHash}' already exists.");
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

                ILLinkCacheEntry.WriteLastUsed(Path.Combine(stagingDirectory, ILLinkCacheEntry.LastUsedFileName));
                Directory.Move(stagingDirectory, entryDirectory);
                stagingDirectory = null;
                _log.LogMessage(MessageImportance.Low, $"ILLink cache stored: {inputHash}");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            _log.LogMessage(MessageImportance.Low, $"ILLink cache store failed for '{inputHash}': {ex.Message}");
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
                    _log.LogMessage(MessageImportance.Low, $"ILLink cache staging cleanup failed for '{stagingDirectory}': {ex.Message}");
                }
            }
        }
    }

    private void UpdateLastUsed(string entryDirectory)
    {
        string temporaryFile = Path.Combine(entryDirectory, ILLinkCacheEntry.LastUsedFileName + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            ILLinkCacheEntry.WriteLastUsed(temporaryFile);
            // Replace rather than overwrite so parallel restores cannot leave a partially written marker.
            File.Replace(temporaryFile, Path.Combine(entryDirectory, ILLinkCacheEntry.LastUsedFileName), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.LogMessage(MessageImportance.Low, $"ILLink cache last-used update failed for '{entryDirectory}': {ex.Message}");
        }
        finally
        {
            try
            {
                File.Delete(temporaryFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log.LogMessage(MessageImportance.Low, $"ILLink cache last-used cleanup failed for '{temporaryFile}': {ex.Message}");
            }
        }
    }

    private string GetEntryDirectory(string inputHash)
    {
        if (!ILLinkCacheEntry.IsEntryName(inputHash))
            throw new ArgumentException("The input hash must be a lowercase SHA-256 hex string.", nameof(inputHash));

        return Path.Combine(CacheDirectory, ILLinkCacheEntry.VersionDirectory, inputHash);
    }

    private string GetWriterMutexName(string inputHash)
    {
        string directory = CacheDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            directory = directory.ToUpperInvariant();

#if NET
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(directory + "\n" + inputHash));
        return @"Global\ILLinkCache-" + Convert.ToHexString(hash);
#else
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(directory + "\n" + inputHash));
        return @"Global\ILLinkCache-" + BitConverter.ToString(hash).Replace("-", "");
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

    private static List<string> ReadManifest(string entryDirectory)
    {
        using var manifest = new BinaryReader(File.OpenRead(Path.Combine(entryDirectory, "manifest")));
        if (manifest.ReadInt32() != FormatVersion)
            throw new InvalidDataException("Unsupported ILLink cache format.");

        int fileCount = manifest.ReadInt32();
        if (fileCount < 0 || fileCount > manifest.BaseStream.Length - manifest.BaseStream.Position)
            throw new InvalidDataException("Invalid ILLink cache file count.");

        var files = new List<string>();
        var paths = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        string outputsDirectory = Path.Combine(entryDirectory, "outputs");
        for (int i = 0; i < fileCount; i++)
        {
            string relativePath = manifest.ReadString();
            ValidateRelativePath(relativePath);
            if (!paths.Add(relativePath))
                throw new InvalidDataException("Invalid ILLink cache output inventory.");

            long length = manifest.ReadInt64();
            byte[] expectedHash = manifest.ReadBytes(32);
            string cachedFile = Path.Combine(outputsDirectory, relativePath);

            if (expectedHash.Length != 32 || new FileInfo(cachedFile).Length != length ||
                !expectedHash.SequenceEqual(HashFile(cachedFile)))
                throw new InvalidDataException($"Invalid ILLink cache output: '{relativePath}'.");

            files.Add(relativePath);
        }

        if (manifest.BaseStream.Position != manifest.BaseStream.Length)
            throw new InvalidDataException("Unexpected data in ILLink cache manifest.");

        return files;
    }

    private static void ValidateRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            throw new InvalidDataException("Invalid ILLink cache output path.");

        foreach (string component in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (component is "" or "." or ".." || component.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && component[component.Length - 1] is '.' or ' '))
                throw new InvalidDataException("Invalid ILLink cache output path.");
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
