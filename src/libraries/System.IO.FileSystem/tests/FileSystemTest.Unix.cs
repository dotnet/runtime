// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemTest
    {
        [GeneratedDllImport("libc", SetLastError = true)]
        protected static partial int geteuid();

        [GeneratedDllImport("libc", CharSet = CharSet.Ansi, SetLastError = true)]
        protected static partial int mkfifo(string path, int mode);
    }
}
