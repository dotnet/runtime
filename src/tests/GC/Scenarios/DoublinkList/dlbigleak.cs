// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************
/*Test case for testing GC with cyclic double linked list leaks
/*It's based on DoubLinkGen, the deference is its base node has 1MB
/*memory, the nodes number inside of every cyclic double linked list
/*is iObj.
/******************************************************************/


namespace DoubLink {
    using System;
    using System.Runtime.CompilerServices;

    public class DLBigLeak
    {
        internal DoubLink[] Mv_Doub;

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
                        iRep = 5;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 30;
                    }
                break;

                default:
                    iRep = 5;
                    iObj = 30;
                break;

            }

            DLBigLeak Mv_Leak = new DLBigLeak();
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
            GC.WaitForPendingFinalizers();

            Console.Write(DLinkNode.FinalCount);
            Console.WriteLine(" DLinkNodes finalized");
            return (DLinkNode.FinalCount==iRep*iObj*10);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        // Do not inline the method that creates GC objects, because it could
        // extend their live intervals until the end of the parent method.
        public void CreateDLinkListsWithLeak(int iRep, int iObj, int iters)
        {
            Mv_Doub = new DoubLink[iRep];
            for (int i = 0; i < iters; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
                GC.Collect();
            }
        }


        public void SetLink(int iRep, int iObj)
        {

            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[0] = new DoubLink(iObj, true);
                GC.Collect();
            }

        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public void MakeLeak(int iRep)
        {

            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = null;
            }

        }

    }

    public class DoubLink
    {
        internal DLinkNode[] Mv_DLink;

        public DoubLink(int Num)
            : this(Num, false)
        {
        }

        public DoubLink(int Num, bool large)
        {

            Mv_DLink = new DLinkNode[Num];

            if (Num == 0)
            {
                return;
            }

            if (Num == 1)
            {
                // only one element
                Mv_DLink[0] = new DLinkNode((large ? 250 : 1), Mv_DLink[0], Mv_DLink[0]);
                return;
            }

            // first element
            Mv_DLink[0] = new DLinkNode((large ? 250 : 1), Mv_DLink[Num - 1], Mv_DLink[1]);

            // all elements in between
            for (int i = 1; i < Num - 1; i++)
            {
                Mv_DLink[i] = new DLinkNode((large ? 250 : i + 1), Mv_DLink[i - 1], Mv_DLink[i + 1]);
            }

            // last element
            Mv_DLink[Num - 1] = new DLinkNode((large ? 250 : Num), Mv_DLink[Num - 2], Mv_DLink[0]);
        }


        public int NodeNum
        {
            get
            {
                return Mv_DLink.Length;
            }
        }


        public DLinkNode this[int index]
        {
            get
            {
                return Mv_DLink[index];
            }

            set
            {
                Mv_DLink[index] = value;
            }
        }

    }

    public class DLinkNode
    {
        // disabling unused variable warning
#pragma warning disable 0414
        internal DLinkNode Last;
        internal DLinkNode Next;
        internal int[] Size;
#pragma warning restore 0414

        public static int FinalCount = 0;

        public DLinkNode(int SizeNum, DLinkNode LastObject, DLinkNode NextObject)
        {
            Last = LastObject;
            Next = NextObject;
            Size = new int[SizeNum * 1024];
        }

        ~DLinkNode()
        {
            FinalCount++;
        }
    }
}
