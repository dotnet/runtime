// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Path.GetFullPath(name);
        }
    }
}
