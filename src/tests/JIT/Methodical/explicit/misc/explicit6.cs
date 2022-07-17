// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit6_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private char _BASEPAD_0;

            public BaseNode()
            {
                _BASEPAD_0 = 'k';
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 'k') throw new Exception("m_BASEPAD_0");
            }
        }

        private class Node : BaseNode
        {
            private ulong _PREPAD_0;
            private byte _PREPAD_1;
            private byte _PREPAD_2;
            private String _PREPAD_3;
            private ulong _PREPAD_4;
            private String _PREPAD_5;
            public Node m_leftChild;
            private ulong _MID1PAD_0;
            private ushort _MID1PAD_1;
            private String _MID1PAD_2;
            public int m_weight;
            private byte _MID2PAD_0;
            private uint _MID2PAD_1;
            private char _MID2PAD_2;
            private byte _MID2PAD_3;
            private String _MID2PAD_4;
            private ulong _MID2PAD_5;
            private ulong _MID2PAD_6;
            private String _MID2PAD_7;
            private byte _MID2PAD_8;
            private uint _MID2PAD_9;
            private uint _MID2PAD_10;
            private int _MID2PAD_11;
            public Node m_rightChild;
            private ulong _AFTERPAD_0;
            private ulong _AFTERPAD_1;
            private ulong _AFTERPAD_2;
            private uint _AFTERPAD_3;
            private int _AFTERPAD_4;
            private ushort _AFTERPAD_5;
            private byte _AFTERPAD_6;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 84;
                _PREPAD_1 = 230;
                _PREPAD_2 = 70;
                _PREPAD_3 = "31530";
                _PREPAD_4 = 97;
                _PREPAD_5 = "2235";
                _MID1PAD_0 = 171;
                _MID1PAD_1 = 56;
                _MID1PAD_2 = "29777";
                _MID2PAD_0 = 63;
                _MID2PAD_1 = 0;
                _MID2PAD_2 = 'P';
                _MID2PAD_3 = 62;
                _MID2PAD_4 = "28537";
                _MID2PAD_5 = 221;
                _MID2PAD_6 = 214;
                _MID2PAD_7 = "32307";
                _MID2PAD_8 = 83;
                _MID2PAD_9 = 223;
                _MID2PAD_10 = 43;
                _MID2PAD_11 = 174;
                _AFTERPAD_0 = 220;
                _AFTERPAD_1 = 194;
                _AFTERPAD_2 = 125;
                _AFTERPAD_3 = 109;
                _AFTERPAD_4 = 126;
                _AFTERPAD_5 = 48;
                _AFTERPAD_6 = 214;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 84) throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != 230) throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != 70) throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != "31530") throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 97) throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != "2235") throw new Exception("m_PREPAD_5");
                if (_MID1PAD_0 != 171) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 56) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != "29777") throw new Exception("m_MID1PAD_2");
                if (_MID2PAD_0 != 63) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != 0) throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 'P') throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 62) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != "28537") throw new Exception("m_MID2PAD_4");
                if (_MID2PAD_5 != 221) throw new Exception("m_MID2PAD_5");
                if (_MID2PAD_6 != 214) throw new Exception("m_MID2PAD_6");
                if (_MID2PAD_7 != "32307") throw new Exception("m_MID2PAD_7");
                if (_MID2PAD_8 != 83) throw new Exception("m_MID2PAD_8");
                if (_MID2PAD_9 != 223) throw new Exception("m_MID2PAD_9");
                if (_MID2PAD_10 != 43) throw new Exception("m_MID2PAD_10");
                if (_MID2PAD_11 != 174) throw new Exception("m_MID2PAD_11");
                if (_AFTERPAD_0 != 220) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 194) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != 125) throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != 109) throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != 126) throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != 48) throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 214) throw new Exception("m_AFTERPAD_6");
            }

            public virtual Node growTree(int maxHeight, String indent)
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
                return this;
            }

            public virtual void rotateTree(ref int leftWeight, ref int rightWeight)
            {
                //Console.WriteLine("rotateTree(" + m_weight.ToString() + ")");
                VerifyValid();

                //	create node objects for children
                Node newLeftChild = null, newRightChild = null;
                if (m_leftChild != null)
                {
                    newRightChild = new Node();
                    newRightChild.m_leftChild = m_leftChild.m_leftChild;
                    newRightChild.m_rightChild = m_leftChild.m_rightChild;
                    newRightChild.m_weight = m_leftChild.m_weight;
                }
                if (m_rightChild != null)
                {
                    newLeftChild = new Node();
                    newLeftChild.m_leftChild = m_rightChild.m_leftChild;
                    newLeftChild.m_rightChild = m_rightChild.m_rightChild;
                    newLeftChild.m_weight = m_rightChild.m_weight;
                }

                //	replace children
                m_leftChild = newLeftChild;
                m_rightChild = newRightChild;

                for (int I = 0; I < 32; I++) { int[] u = new int[1024]; }

                //	verify all valid
                if (m_rightChild != null)
                {
                    if (m_rightChild.m_leftChild != null &&
                        m_rightChild.m_rightChild != null)
                    {
                        m_rightChild.m_leftChild.VerifyValid();
                        m_rightChild.m_rightChild.VerifyValid();
                        m_rightChild.rotateTree(
                            ref m_rightChild.m_leftChild.m_weight,
                            ref m_rightChild.m_rightChild.m_weight);
                    }
                    else
                    {
                        int minus1 = -1;
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
                        m_leftChild.m_leftChild.VerifyValid();
                        m_leftChild.m_rightChild.VerifyValid();
                        m_leftChild.rotateTree(
                            ref m_leftChild.m_leftChild.m_weight,
                            ref m_leftChild.m_rightChild.m_weight);
                    }
                    else
                    {
                        int minus1 = -1;
                        m_leftChild.rotateTree(ref minus1, ref minus1);
                    }
                    if (rightWeight != m_leftChild.m_weight)
                    {
                        Console.WriteLine("right weight do not match.");
                        throw new Exception();
                    }
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Node root = new Node();
                root.growTree(6, "").rotateTree(
                    ref root.m_leftChild.m_weight,
                    ref root.m_rightChild.m_weight);
            }
            catch (Exception)
            {
                Console.WriteLine("*** FAILED ***");
                return 1;
            }
            Console.WriteLine("*** PASSED ***");
            return 100;
        }
    }
}
