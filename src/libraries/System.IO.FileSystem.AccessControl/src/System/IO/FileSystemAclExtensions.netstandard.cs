// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;

namespace System.IO
{
    public static partial class FileSystemAclExtensions
    {
        public static void Create(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
