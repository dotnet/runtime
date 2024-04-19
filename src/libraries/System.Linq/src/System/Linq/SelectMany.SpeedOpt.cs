// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class SelectManySingleSelectorIterator<TSource, TResult>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource element in _source)
                {
                    checked
                    {
                        count += _selector(element).Count();
                    }
                }

                return count;
            }

            public override TResult[] ToArray()
            {
                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                Func<TSource, IEnumerable<TResult>> selector = _selector;
                foreach (TSource item in _source)
                {
                    builder.AddRange(selector(item));
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public override List<TResult> ToList()
            {
                var list = new List<TResult>();

                Func<TSource, IEnumerable<TResult>> selector = _selector;
                foreach (TSource element in _source)
                {
                    list.AddRange(selector(element));
                }

                return list;
            }
        }
    }
}
