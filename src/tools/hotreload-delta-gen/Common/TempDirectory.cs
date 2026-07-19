// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.DotNet.HotReload.Utils.Common;

internal class TempDirectory : IDisposable
{
    public TempDirectory(bool keep = false, string? dirname = null)
    {
        string subdir = dirname ?? System.IO.Path.GetRandomFileName();
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), subdir);
        Keep = keep;
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }
    public bool Keep { get; set; }

    public void Dispose()
    {
        if (!Keep)
            Directory.Delete(Path, true);
        GC.SuppressFinalize(this);
    }
}
