// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal volume helpers that are shared between many projects.</summary>
    internal static partial class DriveInfoInternal
    {
        internal static string[] GetLogicalDrives() => Interop.Sys.GetAllMountPoints();
    }
}
