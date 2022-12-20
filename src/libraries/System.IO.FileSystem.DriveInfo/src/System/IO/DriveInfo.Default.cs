// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public DriveType DriveType => DriveType.Unknown;
        public string DriveFormat => "memfs";
        public long AvailableFreeSpace => 0;
        public long TotalFreeSpace => 0;
        public long TotalSize => 0;

        private static string[] GetMountPoints() => Environment.GetLogicalDrives();
    }
}
