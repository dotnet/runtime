// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************
/* a test case based on DoubLinkStay. Instead of delete all of
/* the reference to a Cyclic Double linked list, it creats one
/* reference to the first node of the linked list, then delete all old
/* reference. This test trys to make fake leak for GC.
/**************************************************************/

namespace DoubLink {
    using System;
    using System.Runtime.CompilerServices;

    public class DoubLinkNoLeak2
    {

        internal DoubLink[] Mv_Doub;
        internal DLinkNode[] Mv_DLink;
        internal int n_count = 0;

        public static int Main(System.String [] Args)
        {
            int iRep = 0;
            int iObj = 0;

            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    iObj = 10;
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 10;
                    }
                break;

                default:
                    iRep = 5;
                    iObj = 10;
                break;
            }

            DoubLinkNoLeak2 Mv_Leak = new DoubLinkNoLeak2();
            if(Mv_Leak.runTest(iRep, iObj ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;

        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void CreateMvDLink(int iRep) {
            Mv_DLink = new DLinkNode[iRep * 10];
        }
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DestroyMvDLink() {
            Mv_DLink = null;
        }
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DestroyMVDoub() {
            Mv_Doub = null;
        }

        public bool runTest(int iRep, int iObj)
        {
            CreateMvDLink(iRep);

            for(int i=0; i <10; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }

            DestroyMvDLink();
            DestroyMVDoub();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            //do a second GC collect since some nodes may have been still alive at the time of first collect
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int totalNodes = iRep * iObj * 10;
            Console.Write(DLinkNode.FinalCount);
            Console.Write(" DLinkNodes finalized out of ");
            Console.WriteLine(totalNodes);

            return (DLinkNode.FinalCount == totalNodes);

        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void SetLink(int iRep, int iObj)
        {
            Mv_Doub = new DoubLink[iRep];

            for(int i=0; i<iRep; i++)
            {
                Mv_Doub[i] = new DoubLink(iObj);

                Mv_DLink[n_count] = Mv_Doub[i][0];
                n_count++;
            }

        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
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
