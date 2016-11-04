// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Microsoft.Win32.SafeHandles.SafeFileHandle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.FileAccess))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.FileMode))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.FileShare))]

namespace System.IO
{
    [System.FlagsAttribute]
    public enum FileAttributes
    {
        Archive = 32,
        Compressed = 2048,
        Device = 64,
        Directory = 16,
        Encrypted = 16384,
        Hidden = 2,
        IntegrityStream = 32768,
        Normal = 128,
        NoScrubData = 131072,
        NotContentIndexed = 8192,
        Offline = 4096,
        ReadOnly = 1,
        ReparsePoint = 1024,
        SparseFile = 512,
        System = 4,
        Temporary = 256,
    }
}
