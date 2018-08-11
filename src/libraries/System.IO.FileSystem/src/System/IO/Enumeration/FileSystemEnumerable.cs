// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

#if MS_IO_REDIST
namespace Microsoft.IO.Enumeration
#else
namespace System.IO.Enumeration
#endif
{
    /// <summary>
    /// Enumerable that allows utilizing custom filter predicates and tranform delegates.
    /// </summary>
    public class FileSystemEnumerable<TResult> : IEnumerable<TResult>
    {
        private DelegateEnumerator _enumerator;
        private readonly FindTransform _transform;
        private readonly EnumerationOptions _options;
        private readonly string _directory;

        public FileSystemEnumerable(string directory, FindTransform transform, EnumerationOptions options = null)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _options = options ?? EnumerationOptions.Default;

            // We need to create the enumerator up front to ensure that we throw I/O exceptions for
            // the root directory on creation of the enumerable.
            _enumerator = new DelegateEnumerator(this);
        }

        public FindPredicate ShouldIncludePredicate { get; set; }
        public FindPredicate ShouldRecursePredicate { get; set; }

        public IEnumerator<TResult> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new DelegateEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Delegate for filtering out find results.
        /// </summary>
        public delegate bool FindPredicate(ref FileSystemEntry entry);

        /// <summary>
        /// Delegate for transforming raw find data into a result.
        /// </summary>
        public delegate TResult FindTransform(ref FileSystemEntry entry);

        private sealed class DelegateEnumerator : FileSystemEnumerator<TResult>
        {
            private readonly FileSystemEnumerable<TResult> _enumerable;

            public DelegateEnumerator(FileSystemEnumerable<TResult> enumerable)
                : base(enumerable._directory, enumerable._options)
            {
                _enumerable = enumerable;
            }

            protected override TResult TransformEntry(ref FileSystemEntry entry) => _enumerable._transform(ref entry);
            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
                => _enumerable.ShouldRecursePredicate?.Invoke(ref entry) ?? true;
            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => _enumerable.ShouldIncludePredicate?.Invoke(ref entry) ?? true;
        }
    }
}
