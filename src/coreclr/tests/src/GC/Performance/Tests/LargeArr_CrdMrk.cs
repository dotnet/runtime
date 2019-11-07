// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

//Try to allocate in such a way that card marking path is hit often
//That happens when Gen2 objects reference objects in Gen0 or Gen1
//This test exercises gc_heap::mark_through_cards_for_segments 
namespace LargeArr_CrdMrk
{
    class MyObj
    {
        public object oRef=null;
    }
    class LargeArr_CrdMrk
    {
        public static int LISTSIZE = 30000000;
        public static int ITERATIONS = 20;
        public static int STEP = 10;
        public static int SEED = 1234;
        static int Main(string[] args)
        {
            ParseArgs(args);
            List<MyObj> MyList = new List<MyObj>(LISTSIZE);

            for (int i = 0; i < LISTSIZE; i++)
            {
                MyList.Add(new MyObj());
            }
            Console.WriteLine("Done creating large array");
            //Make sure MyList gets in Gen2
            GC.Collect();
            GC.Collect();

            Random Rand = new Random(SEED);
            //allocate a new object for every STEP objects in the array
            for (int i = 0; i < LISTSIZE; i += STEP)
            {
                int pos = Rand.Next(i, i + STEP);
                byte[] bArr = new byte[3];
                bArr[1] = 5;
                MyList[pos].oRef = bArr;

            }

            //iterate deleting the old refs and allocating new ones
            for (int k = 0; k < ITERATIONS; k++)
            {
                for (int i = 0; i < LISTSIZE; i += STEP)
                {
                    for (int j = 0; j < STEP; j++)
                    {
                        if (MyList[j].oRef != null)
                        {
                            MyList[j].oRef = null;
                        }

                    }
                    int pos = Rand.Next(i, i + STEP);
                    byte[] bArr = new byte[3];
                    bArr[1] = 5;
                    MyList[pos].oRef = bArr;
                }
                Console.WriteLine("Done iter " + k);
            }
            GC.KeepAlive(MyList);

            return 100;
        }
        static void ParseArgs(string[] args)
        {
            if(args.Length>0)
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
    }
}
