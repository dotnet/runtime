// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Linq.Parallel.Tests
{
    public partial class ParallelQueryCombinationTests
    {
        public delegate ParallelQuery<int> Operation(int start, int count, Operation source = null);

        public static LabeledOperation Label(string label, Operation item)
        {
            return new LabeledOperation(label, item);
        }

        public struct LabeledOperation
        {
            private readonly string _label;
            private readonly Operation _item;

            public Operation Item
            {
                get { return _item; }
            }

            internal LabeledOperation(string label, Operation item)
            {
                _label = label;
                _item = item;
            }

            public override string ToString()
            {
                return _label;
            }

            public LabeledOperation Append(LabeledOperation next)
            {
                Operation op = Item;
                Operation nxt = next.Item;
                return Label(ToString() + "|" + next.ToString(), (start, count, source) => nxt(start, count, (s, c, ignore) => op(s, c, source)));
            }
        }
    }
}
