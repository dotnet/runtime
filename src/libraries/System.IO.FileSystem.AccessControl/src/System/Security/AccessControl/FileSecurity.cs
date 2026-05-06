// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        }

        internal FileSecurity(SafeFileHandle? handle, AccessControlSections includeSections)
            : base(false, handle, includeSections, false)
        {
        }
    }
}
