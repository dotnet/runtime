// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a directory's content in the file provider.
    /// </summary>
    public interface IDirectoryContents : IEnumerable<IFileInfo>
    {
        /// <summary>
        /// True if a directory was located at the given path.
        /// </summary>
        bool Exists { get; }
    }
}
