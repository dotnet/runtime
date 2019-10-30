// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************
/* a test case based on DoubLinkStay. Instead of delete all of
/* the reference to a Cyclic Double linked list, it creats one
/* reference to the first node of the linked list and save it in
/* a local array in SetLink, then delete all old reference. To
/* check if GC collects leak when the local array out of stack.
/**************************************************************/

namespace DoubLink {

    using System;
    using System.Runtime.CompilerServices;

    public class DLStack
    {

        internal DoubLink[] Mv_Doub;
        internal int n_count = 0;

        public static int Main(System.String [] Args)
        {
            int iRep = 100;
            int iObj = 10;

            Console.WriteLine("Test should return with ExitCode 100 ...");
            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 100;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 100;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 10;
                    }
                break;

                default:
                    iRep = 100;
                    iObj = 10;
                break;
            }

            DLStack Mv_Leak = new DLStack();
            if(Mv_Leak.runTest(iRep, iObj ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool DrainFinalizerQueue(int iRep, int iObj)
        {
            int lastValue = DLinkNode.FinalCount;
            while (true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (DLinkNode.FinalCount == iRep * iObj * 10)
                {
                    return true;
                }

                if (DLinkNode.FinalCount != lastValue)
                {
                    Console.WriteLine(" Performing Collect/Wait/Collect cycle again");
                    lastValue = DLinkNode.FinalCount;
                    continue;
                }

                Console.WriteLine(" Finalized number stable at " + lastValue);
                return false;
            }
        }


        public bool runTest(int iRep, int iObj)
        {
            CreateDLinkListsWithLeak(iRep, iObj, 10);

            bool success = false;
            if (DrainFinalizerQueue(iRep, iObj))
            {
                success = true;
            }

            Console.WriteLine("{0} DLinkNodes finalized", DLinkNode.FinalCount);
            return success;

        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        // Do not inline the method that creates GC objects, because it could 
        // extend their live intervals until the end of the parent method.
        public void CreateDLinkListsWithLeak(int iRep, int iObj, int iters)
        {
            for(int i = 0; i < iters; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }
        }


        public void SetLink(int iRep, int iObj)
        {
            DLinkNode[] Mv_DLink;

            Mv_Doub = new DoubLink[iRep];
            Mv_DLink = new DLinkNode[iRep*10];

            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = new DoubLink(iObj);
                Mv_DLink[n_count] = Mv_Doub[i][0];
                n_count++;
            }

        }


        public void MakeLeak(int iRep)
        {
            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = null;
            }
        }

    }
}
