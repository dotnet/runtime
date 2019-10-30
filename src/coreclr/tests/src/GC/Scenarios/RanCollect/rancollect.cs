// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**************************************************************************
/*Test: RanCollect
/*args: iRep--the repeat times of alloc and delete
/*  iObj--max elements' number for every collection object
/*  iBigSize -- max Bignode's size. (10 <-->4MB)
/*  iSeed -- seed of random generator, for getting random elements' number
/*Description:This test use collection objects (L_ArrList2, L_Queue, L_ArrList1) and
/*  Variant(L_Vart). It has iRep loops. Inside every loop, create random number
/*  elements in Collection Objects and Variant Object.every element's size
/*  is random, from 0 to iBigSize*iBigSize*10*4KB, for very loop, it also
/*  delete random number elements or all the objects. samply change the four
/*  arguments, you can get handreds of GC condition.
/****************************************************************************/

namespace DefaultNamespace {
    using System;
    //using System.Collections.Generic;

    internal class RanCollect
    {
        public static int Main(String [] Args)
        {
            int iRep = 0;
            int iObj = 0;
            int iBigSize = 0;
            int iSeed = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            if (Args.Length == 4)
            {
                if (!Int32.TryParse( Args[0], out iRep) ||
                    !Int32.TryParse( Args[1], out iObj) ||
                    !Int32.TryParse( Args[2], out iBigSize ) ||
                    !Int32.TryParse( Args[3], out iSeed ) )
                    {
                        return 1;
                    }
            }
            else
            {
                iRep = 10;
                iObj = 100;
                iBigSize = 2;
                iSeed = 49;
            }

            if(iObj <= 10)
            {
                Console.WriteLine("the second argument must be larger than 10.");
                return 1;
            }

            Console.Write("iRep= ");
            Console.Write(iRep);
            Console.Write(" ; iObj= ");
            Console.Write(iObj);
            Console.Write(" ; iBigSize=");
            Console.Write(iBigSize);
            Console.Write(" ; iSeed = ");
            Console.WriteLine(iSeed);

            RanCollect Mv_Obj = new RanCollect();

            if(Mv_Obj.runTest(iRep, iObj, iBigSize, iSeed))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.WriteLine("Test Failed");
            return 1;
        }


        public virtual bool runTest(int iRep, int iObj, int iBigSize, int iSeed)
        {

            ArrayList L_ArrList1 = new ArrayList();  //whose node is big double link object (DoubLinkBig).
            ArrayList L_ArrList2 = new ArrayList();   //whose node is MinNode .
            Queue L_Queue = new Queue();    //Whose node is DLRanBigNode.
            Random r = new Random(iSeed);

            int num = r.Next (10, iObj-1);
            int delnum;
            Object [] L_Vart = null;

            Console.Write(num);
            Console.WriteLine (" number's elements in collection objects");
            for(int i=0; i<iRep;i++)
            {
                /*allocate memory*/
                L_Vart = new Object[num];

                for(int j=0; j<num; j++)
                {
                    int Size= r.Next(3, num); //the size of nodes.
                    /*L_ArrList1 element's size is from 0 to iBigSize*iBigSize*10*4KB*/
                    L_ArrList1.Add(new DoubLinkBig(r.Next(iBigSize)));

                    /*L_ArrList2 element's size is Size number bytes;*/
                    L_ArrList2.Add( new MinNode(Size));

                    /*L_Queue element's size is from 0 to 1M*/
                    L_Queue.Enqueue(new DLRanBigNode(250, null, null));

                    if(j%6==0)
                    {
                        L_Vart[j] = (new DLRanBigNode(250, null, null));
                    }
                    else
                    {
                        L_Vart[j] = (new MinNode(Size));
                    }

                    L_ArrList1.RemoveAt(0);
                }

                /*start to make leak*/

                if(r.Next(1, iRep)/3 == 0 || num < iObj/8)  //learn all the nodes
                {
                    num = r.Next(10, iObj-1);

                    L_ArrList1 = new ArrayList();  //whose node is big double link object (DoubLinkBig).
                    L_ArrList2 = new ArrayList();   //whose node is MinNode .
                    L_Queue = new Queue();  //Whose node is DLRanBigNode.
                    Console.WriteLine("all objects were deleted at the end of loop {0}",i);
                    Console.WriteLine ("{0} number's elements in every collection objects in loop {1}", num, (i+1));
                }
                else
                {
                    if (L_ArrList2.Count <=1)
                    {
                        delnum = 1;
                    }
                    else
                    {
                        delnum = r.Next (1, L_ArrList2.Count);  //going to delete delnum nodes
                    }

                    if (delnum > (L_ArrList2.Count*3/4))
                    {
                        delnum = L_ArrList2.Count/2;
                    }
                    num = L_ArrList2.Count - delnum;   //going to add num nodes

                    for(int j=0; j<delnum; j++)
                    {
                        L_ArrList2.RemoveAt(0);
                        L_Queue.Dequeue();
                    }
                    Console.WriteLine("{0} were deleted in each collections at the end of loop {1}", delnum, i);
                    Console.WriteLine ("{0} elements in each collection objects in loop ", num*2, (i+1));

                }

            }

            return true;
        }
    }

