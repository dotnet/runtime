// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Android.Build.Ndk
{
    public sealed class AndroidArch(string archName, string abi, string triple)
    {
        public string ArchName { get; set; } = archName;

        public string Abi { get; set; } = abi;

        public string Triple { get; set; } = triple;
    }
}
