// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class LinkedList_Generic_Tests<T> : ICollection_Generic_Tests<T>
    {
        [Fact]
        public void Verify()
        {
            LinkedListNode<T> node;
            int seed = 21543;
            T value;

            //[] Verify passing default(T) into the constructor
            node = new LinkedListNode<T>(default(T));
            VerifyLinkedListNode(node, default(T), null, null, null);

            //[] Verify passing something other then default(T) into the constructor
            value = CreateT(seed++);
            node = new LinkedListNode<T>(value);
            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing something other then default(T) into the constructor and set the value to something other then default(T)
            value = CreateT(seed++);
            node = new LinkedListNode<T>(value);
            value = CreateT(seed++);
            node.Value = value;

            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing something other then default(T) into the constructor and set the value to default(T)
            value = CreateT(seed++);
            node = new LinkedListNode<T>(value);
            node.Value = default(T);

            VerifyLinkedListNode(node, default(T), null, null, null);

            //[] Verify passing default(T) into the constructor and set the value to something other then default(T)
            node = new LinkedListNode<T>(default(T));
            value = CreateT(seed++);
            node.Value = value;

            VerifyLinkedListNode(node, value, null, null, null);

            //[] Verify passing default(T) into the constructor and set the value to default(T)
            node = new LinkedListNode<T>(default(T));
            value = CreateT(seed++);
            node.Value = default(T);

            VerifyLinkedListNode(node, default(T), null, null, null);
        }
    }
}
