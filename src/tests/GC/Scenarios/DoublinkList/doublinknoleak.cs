// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************
/* this test creates Cyclic double linked list objects in a loop,
/* before creat a new object, it delete all node references to root
/* except the first node to make a fake leak for GC.
/**************************************************************/

namespace DoubLink {
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class DoubLinkNoLeak
    {
        internal DoubLink[] Mv_Doub;
        internal Queue<DLinkNode> Mv_Save = new Queue<DLinkNode>(10);

        public static int Main(System.String [] Args)
        {
            int iRep = 0;
            int iObj = 0;

            Console.WriteLine("Test should return with ExitCode 100 ...");
            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 30;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 30;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 10;
                    }
                break;

                default:
                    iRep = 30;
                    iObj = 10;
                break;
            }

            DoubLinkNoLeak Mv_Leak = new DoubLinkNoLeak();
            if(Mv_Leak.runTest(iRep, iObj ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool runTest(int iRep, int iObj)
        {
            CreateDLinkListsWithLeak(iRep, iObj, 10);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Console.WriteLine("{0} DLinkNodes finalized", DLinkNode.FinalCount);
            return (DLinkNode.FinalCount==iRep*iObj*10);

        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        // Do not inline the method that creates GC objects, because it could
        // extend their live intervals until the end of the parent method.
        public void CreateDLinkListsWithLeak(int iRep, int iObj, int iters)
        {
            for(int i = 0; i < iters; i++)
            {
                SetLink(iRep, iObj);
            }

            Mv_Doub = null;
            Mv_Save = null;
        }


        public void SetLink(int iRep, int iObj)
        {
            Mv_Doub = new DoubLink[iRep];
            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = new DoubLink(iObj);
                Mv_Save.Enqueue(Mv_Doub[i][0]);
            }

        }

    }
}
