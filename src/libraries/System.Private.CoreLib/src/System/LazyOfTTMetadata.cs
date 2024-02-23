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

        public TMetadata Metadata
        {
            get
            {
                return _metadata;
            }
        }
    }
}
