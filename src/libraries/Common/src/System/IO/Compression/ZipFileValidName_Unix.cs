// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    public partial class ZipArchiveEntry
    {
        internal string ValidFullName
        {
            get
            {
                return FullName;
            }
        }
    }
}
