// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    internal sealed class ClonableStack<T> : System.Collections.Generic.List<T>
    {
        public ClonableStack() { }

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
