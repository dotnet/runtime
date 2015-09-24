// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Linq.Parallel.Tests
{
    internal static class Labeled
    {
        public static Labeled<T> Label<T>(string label, T item)
        {
            return new Labeled<T>(label, item);
        }

        public static Labeled<ParallelQuery<T>> Order<T>(this Labeled<ParallelQuery<T>> query)
        {
            return Label(query.ToString() + "-Ordered", query.Item.AsOrdered());
        }
    }

    public struct Labeled<T>
    {
        private readonly string _label;
        private readonly T _item;

        public T Item
        {
            get { return _item; }
        }

        internal Labeled(string label, T item)
        {
            _label = label;
            _item = item;
        }

        public override string ToString()
        {
            return _label;
        }
    }
}
