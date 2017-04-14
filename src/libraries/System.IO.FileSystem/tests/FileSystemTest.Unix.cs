// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class FileSystemTest
    {
        [DllImport("libc", SetLastError = true)]
        protected static extern int geteuid();

        [DllImport("libc", SetLastError = true)]
        protected static extern int mkfifo(string path, int mode);
    }
}
