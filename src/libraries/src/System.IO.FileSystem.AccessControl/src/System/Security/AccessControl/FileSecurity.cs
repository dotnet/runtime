// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    public sealed class FileSecurity : FileSystemSecurity
    {
        public FileSecurity()
            : base(false)
        {
        }

        public FileSecurity(string fileName, AccessControlSections includeSections)
            : base(false, fileName, includeSections, false)
        {
            // This will validate the passed path
            Path.GetFullPath(fileName);
        }

        internal FileSecurity(SafeFileHandle handle, AccessControlSections includeSections)
            : base(false, handle, includeSections, false)
        {
        }
    }
}
