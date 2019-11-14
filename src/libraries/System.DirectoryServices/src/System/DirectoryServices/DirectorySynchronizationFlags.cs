// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    [Flags]
    public enum DirectorySynchronizationOptions : long
    {
        None = 0,
        ObjectSecurity = 0x1,
        ParentsFirst = 0x0800,
        PublicDataOnly = 0x2000,
        IncrementalValues = 0x80000000
    }
}