    public class DoubLinkBig
    {
        internal DLRanBigNode[] Mv_DLink;
        internal int NodeNum;
        public DoubLinkBig(int Num)
        {
            NodeNum = Num;
            Mv_DLink = new DLRanBigNode[Num];

            if (Num == 0)
            {
                return;
            }

            if (Num == 1)
            {
                Mv_DLink[0] = new DLRanBigNode(Num * 10, Mv_DLink[0], Mv_DLink[0]);
                return;
            }

            Mv_DLink[0] = new DLRanBigNode(Num * 10, Mv_DLink[Num - 1], Mv_DLink[1]);
            for (int i = 1; i < Num - 1; i++)
            {
                Mv_DLink[i] = new DLRanBigNode(Num * 10, Mv_DLink[i - 1], Mv_DLink[i + 1]);
            }
            Mv_DLink[Num - 1] = new DLRanBigNode(Num * 10, Mv_DLink[Num - 2], Mv_DLink[0]);

        }

        public virtual int GetNodeNum()
        {
            return NodeNum;
        }
    }

    internal class MinNode
    {
        public MinNode(int size)
        {

            byte[] obj = new byte[size];

            if (size > 0)
            {
                obj[0] = (byte)10;
                if (size > 1)
                {
                    obj[size - 1] = (byte)11;
                }
            }

        }
    }

    public class DLRanBigNode
    {
        // disabling unused variable warning
#pragma warning disable 0414
        internal DLRanBigNode Last;
        internal DLRanBigNode Next;
        internal int[] Size;
#pragma warning restore 0414

        internal static int FACTOR = 1024;

        public DLRanBigNode(int SizeNum, DLRanBigNode LastObject, DLRanBigNode NextObject)
        {
            Last = LastObject;
            Next = NextObject;
            Random r = new Random(10);
            Size = new int[FACTOR * r.Next(SizeNum)];
        }
    }


    //Queue implemented as a circular array
    class Queue
    {
        int m_Capacity = 20; //default capacity
        int m_Size = 0;
        Object[] m_Array;
        int m_First = 0;
        int m_Last = -1;

        public Queue()
        {
            m_Array = new Object[m_Capacity];
        }
        public Queue(int capacity)
        {
            m_Capacity = capacity;
            m_Array = new Object[m_Capacity];
        }
        public int Count
        {
            get
            {
                return m_Size;
            }
        }
       
        public void Enqueue(Object obj)
        {
            if(m_Size >= m_Capacity) //array full; increase capacity
            {
                int newCapacity = m_Capacity * 2;
                Object[] newArray = new Object[newCapacity];

                int current = m_First;
                for (int i = 0; i < m_Size; i++)
                {
                    newArray[0] = m_Array[current];
                    current = (current+1) % m_Capacity;
                }
                m_Array = newArray;
                m_First = 0;
                m_Last = m_Size - 1;
                m_Capacity = newCapacity;
            }
           
            m_Last++;
            if(m_Last == m_Capacity) //wrap around 
                m_Last = m_Last % m_Capacity;
            m_Array[m_Last] = obj;
            m_Size++;
        }

        public Object Dequeue()
        {
            if (m_Size == 0)
                throw new InvalidOperationException();

            Object returnObject = m_Array[m_First];
            m_Array[m_First] = null;
            m_First = (m_First+1) % m_Capacity;
            m_Size--;
            return returnObject;
        }
    }

    class ArrayList
    {
        int m_Capacity = 20; //default capacity
        int m_Size = 0;
        Object[] m_Array;

        public ArrayList()
        {
            m_Array = new Object[m_Capacity];
        }
        public ArrayList(int capacity)
        {
            m_Capacity = capacity;
            m_Array = new Object[m_Capacity];
        }

        public int Count
        {
            get
            {
                return m_Size;
            }
        }

        public int Capacity
        {
            get
            {
                return m_Capacity;
            }
        }

        //Add an Object; returns the array index at which the object was added;
        public int Add(Object obj)
        {

            if (m_Size >= m_Capacity) //increase capacity
            {
                int newCapacity = m_Capacity * 2;
                Object[] newArray = new Object[newCapacity];
                for (int i = 0; i < m_Size; i++)
                {
                    newArray[i] = m_Array[i];
                }
                m_Array = newArray;
                m_Capacity = newCapacity;
            }

           
            m_Array[m_Size] = obj;
            m_Size++;
            return (m_Size - 1);
       
        }

        public void RemoveAt(int position)
        {
            if (position < 0 || position >= m_Size)
                throw new ArgumentOutOfRangeException();

            m_Array[position] = null;

            //shift elements to fill the empty slot
            for (int i = position; i < m_Size-1; i++)
            {
                m_Array[i] = m_Array[i + 1];
            }
            m_Size--;
        }
    }
}
