// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// An empty file provider with no contents.
    /// </summary>
    public class NullFileProvider : IFileProvider
    {
        /// <summary>
        /// Enumerate a non-existent directory.
        /// </summary>
        /// <param name="subpath">A path under the root directory. This parameter is ignored.</param>
        /// <returns>A <see cref="IDirectoryContents"/> that does not exist and does not contain any contents.</returns>
        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

        /// <summary>
        /// Locate a non-existent file.
        /// </summary>
        /// <param name="subpath">A path under the root directory.</param>
        /// <returns>A <see cref="IFileInfo"/> representing a non-existent file at the given path.</returns>
        public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that monitors nothing.
        /// </summary>
        /// <param name="filter">Filter string used to determine what files or folders to monitor. This parameter is ignored.</param>
        /// <returns>A <see cref="IChangeToken"/> that does not register callbacks.</returns>
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
