// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections;


// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/****************************************************************************
/*Title:    GCQueue
/*Purpose:  Stress GC to see if it can collect dead GCQueue objects and BitArray
/*          objects. BitArrays are element of Queue.
/*Coverage: GCQueue objects lost reference;
/*          GCQueue objects out of scope;
/*          BitArray objects lost reference as elements of Queue;
/*arguments:iRep: count of G_Queue, repeat time of local stacks assignment.
/*          iObj: count of local Queues' elements.
/****************************************************************************/


namespace DefaultNamespace
{
    internal class BitArrayNode
    {
        public BitArrayNode(int num)
        {
            int[] temp = new int[num];
            for (int i = 0; i < num; i++)
            {
                temp[i] = i;
            }
            BitArray L_Node = new BitArray(temp);
        }
    }

    internal class GCQueue
    {
        internal static Queue G_Queue;
        public static int Main(string[] args)
        {
            int iRep = 0;
            int iObj = 0;
            Console.Out.WriteLine("Test should return with ExitCode 100 ...");

            switch (args.Length)
            {
                case 1:
                    if (!Int32.TryParse(args[0], out iRep))
                    {
                        iRep = 5;
                    }
                    break;
                case 2:
                    if (!Int32.TryParse(args[0], out iRep))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse(args[1], out iObj))
                    {
                        iObj = 5000;
                    }
                    break;
                default:
                    iRep = 5;
                    iObj = 5000;
                    break;
            }

            Console.Out.WriteLine("iRep= " + iRep + " ; iObj= " + iObj);
            GCQueue Mv_Obj = new GCQueue();

            if (Mv_Obj.runTest(iRep, iObj))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool runTest(int iRep, int iObj)
        {
            try
            {
                for (int i = 0; i < iRep; i++)
                {
                    G_Queue = new Queue(0);
                    for (int j = 0; j < iObj; j++)
                    {
                        G_Queue.Enqueue(new BitArray(new int[j]));
                    }
                    Console.Out.WriteLine("i= " + i);
                    MakeLeak(iRep, iObj);
                    GC.Collect();
                }
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("Caught OOM");
                return false;
            }

            return true;
        }


        public void MakeLeak(int iRep, int iObj)
        {
            Queue L_Queue1;
            Queue L_Queue2 = new Queue(1);
            Queue L_Queue3 = new Queue(1, 1);

            byte[] l_obj1;
            BitArrayNode l_obj2;
            int[] l_obj3;

            for (int i = 0; i < iRep; i++)
            {
                L_Queue1 = new Queue();
                for (int j = 1; j < iObj; j++)
                {
                    l_obj1 = new byte[j];
                    l_obj2 = new BitArrayNode(j);
                    l_obj3 = new int[j];
                    l_obj1[0] = (byte)1;
                    l_obj3[0] = 1;

                    if (j > 1)
                    {
                        l_obj1[j - 1] = (byte)2;
                    }

                    L_Queue1.Enqueue(new BitArray(l_obj1));
                    L_Queue2.Enqueue(l_obj2);
                    L_Queue3.Enqueue(new BitArray(l_obj3));
                }

                L_Queue2.Clear();
                while (L_Queue3.Count > 0)
                {
                    L_Queue3.Dequeue();
                }
            }
        }
    }
}
