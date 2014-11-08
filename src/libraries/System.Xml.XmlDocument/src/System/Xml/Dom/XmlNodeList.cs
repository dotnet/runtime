// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace System.Xml
{
    // Represents an ordered collection of nodes.
    public abstract class XmlNodeList : IEnumerable, IDisposable
    {
        // Retrieves a node at the given index.
        public abstract XmlNode Item(int index);

        // Gets the number of nodes in this XmlNodeList.
        public abstract int Count { get; }

        // Provides a simple ForEach-style iteration over the collection of nodes in
        // this XmlNodeList.
        public abstract IEnumerator GetEnumerator();

        // Retrieves a node at the given index.
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public virtual XmlNode this[int i] { get { return Item(i); } }

        void IDisposable.Dispose()
        {
            PrivateDisposeNodeList();
        }

        protected virtual void PrivateDisposeNodeList() { }
    }
}

