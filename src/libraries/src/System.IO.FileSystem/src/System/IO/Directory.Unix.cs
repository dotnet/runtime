// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public static partial class Directory
    {
        public static string[] GetLogicalDrives()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
