// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace SuperPmi;

// The JIT-EE version of src/coreclr/inc/jiteeversionguid.h (embedded as a resource, so the version is always the
// one of the JIT interface the tools are built with).
public static class JitEEVersion
{
    public static readonly Guid Identifier = Load();

    private static Guid Load()
    {
        using Stream stream = typeof(JitEEVersion).Assembly.GetManifestResourceStream("jiteeversionguid.h");
        using var reader = new StreamReader(stream);

        // The line that superpmi.py parses too: constexpr GUID JITEEVersionIdentifier = { /* <GUID> */
        const string Prefix = "constexpr GUID JITEEVersionIdentifier = { /* ";
        while (reader.ReadLine() is string line)
        {
            if (line.StartsWith(Prefix, StringComparison.Ordinal))
                return Guid.Parse(line.AsSpan(Prefix.Length, 36));
        }

        throw new InvalidDataException("JITEEVersionIdentifier is missing from jiteeversionguid.h");
    }
}
