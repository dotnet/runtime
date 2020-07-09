// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************
/*Test case for testing GC with cyclic double linked list leaks
/*It's based on DoubLinkGen, the deference is its base node has 1MB
/*memory, the nodes number inside of every cyclic double linked list
/*is iObj.
/******************************************************************/

namespace DoubLink {
    using System.Threading;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    public class DLBigLeakThd
    {
        internal DoubLink []Mv_Doub;
        internal int iRep = 0;
        internal int iObj = 0;

        public static int Main(System.String [] Args)
        {
            DLBigLeakThd Mv_Leak = new DLBigLeakThd();

            int iRep = 0;
            int iObj = 0;
            int iThd = 0;
            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            //Console.SetOut(TextWriter.Synchronized(Console.Out));

            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 1;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 1;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 20;
                    }
                break;

                case 3:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 1;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 20;
                    }
                    if (!Int32.TryParse( Args[2], out iThd ))
                    {
                        iThd = 2;
                    }
                break;

                default:
                    iRep = 1;
                    iObj = 20;
                    iThd = 2;
                break;
            }

            if (Mv_Leak.runTest(iRep, iObj, iThd ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;

        }


        public bool runTest(int iRep, int iObj, int iThd)
        {
            CreateDLinkListsWithLeak(iRep, iObj, iThd, 20);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            int goal = iRep*15*iThd*iObj+20*iRep*iObj;
            Console.WriteLine("{0}/{1} DLinkNodes finalized", DLinkNode.FinalCount, goal);
            return (DLinkNode.FinalCount==goal);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        // Do not inline the method that creates GC objects, because it could
        // extend their live intervals until the end of the parent method.
        public void CreateDLinkListsWithLeak(int iRep, int iObj, int iThd, int iters)
        {
            this.iRep = iRep;
            this.iObj = iObj;
            Mv_Doub = new DoubLink[iRep];
            Thread [] Mv_Thread = new Thread[iThd];
            for(int i=0; i<iThd; i++)
            {
                Mv_Thread[i] = new Thread(new ThreadStart(this.ThreadStart));
                Mv_Thread[i].Start( );
            }
            for (int i = 0; i < iters; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }
            for(int i=0; i<iThd; i++)
            {
                Mv_Thread[i].Join();
            }
            Mv_Doub = null;
        }


        public void SetLink(int iRep, int iObj)
        {

            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = new DoubLink(iObj, true);
            }
            GC.Collect();
        }


        public void MakeLeak(int iRep)
        {
            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = null;
            }
            GC.Collect();
        }


        public void ThreadStart()
        {
            for(int i=0; i<15; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }
        }

    }
}
