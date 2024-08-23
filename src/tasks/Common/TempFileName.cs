// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

internal sealed class TempFileName : IDisposable
{
    public string Path { get; private set; }
    public TempFileName() => Path = System.IO.Path.GetTempFileName();
    public void Dispose() => File.Delete(Path);
}
