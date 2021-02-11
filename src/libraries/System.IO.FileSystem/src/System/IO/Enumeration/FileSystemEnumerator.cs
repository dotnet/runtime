// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    public unsafe abstract partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        /// <summary>
        /// Encapsulates a find operation.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="options">Enumeration options to use.</param>
        public FileSystemEnumerator(string directory, EnumerationOptions? options = null)
            : this(directory, isNormalized: false, options)
        {
        }

        /// <summary>
        /// Encapsulates a find operation.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="isNormalized">Whether the directory path is already normalized or not.</param>
        /// <param name="options">Enumeration options to use.</param>
        internal FileSystemEnumerator(string directory, bool isNormalized, EnumerationOptions? options = null)
        {
            _originalRootDirectory = directory ?? throw new ArgumentNullException(nameof(directory));

            string path = isNormalized ? directory : Path.GetFullPath(directory);
            _rootDirectory = Path.TrimEndingDirectorySeparator(path);
            _options = options ?? EnumerationOptions.Default;

            Init();
        }

        /// <summary>
        /// Return true if the given file system entry should be included in the results.
        /// </summary>
        protected virtual bool ShouldIncludeEntry(ref FileSystemEntry entry) => true;

        /// <summary>
        /// Return true if the directory entry given should be recursed into.
        /// </summary>
        protected virtual bool ShouldRecurseIntoEntry(ref FileSystemEntry entry) => true;

        /// <summary>
        /// Generate the result type from the current entry;
        /// </summary>
        protected abstract TResult TransformEntry(ref FileSystemEntry entry);

        /// <summary>
        /// Called whenever the end of a directory is reached.
        /// </summary>
        /// <param name="directory">The path of the directory that finished.</param>
        protected virtual void OnDirectoryFinished(ReadOnlySpan<char> directory) { }

        /// <summary>
        /// Called when a native API returns an error that would normally cause a throw.
        /// Return true to continue, or false to throw the default exception for the given error.
        /// </summary>
        /// <param name="error">The native error code.</param>
        protected virtual bool ContinueOnError(int error) => false;

        public TResult Current => _current!;

        object? IEnumerator.Current => Current;

        private void DirectoryFinished()
        {
            _entry = default;

            // Close the handle now that we're done
            CloseDirectoryHandle();
            OnDirectoryFinished(_currentPath.AsSpan());

            // Attempt to grab another directory to process
            if (!DequeueNextDirectory())
            {
                _lastEntryFound = true;
            }
            else
            {
                FindNextEntry();
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            InternalDispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override for any additional cleanup.
        /// </summary>
        /// <param name="disposing">True if called while disposing. False if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        ~FileSystemEnumerator()
        {
            InternalDispose(disposing: false);
        }
    }
}
