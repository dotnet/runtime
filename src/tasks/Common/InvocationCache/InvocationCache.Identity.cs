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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

internal sealed partial class InvocationCache
{
    internal static bool TryComputeKey(TaskLoggingHelper log, string toolName, InvocationDescription invocation, out string key)
    {
        key = string.Empty;
        try
        {
            key = ComputeKey(invocation);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            log.LogMessage(MessageImportance.Low, $"{toolName} cache bypassed: input identity could not be computed: {ex.Message}");
            return false;
        }
    }

    internal static string ComputeKey(InvocationDescription invocation)
    {
        var identities = new Dictionary<string, string>(StringComparer.Ordinal);
        using var sha256 = SHA256.Create();
        using var data = new MemoryStream();
        using var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true);
        writer.Write("invocation-cache-key-v1");
        writer.Write(MapPath(invocation.WorkingDirectory));
        writer.Write(RuntimeInformation.OSDescription);
        writer.Write(RuntimeInformation.ProcessArchitecture.ToString());
        writer.Write(CultureInfo.CurrentCulture.Name);
        writer.Write(CultureInfo.CurrentUICulture.Name);
        string[] context = invocation.GetContext(MapPath);
        writer.Write(context.Length);
        foreach (string value in context)
            writer.Write(value);
        writer.Write(invocation.Outputs.DirectoryPath is not null);
        if (invocation.Outputs.DirectoryPath is not null)
            writer.Write(MapPath(invocation.Outputs.DirectoryPath));
        writer.Write(invocation.Outputs.Files.Length);
        foreach (string output in invocation.Outputs.Files)
            writer.Write(MapPath(output));
        writer.Write(invocation.Inputs.Files.Length);
        foreach (var input in invocation.Inputs.Files.Select(path => (Path: path, Identity: MapPath(path)))
            .OrderBy(input => input.Identity, StringComparer.Ordinal))
        {
            writer.Write(input.Identity);
            try
            {
                using FileStream stream = File.OpenRead(input.Path);
                writer.Write(true);
                writer.Write(sha256.ComputeHash(stream));
            }
            catch (FileNotFoundException)
            {
                writer.Write(false);
            }
            catch (DirectoryNotFoundException)
            {
                writer.Write(false);
            }
        }
        writer.Flush();
        data.Position = 0;
#if NET
        return Convert.ToHexStringLower(sha256.ComputeHash(data));
#else
        return BitConverter.ToString(sha256.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
#endif

        string MapPath(string path)
        {
            if (!identities.TryGetValue(path, out string? identity))
            {
                identity = invocation.MapPath(path);
                identities.Add(path, identity);
            }
            return identity;
        }
    }
}
