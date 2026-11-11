// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.IO;
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
        using var sha256 = SHA256.Create();
        using var data = new MemoryStream();
        using var writer = new BinaryWriter(data, Encoding.UTF8, leaveOpen: true);
        writer.Write("invocation-cache-key-v1");
        writer.Write(invocation.WorkingDirectoryIdentity);
        writer.Write(RuntimeInformation.OSDescription);
        writer.Write(RuntimeInformation.ProcessArchitecture.ToString());
        writer.Write(CultureInfo.CurrentCulture.Name);
        writer.Write(CultureInfo.CurrentUICulture.Name);
        writer.Write(invocation.Context.Length);
        foreach (string value in invocation.Context)
            writer.Write(value);
        writer.Write(invocation.OutputDirectoryIdentity is not null);
        if (invocation.OutputDirectoryIdentity is { } outputDirectoryIdentity)
            writer.Write(outputDirectoryIdentity);
        writer.Write(invocation.Outputs.Files.Length);
        foreach (string identity in invocation.OutputFileIdentities)
            writer.Write(identity);
        writer.Write(invocation.Inputs.Files.Length);
        for (int i = 0; i < invocation.Inputs.Files.Length; i++)
        {
            writer.Write(invocation.InputIdentities[i]);
            try
            {
                using FileStream file = File.OpenRead(invocation.Inputs.Files[i]);
                writer.Write(true);
                writer.Write(sha256.ComputeHash(file));
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
    }
}
