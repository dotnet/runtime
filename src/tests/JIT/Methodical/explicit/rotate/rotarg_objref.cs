// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_rotarg_objref_cs
{
    public class App
    {
        public static int s_weightCount = 1;
        public static int s_objCount = 0;

        private class Weight
        {
            public Weight(int val) { m_value = val; }
            public int m_value;
        }

        private class Node
        {
            public Weight m_weight;
            public Node m_leftChild, m_rightChild;

            public Node()
            {
                s_objCount++;
                m_weight = new Weight(s_weightCount++);
            }

            ~Node()
            {
                //Console.WriteLine("Deleting " + m_weight.ToString());
                s_objCount--;
            }

            public void growTree(int maxHeight, String indent)
            {
                //Console.WriteLine(indent + m_weight.ToString());
                if (maxHeight > 0)
                {
                    m_leftChild = new Node();
                    m_leftChild.growTree(maxHeight - 1, indent + " ");
                    m_rightChild = new Node();
                    m_rightChild.growTree(maxHeight - 1, indent + " ");
                }
                else
                    m_leftChild = m_rightChild = null;
            }

            public void rotateTree(ref Weight leftWeight, ref Weight rightWeight)
            {
                //Console.WriteLine("rotateTree(" + m_weight.ToString() + ") - begin");
                Node newLeftChild = null, newRightChild = null;
                int objCount = s_objCount;
                if (m_leftChild != null)
                {
                    newRightChild = new Node();
                    objCount++;
                    newRightChild.m_leftChild = m_leftChild.m_leftChild;
                    newRightChild.m_rightChild = m_leftChild.m_rightChild;
                    newRightChild.m_weight = m_leftChild.m_weight;
                }
                if (m_rightChild != null)
                {
                    newLeftChild = new Node();
                    objCount++;
                    newLeftChild.m_leftChild = m_rightChild.m_leftChild;
                    newLeftChild.m_rightChild = m_rightChild.m_rightChild;
                    newLeftChild.m_weight = m_rightChild.m_weight;
                }
                m_leftChild = newLeftChild;
                m_rightChild = newRightChild;
                for (int I = 0; I < 1024; I++) { int[] u = new int[1024]; }
                GC.Collect();
                if (m_rightChild != null)
                {
                    if (m_rightChild.m_leftChild != null &&
                        m_rightChild.m_rightChild != null)
                    {
                        m_rightChild.rotateTree(
                            ref m_rightChild.m_leftChild.m_weight,
                            ref m_rightChild.m_rightChild.m_weight);
                    }
                    else
                    {
                        Weight minus1 = null;
                        m_rightChild.rotateTree(ref minus1, ref minus1);
                    }
                    if (leftWeight != m_rightChild.m_weight)
                    {
                        Console.WriteLine("left weight do not match.");
                        throw new Exception();
                    }
                }
                if (m_leftChild != null)
                {
                    if (m_leftChild.m_leftChild != null &&
                        m_leftChild.m_rightChild != null)
                    {
                        m_leftChild.rotateTree(
                            ref m_leftChild.m_leftChild.m_weight,
                            ref m_leftChild.m_rightChild.m_weight);
                    }
                    else
                    {
                        Weight minus1 = null;
                        m_leftChild.rotateTree(ref minus1, ref minus1);
                    }
                    if (rightWeight != m_leftChild.m_weight)
                    {
                        Console.WriteLine("right weight do not match.");
                        throw new Exception();
                    }
                }
                //Console.WriteLine("rotateTree(" + m_weight.ToString() + ") - end");
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Node root = new Node();
            root.growTree(4, "");
            root.rotateTree(ref root.m_leftChild.m_weight, ref root.m_rightChild.m_weight);
            return 100;
        }
    }
}
