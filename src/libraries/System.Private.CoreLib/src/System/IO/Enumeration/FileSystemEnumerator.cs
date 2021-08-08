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
    /// <summary>Enumerates the file system elements of the provided type that are being searched and filtered by a <see cref="FileSystemEnumerable{T}" />.</summary>
    /// <typeparam name="TResult">The type of the result produced by this file system enumerator.</typeparam>
    public unsafe abstract partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        private int _remainingRecursionDepth;

        /// <summary>Encapsulates a find operation.</summary>
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
            _remainingRecursionDepth = _options.MaxRecursionDepth;

            Init();
        }

        /// <summary>When overridden in a derived class, determines whether the specified file system entry should be included in the results.</summary>
        /// <param name="entry">A file system entry reference.</param>
        /// <returns><see langword="true" /> if the specified file system entry should be included in the results; otherwise, <see langword="false" />.</returns>
        protected virtual bool ShouldIncludeEntry(ref FileSystemEntry entry) => true;

        /// <summary>When overridden in a derived class, determines whether the specified file system entry should be recursed.</summary>
        /// <param name="entry">A file system entry reference.</param>
        /// <returns><see langword="true" /> if the specified directory entry should be recursed into; otherwise, <see langword="false" />.</returns>
        protected virtual bool ShouldRecurseIntoEntry(ref FileSystemEntry entry) => true;

        /// <summary>When overridden in a derived class, generates the result type from the current entry.</summary>
        /// <param name="entry">A file system entry reference.</param>
        /// <returns>The result type from the current entry.</returns>
        protected abstract TResult TransformEntry(ref FileSystemEntry entry);

        /// <summary>When overridden in a derived class, this method is called whenever the end of a directory is reached.</summary>
        /// <param name="directory">The directory path as a read-only span.</param>
        protected virtual void OnDirectoryFinished(ReadOnlySpan<char> directory) { }

        /// <summary>When overridden in a derived class, returns a value that indicates whether to continue execution or throw the default exception.</summary>
        /// <param name="error">The native error code.</param>
        /// <returns><see langword="true" /> to continue; <see langword="false" /> to throw the default exception for the given error.</returns>
        protected virtual bool ContinueOnError(int error) => false;

        /// <summary>Gets the currently visited element.</summary>
        /// <value>The currently visited element.</value>
        public TResult Current => _current!;

        /// <summary>Gets the currently visited object.</summary>
        /// <value>The currently visited object.</value>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="FileSystemEnumerator{T}" /> instance is cast to an <see cref="System.Collections.IEnumerator" /> interface.</remarks>
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

        /// <summary>Always throws <see cref="System.NotSupportedException" />.</summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>Releases the resources used by the current instance of the <see cref="Enumeration.FileSystemEnumerator{T}" /> class.</summary>
        public void Dispose()
        {
            InternalDispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>When overridden in a derived class, releases the unmanaged resources used by the <see cref="Enumeration.FileSystemEnumerator{T}" /> class and optionally releases the managed resources.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        ~FileSystemEnumerator()
        {
            InternalDispose(disposing: false);
        }
    }
}
