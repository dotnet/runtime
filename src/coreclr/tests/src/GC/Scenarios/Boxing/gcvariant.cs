// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GCVariant {
    using System;

    internal class GCVariant
    {

        internal static object [] G_Vart;

        public static int Main(String [] Args)
        {
            int iRep = 0;
            int iObj = 0;
            int iNum = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    iObj = 100;
                    iNum = 10;
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 100;
                    }
                    iNum = 10;
                break;

                case 3:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 100;
                    }
                    if (!Int32.TryParse( Args[2], out iNum ))
                    {
                        iNum = 10;
                    }
                break;

                default:
                    iRep = 5;
                    iObj = 100;
                    iNum = 10;
                break;
            }

           
            Console.Write("iRep= ");
            Console.Write(iRep);
            Console.Write(" iObj= ");
            Console.Write(iObj);
            Console.Write(" iNum= ");
            Console.WriteLine(iNum);

            GCVariant Mv_Obj = new GCVariant();

            if(Mv_Obj.runTest(iRep, iObj, iNum ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool runTest(int iRep, int iObj, int iNum)
        {
            DoubLink L_Node1 = new DoubLink(iNum);
            DLinkNode L_Node2 = new DLinkNode(iNum, null, null);

            for(int i= 0; i< iRep; i++)
            {
                G_Vart = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    if(j%2 == 1)
                        G_Vart[j] = (L_Node1);
                    else
                        G_Vart[j] = (L_Node2);
                }
                MakeLeak(iRep, iObj, iNum);

            }
            return true;
        }


        public void MakeLeak(int iRep, int iObj, int iNum)
        {
            DoubLink L_Node1 = new DoubLink(iNum);
            DLinkNode L_Node2 = new DLinkNode(iNum, null, null);
            Object [] L_Vart1 = new Object[iObj];
            Object [] L_Vart2;

            for(int i= 0; i< iRep; i++)
            {
                L_Vart2 = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    if(j%2 == 1)
                    {
                        L_Vart1[j] = (j);
                        L_Vart2[j] = ((double)j);
                    }
                    else
                    {
                        L_Vart2[j] = (L_Node2);
                        L_Vart1[j] = (L_Node1);
                    }
                }
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
                Mv_DLink[0] = new DLinkNode((large ? 256 : 1), Mv_DLink[0], Mv_DLink[0]);
                return;
            }

            // first element
            Mv_DLink[0] = new DLinkNode((large ? 256 : 1), Mv_DLink[Num - 1], Mv_DLink[1]);

            // all elements in between
            for (int i = 1; i < Num - 1; i++)
            {
                Mv_DLink[i] = new DLinkNode((large ? 256 : i + 1), Mv_DLink[i - 1], Mv_DLink[i + 1]);
            }

            // last element
            Mv_DLink[Num - 1] = new DLinkNode((large ? 256 : Num), Mv_DLink[Num - 2], Mv_DLink[0]);
        }


        public int NodeNum
        {
            get
            {
                return Mv_DLink.Length;
            }
        }


    }

    public class DLinkNode
    {
        // disabling unused variable warning
#pragma warning disable 0414
        internal DLinkNode Last;
        internal DLinkNode Next;
#pragma warning restore 0414

        internal int[] Size;

        public DLinkNode(int SizeNum, DLinkNode LastObject, DLinkNode NextObject)
        {
            Last = LastObject;
            Next = NextObject;
            Size = new int[SizeNum * 1024];
            Size[0] = 1;
            Size[SizeNum * 1024 - 1] = 2;
        }
    }

}
