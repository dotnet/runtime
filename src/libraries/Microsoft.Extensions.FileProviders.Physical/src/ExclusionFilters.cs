// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// Specifies filtering behavior for files or directories.
    /// </summary>
    [Flags]
    public enum ExclusionFilters
    {
        /// <summary>
        /// Equivalent to <c>DotPrefixed | Hidden | System</c>. Exclude files and directories when the name begins with a period, or has either <see cref="FileAttributes.Hidden"/> or <see cref="FileAttributes.System"/> is set on <see cref="FileSystemInfo.Attributes"/>.
        /// </summary>
        Sensitive = DotPrefixed | Hidden | System,

        /// <summary>
        /// Exclude files and directories when the name begins with period.
        /// </summary>
        DotPrefixed = 0x0001,

        /// <summary>
        /// Exclude files and directories when <see cref="FileAttributes.Hidden"/> is set on <see cref="FileSystemInfo.Attributes"/>.
        /// </summary>
        Hidden = 0x0002,

        /// <summary>
        /// Exclude files and directories when <see cref="FileAttributes.System"/> is set on <see cref="FileSystemInfo.Attributes"/>.
        /// </summary>
        System = 0x0004,

        /// <summary>
        /// Do not exclude any files.
        /// </summary>
        None = 0
    }
}
