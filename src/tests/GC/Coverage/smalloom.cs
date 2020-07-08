// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//Regression test for Dev 10 bug 479239: GC hangs on x86 rather than throwing OOM
 
using System;
using System.Runtime;

class TestClass
{
	public static int Main()
	{
        ByteArrayList list = new ByteArrayList();


        try
        {
            while (true)
            {
                list.AddByteArray(84500);
            }
        }
        catch (OutOfMemoryException)
        {
        }

        Console.Write("NodesAllocated: ");
        Console.WriteLine(list.NodeCount);
        return 100;
	}

    class ByteArrayList
    {
        class Node
        {
            byte[] data = null;
            int size = 0;
            public Node next = null;

            public Node(int Size)
            {
              data = new byte[Size];
              size = Size;
            }

        }
       
        Node head;

        public int NodeCount = 0;
        public ByteArrayList()
        {
            head = null;
        }

        public void AddByteArray(int size)
        {
            Node newNode = new Node(size);

            if (head == null)
                head = newNode;
            else
            {
                newNode.next = head;
                head = newNode;
            }
            NodeCount++;
        }
    }

}			