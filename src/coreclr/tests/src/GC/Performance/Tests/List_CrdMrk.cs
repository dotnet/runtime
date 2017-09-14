// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

//Try to allocate in such a way that card marking path is hit often
//That happens when Gen2 objects reference objects in Gen0 or Gen1
//This test exercises gc_heap::mark_through_cards_for_segments 
namespace List_CrdMrk
{
    class Node
    {
        public Node next = null;
        public Object oRef = null;

    }

    class List_CrdMrk
    {
        public static int LISTSIZE = 30000000;
        public static int ITERATIONS = 20;
        public static int STEP = 10;
        public static int SEED = 1234;
        public static Node root=null;
        static void Main(string[] args)
        {
            ParseArgs(args);

            //create a linked list
            root = new Node();

            Node current = root;
            //create LISTSIZE nodes
            for (int i = 0; i < LISTSIZE-1; i++)
            {
                current.next = new Node();
                current = current.next;
            }
            Console.WriteLine("Done creating the list");
            //Make sure nodes get in gen2
            GC.Collect();
            GC.Collect();
            
            Random Rand = new Random(SEED);
            //Allocate oRef for some of the nodes
            current = root;
        
            for (int i = 0; i < LISTSIZE/STEP; i++)
            {
                int pos = Rand.Next(0, STEP);
                AdvanceCurrent(ref current, pos);
                byte[] bArr = new byte[3];
                bArr[1] = 5;
                current.oRef = bArr;
                AdvanceCurrent(ref current, STEP - pos);
            }
         
            Console.WriteLine("Done initial allocation");
            
            //iterate deleting the old refs and allocating new ones
            Node current2;
            for (int k = 0; k < ITERATIONS; k++)
            {
                current = root;             
                for (int i = 0; i < LISTSIZE / STEP; i++)
                {
                    //delete the old ref for this section
                    current2 = current;
                    for (int j = 0; j < STEP; j++)
                    {
                        if (current2.oRef != null)
                        {
                            current2.oRef = null;
                        }
                        current2 = current.next;
                    }

                    //create a new object
                    int pos = Rand.Next(0, STEP);
                    AdvanceCurrent(ref current, pos);
                    byte[] bArr = new byte[3];
                    bArr[1] = 5;
                    current.oRef = bArr;
                    AdvanceCurrent(ref current, STEP - pos);
                }
             
                Console.WriteLine("Done iter " + k);
            }
            GC.KeepAlive(root);
        }

        //advance the current pointer
        static void AdvanceCurrent(ref Node cur, int count)
        {
           // Console.WriteLine("advancing" + count);
            for (int i = 0; i < count; i++)
            {
                cur = cur.next;
            }
        }
        static void ParseArgs(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].CompareTo("/?") == 0)
                {
                    Console.WriteLine("Usage: [ArraySize(default {0})] [Iterations(default {1})] [Step(default {2})] [RandomSeed]", LISTSIZE, ITERATIONS, STEP);
                    System.Environment.Exit(0);
                }
                else
                {
                    LISTSIZE = Int32.Parse(args[0]);
                }
            }
            if (args.Length > 1)
            {
                ITERATIONS = Int32.Parse(args[1]);
            }
            if (args.Length > 2)
            {
                STEP = Int32.Parse(args[2]);
            }
            if (args.Length > 3)
            {
                SEED = Int32.Parse(args[3]);
            }
        }

        //static void Print()
        //{
        //    Node cur = root;
        //    while (cur != null)
        //    {
        //        if (cur.oRef == null)
        //            Console.WriteLine("0");
        //        else
        //            Console.WriteLine("1");
        //        cur = cur.next;
        //    }
        //}
    }
}
