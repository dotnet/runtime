// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit1_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private ulong _BASEPAD_0;
            private int _BASEPAD_1;
            private String _BASEPAD_2;
            private uint _BASEPAD_3;
            private ushort _BASEPAD_4;
            private int _BASEPAD_5;
            private uint _BASEPAD_6;
            private ulong _BASEPAD_7;
            private String _BASEPAD_8;
            private int _BASEPAD_9;
            private ulong _BASEPAD_10;
            private char _BASEPAD_11;
            private char _BASEPAD_12;
            private int _BASEPAD_13;

            public BaseNode()
            {
                _BASEPAD_0 = 102;
                _BASEPAD_1 = 185;
                _BASEPAD_2 = "29233";
                _BASEPAD_3 = 180;
                _BASEPAD_4 = 112;
                _BASEPAD_5 = 181;
                _BASEPAD_6 = 169;
                _BASEPAD_7 = 161;
                _BASEPAD_8 = "18456";
                _BASEPAD_9 = 164;
                _BASEPAD_10 = 91;
                _BASEPAD_11 = '`';
                _BASEPAD_12 = 'O';
                _BASEPAD_13 = 37;
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 102) throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != 185) throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != "29233") throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != 180) throw new Exception("m_BASEPAD_3");
                if (_BASEPAD_4 != 112) throw new Exception("m_BASEPAD_4");
                if (_BASEPAD_5 != 181) throw new Exception("m_BASEPAD_5");
                if (_BASEPAD_6 != 169) throw new Exception("m_BASEPAD_6");
                if (_BASEPAD_7 != 161) throw new Exception("m_BASEPAD_7");
                if (_BASEPAD_8 != "18456") throw new Exception("m_BASEPAD_8");
                if (_BASEPAD_9 != 164) throw new Exception("m_BASEPAD_9");
                if (_BASEPAD_10 != 91) throw new Exception("m_BASEPAD_10");
                if (_BASEPAD_11 != '`') throw new Exception("m_BASEPAD_11");
                if (_BASEPAD_12 != 'O') throw new Exception("m_BASEPAD_12");
                if (_BASEPAD_13 != 37) throw new Exception("m_BASEPAD_13");
            }
        }

        private class Node : BaseNode
        {
            public Node m_leftChild;
            private ulong _MID1PAD_0;
            private ushort _MID1PAD_1;
            private byte _MID1PAD_2;
            private byte _MID1PAD_3;
            private char _MID1PAD_4;
            private int _MID1PAD_5;
            private int _MID1PAD_6;
            private String _MID1PAD_7;
            private ushort _MID1PAD_8;
            private uint _MID1PAD_9;
            private byte _MID1PAD_10;
            public int m_weight;
            private int _MID2PAD_0;
            private String _MID2PAD_1;
            private uint _MID2PAD_2;
            private ulong _MID2PAD_3;
            private char _MID2PAD_4;
            private ushort _MID2PAD_5;
            private uint _MID2PAD_6;
            private uint _MID2PAD_7;
            private ulong _MID2PAD_8;
            private ushort _MID2PAD_9;
            private uint _MID2PAD_10;
            public Node m_rightChild;

            public Node()
            {
                m_weight = s_weightCount++;
                _MID1PAD_0 = 131;
                _MID1PAD_1 = 43;
                _MID1PAD_2 = 156;
                _MID1PAD_3 = 160;
                _MID1PAD_4 = '=';
                _MID1PAD_5 = 38;
                _MID1PAD_6 = 174;
                _MID1PAD_7 = "22662";
                _MID1PAD_8 = 72;
                _MID1PAD_9 = 221;
                _MID1PAD_10 = 198;
                _MID2PAD_0 = 192;
                _MID2PAD_1 = "29543";
                _MID2PAD_2 = 122;
                _MID2PAD_3 = 162;
                _MID2PAD_4 = '%';
                _MID2PAD_5 = 32;
                _MID2PAD_6 = 40;
                _MID2PAD_7 = 79;
                _MID2PAD_8 = 41;
                _MID2PAD_9 = 134;
                _MID2PAD_10 = 113;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_MID1PAD_0 != 131) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 43) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != 156) throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 160) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != '=') throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 38) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 174) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != "22662") throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != 72) throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != 221) throw new Exception("m_MID1PAD_9");
                if (_MID1PAD_10 != 198) throw new Exception("m_MID1PAD_10");
                if (_MID2PAD_0 != 192) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != "29543") throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 122) throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 162) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != '%') throw new Exception("m_MID2PAD_4");
                if (_MID2PAD_5 != 32) throw new Exception("m_MID2PAD_5");
                if (_MID2PAD_6 != 40) throw new Exception("m_MID2PAD_6");
                if (_MID2PAD_7 != 79) throw new Exception("m_MID2PAD_7");
                if (_MID2PAD_8 != 41) throw new Exception("m_MID2PAD_8");
                if (_MID2PAD_9 != 134) throw new Exception("m_MID2PAD_9");
                if (_MID2PAD_10 != 113) throw new Exception("m_MID2PAD_10");
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
