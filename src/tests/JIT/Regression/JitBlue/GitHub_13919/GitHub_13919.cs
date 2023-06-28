// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Test to make sure we can compute correct loop nest even in the face
// of loop compaction.

namespace N
{
    public class C
    {
        class Node
        {
            public int value;
            public Node next;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Process(Node head, int initial, int final)
        {
            int result = 0;
            for (Node node = head; node != null; node = node.next)
            {
                int v = node.value;
                if (v == initial)
                {
                    node.value *= 2;
                    do
                    {
                        result += 5 * node.value;
                        node.value += 1;
                    }
                    while (result < final);
                    break;
                }
                result += v;
            }

            return result;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Node head = new Node { value = 6, next = new Node { value = 13, next = new Node { value = 5, next = null } } };

            int expected = 6 + 5 * 26 + 5 * 27 + 5 * 28;
            int result = Process(head, 13, expected);

            // Return 100 on success, anything else on error.
            return result - expected + 100;
        }
    }
}
