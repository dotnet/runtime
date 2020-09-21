// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Security.AccessControl
{
    public sealed class DirectorySecurity : FileSystemSecurity
    {
        public DirectorySecurity()
            : base(true)
        {
        }

        public DirectorySecurity(string name, AccessControlSections includeSections)
            : base(true, name, includeSections, true)
        {
        }
    }
}
