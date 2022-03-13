// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    /// <summary>Specifies whether the underlying handle is inheritable by child processes.</summary>
    public enum HandleInheritability
    {
        /// <summary>Specifies that the handle is not inheritable by child processes.</summary>
        None = 0,
        /// <summary>Specifies that the handle is inheritable by child processes.</summary>
        Inheritable = 1,
    }
}
