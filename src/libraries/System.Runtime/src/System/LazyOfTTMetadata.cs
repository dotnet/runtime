// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System
{
    public class Lazy<T, TMetadata> : Lazy<T>
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
