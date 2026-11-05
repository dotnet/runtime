// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System
{
    public class Lazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T, TMetadata> : Lazy<T>
    {
        private readonly TMetadata _metadata;

        public Lazy(Func<T> valueFactory, TMetadata metadata) :
            base(valueFactory)
        {
            _metadata = metadata;
        }

        public Lazy(TMetadata metadata) :
            base()
        {
            _metadata = metadata;
        }


        public Lazy(TMetadata metadata, bool isThreadSafe) :
            base(isThreadSafe)
        {
            _metadata = metadata;
        }

        public Lazy(Func<T> valueFactory, TMetadata metadata, bool isThreadSafe) :
            base(valueFactory, isThreadSafe)
        {
            _metadata = metadata;
        }

        public Lazy(TMetadata metadata, LazyThreadSafetyMode mode) :
            base(mode)
        {
            _metadata = metadata;
        }

        public Lazy(Func<T> valueFactory, TMetadata metadata, LazyThreadSafetyMode mode) :
            base(valueFactory, mode)
        {
            _metadata = metadata;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T, TMetadata}"/> class with the specified metadata, initialization function
        /// and thread-safety mode, and that specifies whether exceptions thrown by the initialization function are cached.
        /// </summary>
        /// <param name="valueFactory">The <see cref="Func{T}"/> invoked to produce the lazily-initialized value when it is needed.</param>
        /// <param name="metadata">The metadata associated with the value.</param>
        /// <param name="mode">The lazy thread-safety mode.</param>
        /// <param name="cacheExceptions">
        /// <see langword="true"/> to cache an exception thrown by <paramref name="valueFactory"/>; <see langword="false"/> to invoke
        /// <paramref name="valueFactory"/> again on the next access to <see cref="Lazy{T}.Value"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> contains an invalid value.</exception>
        public Lazy(Func<T> valueFactory, TMetadata metadata, LazyThreadSafetyMode mode, bool cacheExceptions) :
            base(valueFactory, mode, cacheExceptions)
        {
            _metadata = metadata;
        }

        public TMetadata Metadata
        {
            get
            {
                return _metadata;
            }
        }
    }
}
