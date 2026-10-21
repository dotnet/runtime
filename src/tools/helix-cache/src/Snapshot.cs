// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace HelixCache;

internal sealed class Snapshot
{
    private static readonly DateTime s_timestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal required string Path { get; init; }
    internal required string Digest { get; init; }
    internal required PayloadKind Kind { get; init; }

    internal static Snapshot Capture(string source, string cache)
    {
        source = System.IO.Path.GetFullPath(source);
        FileAttributes attributes = File.GetAttributes(source);
        RejectLink(source, attributes);
        bool directory = (attributes & FileAttributes.Directory) != 0;
        if (!directory && !source.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Payload '{source}' must be a directory or a ZIP archive.");
        }

        using TemporaryDirectory temporary = new(System.IO.Path.Combine(cache, "tmp"));
        EnsureOutside(source, temporary.Path);
        string name = directory ? "payload" : System.IO.Path.GetFileName(source);
        string staged = System.IO.Path.Combine(temporary.Path, name);
        Copy(source, staged);
        string digest = Hash(staged);
        string parent = System.IO.Path.Combine(cache, "inputs", "v1");
        Directory.CreateDirectory(parent);
        string final = System.IO.Path.Combine(parent, digest);
        try
        {
            Directory.Move(temporary.Path, final);
        }
        catch (IOException) when (Directory.Exists(final))
        {
            string existing = System.IO.Path.Combine(final, name);
            if (Hash(existing) != digest)
            {
                throw new InvalidDataException($"Snapshot failed validation: {existing}.");
            }
        }

        return new Snapshot { Path = System.IO.Path.Combine(final, name), Digest = digest, Kind = directory ? PayloadKind.Directory : PayloadKind.Archive };
    }

    internal static void EnsureOutside(string source, string destination)
    {
        if (Directory.Exists(source) &&
            (destination == source || destination.StartsWith(System.IO.Path.TrimEndingDirectorySeparator(source) + "/", StringComparison.Ordinal)))
        {
            throw new InvalidDataException($"Cache or staging directory '{destination}' must not be inside input directory '{source}'.");
        }
    }

    internal static void ValidateRelativePath(string path, bool allowEmpty = false)
    {
        if ((!allowEmpty && string.IsNullOrWhiteSpace(path)) || System.IO.Path.IsPathRooted(path) ||
            path.Split('/').Any(p => p is "." or "..") || path.Contains('\\'))
        {
            throw new InvalidDataException($"Expected a relative path without traversal: {path}.");
        }
    }

    internal static void Copy(string source, string destination)
    {
        FileAttributes attributes = File.GetAttributes(source);
        RejectLink(source, attributes);
        UnixFileMode mode = File.GetUnixFileMode(source);
        if ((attributes & FileAttributes.Directory) != 0)
        {
            Directory.CreateDirectory(destination);
            foreach (string child in Directory.EnumerateFileSystemEntries(source).Order(StringComparer.Ordinal))
            {
                Copy(child, System.IO.Path.Combine(destination, System.IO.Path.GetFileName(child)));
            }
            Directory.SetLastWriteTimeUtc(destination, s_timestamp);
        }
        else
        {
            using (FileStream input = File.OpenRead(source))
            using (FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write))
            {
                input.CopyTo(output);
            }
            File.SetLastWriteTimeUtc(destination, s_timestamp);
        }
        File.SetUnixFileMode(destination, mode);
    }

    internal static string Hash(string path)
    {
        using MemoryStream manifest = new();
        using (Utf8JsonWriter writer = new(manifest))
        {
            writer.WriteStartArray();
            WriteTree(writer, path, Directory.Exists(path) ? "" : System.IO.Path.GetFileName(path));
            writer.WriteEndArray();
        }
        return Convert.ToHexStringLower(SHA256.HashData(manifest.GetBuffer().AsSpan(0, checked((int)manifest.Length))));
    }

    private static void WriteTree(Utf8JsonWriter writer, string path, string relative)
    {
        FileAttributes attributes = File.GetAttributes(path);
        RejectLink(path, attributes);
        bool directory = (attributes & FileAttributes.Directory) != 0;
        writer.WriteStartArray();
        writer.WriteStringValue(relative);
        writer.WriteStringValue(directory ? "directory" : "file");
        writer.WriteNumberValue((int)File.GetUnixFileMode(path));
        if (!directory)
        {
            using FileStream file = File.OpenRead(path);
            writer.WriteStringValue(Convert.ToHexStringLower(SHA256.HashData(file)));
        }
        writer.WriteEndArray();
        if (directory)
        {
            foreach (string child in Directory.EnumerateFileSystemEntries(path).Order(StringComparer.Ordinal))
            {
                string name = System.IO.Path.GetFileName(child);
                WriteTree(writer, child, relative.Length == 0 ? name : relative + "/" + name);
            }
        }
    }

    private static void RejectLink(string path, FileAttributes attributes)
    {
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Symbolic links are not supported in directory payloads or results: {path}. Use an archive for payloads containing links.");
        }
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    internal string Path { get; }

    internal TemporaryDirectory(string parent)
    {
        Path = System.IO.Path.Combine(parent, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            // Copied inputs can contain read-only directories.
            foreach (string directory in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories).Prepend(Path))
            {
                File.SetUnixFileMode(directory, File.GetUnixFileMode(directory) | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            Directory.Delete(Path, recursive: true);
        }
    }
}
