// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileProviders.Composite;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Looks up files using a collection of <see cref="IFileProvider"/>.
    /// </summary>
    public class CompositeFileProvider : IFileProvider
    {
        private readonly IFileProvider[] _fileProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeFileProvider" /> class using a collection of file provider objects.
        /// </summary>
        /// <param name="fileProviders">The collection of <see cref="IFileProvider" /> objects.</param>
        public CompositeFileProvider(params IFileProvider[]? fileProviders)
        {
            _fileProviders = fileProviders ?? Array.Empty<IFileProvider>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeFileProvider" /> class using a collection of file provider objects.
        /// </summary>
        /// <param name="fileProviders">The collection of <see cref="IFileProvider" /> objects.</param>
        public CompositeFileProvider(IEnumerable<IFileProvider> fileProviders)
        {
            ArgumentNullException.ThrowIfNull(fileProviders);

            _fileProviders = fileProviders.ToArray();
        }

        /// <summary>
        /// Locates a file at the given path.
        /// </summary>
        /// <param name="subpath">The path that identifies the file.</param>
        /// <returns>The file information. The caller must check the <see cref="IFileInfo.Exists" /> property. This is the first existing <see cref="IFileInfo"/> returned by the provided <see cref="IFileProvider"/> or a not found <see cref="IFileInfo"/> if no existing files are found.</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            foreach (IFileProvider fileProvider in _fileProviders)
            {
                IFileInfo fileInfo = fileProvider.GetFileInfo(subpath);
                if (fileInfo != null && fileInfo.Exists)
                {
                    return fileInfo;
                }
            }
            return new NotFoundFileInfo(subpath);
        }

        /// <summary>
        /// Enumerates a directory at the given path, if any.
        /// </summary>
        /// <param name="subpath">The path that identifies the directory.</param>
        /// <returns>The contents of the directory. Caller must check Exists property.
        /// The content is a merge of the contents of the provided <see cref="IFileProvider"/>.
        /// When there are multiple <see cref="IFileInfo"/> objects with the same Name property, only the first one is included in the results.</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var directoryContents = new CompositeDirectoryContents(_fileProviders, subpath);
            return directoryContents;
        }

        /// <summary>
        /// Creates a <see cref="IChangeToken"/> for the specified <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">A filter string used to determine what files or folders to monitor. Examples: \*\*/\*.cs, \*.\*, subFolder/\*\*/\*.cshtml.</param>
        /// <returns>An <see cref="IChangeToken"/> that is notified when a file matching <paramref name="pattern"/> is added, modified, or deleted.
        /// The change token will be notified when one of the change token returned by the provided <see cref="IFileProvider"/> is notified.</returns>
        public IChangeToken Watch(string pattern)
        {
            // Watch all file providers
            var changeTokens = new List<IChangeToken>();
            foreach (IFileProvider fileProvider in _fileProviders)
            {
                IChangeToken changeToken = fileProvider.Watch(pattern);
                if (changeToken is not (null or NullChangeToken))
                {
                    changeTokens.Add(changeToken);
                }
            }

            return changeTokens.Count switch
            {
                0 => NullChangeToken.Singleton,
                1 => changeTokens[0],
                _ => new CompositeChangeToken(changeTokens)
            };
        }

        /// <summary>
        /// Gets the list of configured <see cref="IFileProvider" /> instances.
        /// </summary>
        public IEnumerable<IFileProvider> FileProviders => _fileProviders;
    }
}
