// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;


// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/******************************************************************
/*Test case for testing GC with cyclic single linked list leaks
/*In every loop. SetLink() to create a SingLink object array whose size
/*is iRep,  each SingLink Object is a iObj node cyclic single
/*linked list. MakeLeak() deletes all the object reference in the array
/*to make all the cyclic single linked lists become memory leaks.
/******************************************************************/

namespace SingLink
{
    public class SingLinkStay
    {
        internal SingLink[] Mv_Sing;

        public static int Main(System.String[] Args)
        {
            int iRep = 0;
            int iObj = 0;

            Console.WriteLine("Test should return with ExitCode 100 ...");
            switch (Args.Length)
            {
                case 1:
                    if (!Int32.TryParse(Args[0], out iRep))
                    {
                        iRep = 100;
                    }
                    break;
                case 2:
                    if (!Int32.TryParse(Args[0], out iRep))
                    {
                        iRep = 100;
                    }
                    if (!Int32.TryParse(Args[1], out iObj))
                    {
                        iObj = 10;
                    }
                    break;
                default:
                    iRep = 100;
                    iObj = 10;
                    break;
            }

            SingLinkStay Mv_Leak = new SingLinkStay();
            if (Mv_Leak.runTest(iRep, iObj))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test Failed");
                return 1;
            }
        }


        public bool runTest(int iRep, int iObj)
        {
            for (int i = 0; i < 20; i++)
            {
                SetLink(iRep, iObj);
                MakeLeak(iRep);
            }
            return true;
        }


        public void SetLink(int iRep, int iObj)
        {
            Mv_Sing = new SingLink[iRep];
            for (int i = 0; i < iRep; i++)
            {
                Mv_Sing[i] = new SingLink(iObj);
            }
        }


        public void MakeLeak(int iRep)
        {
            for (int i = 0; i < iRep; i++)
            {
                Mv_Sing[i] = null;
            }
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
