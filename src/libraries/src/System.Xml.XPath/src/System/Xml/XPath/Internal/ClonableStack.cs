// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MS.Internal.Xml.XPath
{
    internal sealed class ClonableStack<T> : System.Collections.Generic.List<T>
    {
        public ClonableStack() { }
        public ClonableStack(int capacity) : base(capacity) { }

        private ClonableStack(System.Collections.Generic.IEnumerable<T> collection) : base(collection) { }

        public void Push(T value)
        {
            base.Add(value);
        }

        public T Pop()
        {
            int last = base.Count - 1;
            T result = base[last];
            base.RemoveAt(last);
            return result;
        }

        public T Peek()
        {
            return base[base.Count - 1];
        }

        public ClonableStack<T> Clone() { return new ClonableStack<T>(this); }
    }
}
