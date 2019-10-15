// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************
/* a basic test case for GC with cyclic single linked list leaks.
/* Creat a SingLink object which is a cyclic single linked list
/* object with iObj number node. then deletes its reference when
/* the next object is created. Do this loop iRep times.
/**************************************************************/


namespace SingLink {
    using System;
    using System.Runtime.CompilerServices;

    public class SingLinkGen
    {
// disabling unused variable warning
#pragma warning disable 0414
        internal SingLink Mv_Sing;
#pragma warning restore 0414

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

            SingLinkGen Mv_Leak = new SingLinkGen();

            if(Mv_Leak.runTest(iRep, iObj ))
            {
                Console.WriteLine( "Test Passed" );
                return 100;
            }
            else
            {
                Console.WriteLine( "Test Failed" );
                return 1;
            }
        }


        public bool runTest(int iRep, int iObj)
        {
            int retVal = SetLink(iRep, iObj);

            Console.Write("Times ~LinkNode() was called: ");
            Console.WriteLine(retVal);
            return ( retVal == iRep*iObj);
        }
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void Create(int iObj) {
            Mv_Sing = new SingLink(iObj);
        }
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void Delete() {
            Mv_Sing = null;
            GC.Collect();
        }

        public int SetLink(int iRep, int iObj)
        {
            for(int i=0; i<iRep; i++)
            {
                Create(iObj);
                //Console.WriteLine("after number {0} singlink is set: {1}", i, GC.GetTotalMemory(false) );

                Delete();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

            }
            //Console.WriteLine("total allocated memory: {0}", GC.GetTotalMemory(false));

            return LinkNode.FinalCount;

        }

    }

    public class LinkNode
    {
        // disabling unused variable warning
#pragma warning disable 0414
        internal LinkNode Last;
        internal int[] Size;
#pragma warning restore 0414

        public static int FinalCount = 0;

        ~LinkNode()
        {
            FinalCount++;
        }

        public LinkNode(int SizeNum, LinkNode LastObject)
        {
            Last = LastObject;
            Size = new int[SizeNum * 1024];
        }
    }

    public class SingLink
    {
        internal LinkNode[] Mv_SLink;

        public SingLink(int Num)
        {
            Mv_SLink = new LinkNode[Num];

            if (Num == 0)
            {
                return;
            }

            if (Num == 1)
            {
                Mv_SLink[0] = new LinkNode(1, Mv_SLink[0]);
            }
            else
            {
                Mv_SLink[0] = new LinkNode(1, Mv_SLink[Num - 1]);
            }

            for (int i = 1; i < Num - 1; i++)
            {
                Mv_SLink[i] = new LinkNode((i + 1), Mv_SLink[i - 1]);
            }

            Mv_SLink[Num - 1] = new LinkNode(Num, Mv_SLink[0]);
        }
    }
}

