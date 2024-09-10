// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// A read-only file provider abstraction.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Locates a file at the given path.
        /// </summary>
        /// <param name="subpath">The relative path that identifies the file.</param>
        /// <returns>The file information. Caller must check Exists property.</returns>
        IFileInfo GetFileInfo(string subpath);

        /// <summary>
        /// Enumerates a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">The relative path that identifies the directory.</param>
        /// <returns>Returns the contents of the directory.</returns>
        IDirectoryContents GetDirectoryContents(string subpath);

        /// <summary>
        /// Creates an <see cref="IChangeToken"/> for the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">A filter string used to determine what files or folders to monitor. Examples: **/*.cs, *.*, subFolder/**/*.cshtml.</param>
        /// <returns>An <see cref="IChangeToken"/> that is notified when a file matching <paramref name="filter"/> is added, modified, or deleted.</returns>
        IChangeToken Watch(string filter);
    }
}
