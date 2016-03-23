// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

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
                m_aMem[0] = (byte) 10;
                m_aMem[999] = (byte) 10;
            }
            else
            {
                m_aMem = new byte[10];
                m_aMem[0] = (byte) 10;
                m_aMem[9] = (byte) 10;
            }

            Switch = !Switch;
        }

        public void Grow()
        {
            m_aMem = new byte[(m_iCount+=100)];
            m_aMem[0] = (byte) 10;
            m_aMem[m_iCount-1] = (byte) 10;
        }
    }


    public class BinTree
    {
        internal Node m_pRoot;
        internal Random m_Random;
        internal TreeType m_TreeType;

        public BinTree(int ThreadId, TreeType treeType)
        {
            // the following intended to ensure the console output was legible...
            //Console.SetOut(TextWriter.Synchronized(Console.Out));
            m_TreeType = treeType;
            m_pRoot = null;
            m_Random = new Random();
        }


        public void Empty (int ThreadId)
        {
            Console.Out.WriteLine("Thread " + ThreadId + ": Tree Empty");
            m_pRoot = null;
        }


        public void AddNodes (int howMany, int ThreadId)
        {
            for (int i = 0; i < howMany; i++)
            {
                m_pRoot = Insert(m_pRoot, m_Random.Next(100));
            }
            Console.Out.WriteLine("Thread " + ThreadId + " Added: " + howMany + " Nodes: " + GC.GetTotalMemory(false));
        }


        public void DeleteNodes (int howMany, int ThreadId)
        {
            for (int i = 0; i < howMany; i++)
            {
                m_pRoot = Delete(m_pRoot, m_Random.Next(100) );
            }
            Console.Out.WriteLine("Thread " + ThreadId +" Deleted: " + howMany + " Nodes: " + GC.GetTotalMemory(false));
        }


        public Node Insert(Node root, int element)
        {
            if(root == null)                                            //if is NULL make a new node
            {                                                           //and copy number to the new node
                root=new Node();                                        //make new node
                root.m_data = element;                                  //copy number
                root.m_pLeft=null ;                                     //set the children to NULL
                root.m_pRight=null;
            }
            else if(element < root.m_data)
            {
                root.m_pLeft = Insert(root.m_pLeft, element);
            }
            else
            {
                root.m_pRight = Insert(root.m_pRight, element);
            }

            if (m_TreeType==TreeType.Growing)
            {
                root.Grow();
            }
            else if (m_TreeType==TreeType.Living)
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
                if(root.m_pRight == null)                                       //check if it has right child.
                {                                                           //If it has no right child
                    return root.m_pLeft;
                }

                if (root.m_pLeft == null)
                {
                    return root.m_pRight;
                }
                else
                {
                    for (temp = root.m_pLeft; temp.m_pRight != null; temp = temp.m_pRight);
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

            if (m_TreeType==TreeType.Growing)
            {
                root.Grow();
            }
            else if (m_TreeType==TreeType.Living)
            {
                root.Live();
            }

            return root;
        }
    }
}
