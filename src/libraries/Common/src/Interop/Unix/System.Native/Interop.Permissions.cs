// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum Permissions
        {
            Mask = S_IRWXU | S_IRWXG | S_IRWXO,

            S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,
            S_IRUSR = 0x100,
            S_IWUSR = 0x80,
            S_IXUSR = 0x40,

            S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,
            S_IRGRP = 0x20,
            S_IWGRP = 0x10,
            S_IXGRP = 0x8,

            S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,
            S_IROTH = 0x4,
            S_IWOTH = 0x2,
            S_IXOTH = 0x1,

            S_IXUGO = S_IXUSR | S_IXGRP | S_IXOTH,
        }
    }
}
