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


        public bool runTest(int iRep, int iObj)
        {

            for(int i=0; i <10; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }

			long lastTotalMemory = long.MaxValue;
			long curTotalMemory = GC.GetTotalMemory(false);
			
			while (lastTotalMemory != curTotalMemory)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();

				lastTotalMemory = curTotalMemory;
				curTotalMemory = GC.GetTotalMemory(false);
			}

            Console.WriteLine("{0} DLinkNodes finalized", DLinkNode.FinalCount);
            return (DLinkNode.FinalCount==iRep*iObj*10);

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
