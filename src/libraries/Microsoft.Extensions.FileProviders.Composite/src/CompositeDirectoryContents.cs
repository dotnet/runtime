// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.FileProviders.Composite
{
    /// <summary>
    /// Represents the result of a call composition of <see cref="IFileProvider.GetDirectoryContents(string)"/>
    /// for a list of <see cref="IFileProvider"/> and a path.
    /// </summary>
    public class CompositeDirectoryContents : IDirectoryContents
    {
        private readonly IList<IFileProvider> _fileProviders;
        private readonly string _subPath;
        private List<IFileInfo> _files;
        private bool _exists;
        private List<IDirectoryContents> _directories;

        /// <summary>
        /// Creates a new instance of <see cref="CompositeDirectoryContents"/> to represents the result of a call composition of
        /// <see cref="IFileProvider.GetDirectoryContents(string)"/>.
        /// </summary>
        /// <param name="fileProviders">The list of <see cref="IFileProvider"/> for which the results have to be composed.</param>
        /// <param name="subpath">The path.</param>
        public CompositeDirectoryContents(IList<IFileProvider> fileProviders, string subpath)
        {
            if (fileProviders == null)
            {
                throw new ArgumentNullException(nameof(fileProviders));
            }
            _fileProviders = fileProviders;
            _subPath = subpath;
        }

        private void EnsureDirectoriesAreInitialized()
        {
            if (_directories == null)
            {
                _directories = new List<IDirectoryContents>();
                foreach (var fileProvider in _fileProviders)
                {
                    var directoryContents = fileProvider.GetDirectoryContents(_subPath);
                    if (directoryContents != null && directoryContents.Exists)
                    {
                        _exists = true;
                        _directories.Add(directoryContents);
                    }
                }
            }
        }

        private void EnsureFilesAreInitialized()
        {
            EnsureDirectoriesAreInitialized();
            if (_files == null)
            {
                _files = new List<IFileInfo>();
                var names = new HashSet<string>();
                for (var i = 0; i < _directories.Count; i++)
                {
                    var directoryContents = _directories[i];
                    foreach (var file in directoryContents)
                    {
                        if (names.Add(file.Name))
                        {
                            _files.Add(file);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates an enumerator for all files in all providers given.
        /// Ensures each item in the collection is distinct.
        /// </summary>
        /// <returns>An enumerator over all files in all given providers</returns>
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            EnsureFilesAreInitialized();
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureFilesAreInitialized();
            return _files.GetEnumerator();
        }

        /// <summary>
        /// True if any given providers exists
        /// </summary>
        public bool Exists
        {
            get
            {
                EnsureDirectoriesAreInitialized();
                return _exists;
            }
        }
    }
}