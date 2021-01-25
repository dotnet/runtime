// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading;
using System;
using System.IO;


// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/**
 * Description:
 *      Mainly stresses the GC by creating n threads each manipulating its own local binary tree.
 *      Differs from thdtree in a way that the nodes of the binary trees grow during the lifetime.
 */



namespace DefaultNamespace
{
    public enum TreeType
    {
        Normal,
        Growing,
        Living
    }

    public class Node
    {
        internal int m_data;
        internal Node m_pLeft;
        internal Node m_pRight;
        internal byte[] m_aMem;
        internal bool Switch;
        internal int m_iCount;

        public Node()
        {
            m_aMem = new byte[10];
            m_aMem[0] = (byte)10;
            m_aMem[9] = (byte)10;
        }

        public void Live()
        {
            if (Switch)
            {
                m_aMem = new byte[1000];
                m_aMem[0] = (byte)10;
                m_aMem[999] = (byte)10;
            }
            else
            {
                m_aMem = new byte[10];
                m_aMem[0] = (byte)10;
                m_aMem[9] = (byte)10;
            }

            Switch = !Switch;
        }

        public void Grow()
        {
            m_aMem = new byte[(m_iCount += 100)];
            m_aMem[0] = (byte)10;
            m_aMem[m_iCount - 1] = (byte)10;
        }
    }


    public class BinTree
    {
        internal Node m_pRoot;
        internal Random m_Random;
        internal TreeType m_TreeType;

        public BinTree(int ThreadId, TreeType treeType)
        {
            m_TreeType = treeType;
            m_pRoot = null;
            m_Random = new Random();
        }


        public void Empty(int ThreadId)
        {
            Console.Out.WriteLine("Thread " + ThreadId + ": Tree Empty");
            m_pRoot = null;
        }


        public void AddNodes(int howMany, int ThreadId)
        {
            for (int i = 0; i < howMany; i++)
            {
                m_pRoot = Insert(m_pRoot, m_Random.Next(100));
            }
            Console.Out.WriteLine("Thread " + ThreadId + " Added: " + howMany + " Nodes: " + GC.GetTotalMemory(false));
        }


        public void DeleteNodes(int howMany, int ThreadId)
        {
            for (int i = 0; i < howMany; i++)
            {
                m_pRoot = Delete(m_pRoot, m_Random.Next(100));
            }
            Console.Out.WriteLine("Thread " + ThreadId + " Deleted: " + howMany + " Nodes: " + GC.GetTotalMemory(false));
        }


        public Node Insert(Node root, int element)
        {
            if (root == null)                                            //if is NULL make a new node
            {                                                           //and copy number to the new node
                root = new Node();                                        //make new node
                root.m_data = element;                                  //copy number
                root.m_pLeft = null;                                     //set the children to NULL
                root.m_pRight = null;
            }
            else if (element < root.m_data)
            {
                root.m_pLeft = Insert(root.m_pLeft, element);
            }
            else
            {
                root.m_pRight = Insert(root.m_pRight, element);
            }

            if (m_TreeType == TreeType.Growing)
            {
                root.Grow();
            }
            else if (m_TreeType == TreeType.Living)
            {
                root.Live();
            }

            return root;
        }


        public Node Delete(Node root, int element)
        {
            Node temp = null;

            if (root == null)
            {
                return null;                                                //Node not found
            }
            else if (element == root.m_data)                                 //if it was the first data (node)
            {
                if (root.m_pRight == null)                                       //check if it has right child.
                {                                                           //If it has no right child
                    return root.m_pLeft;
                }

                if (root.m_pLeft == null)
                {
                    return root.m_pRight;
                }
                else
                {
                    for (temp = root.m_pLeft; temp.m_pRight != null; temp = temp.m_pRight) ;
                    root.m_data = temp.m_data;
                    root.m_pLeft = Delete(root.m_pLeft, temp.m_data);
                }
            }
            else if (root.m_data > element)
            {
                root.m_pLeft = Delete(root.m_pLeft, element);
            }
            else
            {
                root.m_pRight = Delete(root.m_pRight, element);
            }

            if (m_TreeType == TreeType.Growing)
            {
                root.Grow();
            }
            else if (m_TreeType == TreeType.Living)
            {
                root.Live();
            }

            return root;
        }
    }

    public class TreeThread
    {
        internal int[] mA_Count;
        internal int m_id = 0;
        internal BinTree m_BinTree;
        internal Thread Mv_Thread;

        public TreeThread(int ThreadId, TreeType treeType, int[] count)
        {
            mA_Count = count;
            m_BinTree = new BinTree(ThreadId, treeType);
            m_id = ThreadId;
            Mv_Thread = new Thread(new ThreadStart(this.ThreadStart));
            Mv_Thread.Start();
            Console.Out.WriteLine("Started Thread: " + m_id);
        }

        public void ThreadStart()
        {                                           //All threads start here
            for (int i = 0; i < mA_Count.Length; i++)
            {
                if (mA_Count[i] == 0)
                {
                    m_BinTree.Empty(m_id);
                }
                else if (mA_Count[i] > 0)
                {
                    m_BinTree.AddNodes(mA_Count[i], m_id);
                }
                else
                {
                    m_BinTree.DeleteNodes((mA_Count[i] * -1), m_id);
                }
            }
        }
    }

    public class ThdTreeGrowingObj
    {
        public static int Main(string[] args)
        {
            int iNofThread = 0;

            if (args.Length == 1)
            {
                if (!Int32.TryParse(args[0], out iNofThread))
                {
                    iNofThread = 2;
                }
            }
            else
            {
                iNofThread = 2;
            }

            int[] count = { 300, 1000, -350, 0, 71, 200 };
            TreeThread Mv_TreeThread;
            for (int i = 0; i < iNofThread; i++)
            {
                Mv_TreeThread = new TreeThread(i, TreeType.Growing, count);              //Each treethread object launches a thread
            }
            return 100;
        }
    }
}
