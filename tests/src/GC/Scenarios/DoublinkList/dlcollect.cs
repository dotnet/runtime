// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************
/* a test case based on DoubLinkStay. Instead of saving references
/* into array, it save them into List<object>
/* to see if GC can handle Collections references correctly.
/**************************************************************/

namespace DoubLink {
    using System;
    using System.Collections.Generic;

    public class DLCollect
    {

        internal DoubLink []Mv_Doub;
        internal List<DoubLink> Mv_Collect;

        public static int Main(String [] Args)
        {
            int iRep = 0;
            int iObj = 0;

            Console.WriteLine("Test should return with ExitCode 100 ...");
            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 20;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 20;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 10;
                    }
                break;

                default:
                    iRep = 20;
                    iObj = 10;
                break;

            }

            DLCollect Mv_Leak = new DLCollect();
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

            Mv_Collect = new List<DoubLink>(iRep);
            for(int i=0; i <10; i++)
            {
                SetLink(iRep, iObj);
                Mv_Collect.RemoveRange(0, Mv_Collect.Count);
                GC.Collect();
            }

            GC.WaitForPendingFinalizers();

            Console.WriteLine("{0} DLinkNodes finalized", DLinkNode.FinalCount);
            return (DLinkNode.FinalCount==iRep*iObj*10);
        }


        public void SetLink(int iRep, int iObj)
        {
            Mv_Doub = new DoubLink[iRep];

            for(int i=0; i<iRep; i++)
            {
                // create DoubLink element in array
                Mv_Doub[i] = new DoubLink(iObj);

                // add DoubLink element to List<object>
                Mv_Collect.Add(Mv_Doub[i]);

                // kill reference to DoubLink in array
                Mv_Doub[i] = null;
            }

        }

    }
}
