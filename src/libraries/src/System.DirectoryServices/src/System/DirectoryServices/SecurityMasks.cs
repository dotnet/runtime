// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Specifies the available options for examining security information of an object.
    /// </devdoc>
    [Flags]
    public enum SecurityMasks
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Dacl = 4,
        Sacl = 8
    }
}
