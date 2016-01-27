// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
