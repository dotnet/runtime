// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit7_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private String _BASEPAD_0;
            private String _BASEPAD_1;
            private uint _BASEPAD_2;
            private ushort _BASEPAD_3;
            private uint _BASEPAD_4;
            private char _BASEPAD_5;
            private uint _BASEPAD_6;
            private ushort _BASEPAD_7;
            private int _BASEPAD_8;
            private byte _BASEPAD_9;
            private uint _BASEPAD_10;
            private char _BASEPAD_11;
            private int _BASEPAD_12;
            private ushort _BASEPAD_13;
            private uint _BASEPAD_14;
            private ulong _BASEPAD_15;
            private int _BASEPAD_16;

            public BaseNode()
            {
                _BASEPAD_0 = "404";
                _BASEPAD_1 = "16309";
                _BASEPAD_2 = 167;
                _BASEPAD_3 = 138;
                _BASEPAD_4 = 99;
                _BASEPAD_5 = 'I';
                _BASEPAD_6 = 172;
                _BASEPAD_7 = 5;
                _BASEPAD_8 = 108;
                _BASEPAD_9 = 46;
                _BASEPAD_10 = 147;
                _BASEPAD_11 = 'u';
                _BASEPAD_12 = 92;
                _BASEPAD_13 = 17;
                _BASEPAD_14 = 209;
                _BASEPAD_15 = 129;
                _BASEPAD_16 = 145;
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != "404") throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != "16309") throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != 167) throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != 138) throw new Exception("m_BASEPAD_3");
                if (_BASEPAD_4 != 99) throw new Exception("m_BASEPAD_4");
                if (_BASEPAD_5 != 'I') throw new Exception("m_BASEPAD_5");
                if (_BASEPAD_6 != 172) throw new Exception("m_BASEPAD_6");
                if (_BASEPAD_7 != 5) throw new Exception("m_BASEPAD_7");
                if (_BASEPAD_8 != 108) throw new Exception("m_BASEPAD_8");
                if (_BASEPAD_9 != 46) throw new Exception("m_BASEPAD_9");
                if (_BASEPAD_10 != 147) throw new Exception("m_BASEPAD_10");
                if (_BASEPAD_11 != 'u') throw new Exception("m_BASEPAD_11");
                if (_BASEPAD_12 != 92) throw new Exception("m_BASEPAD_12");
                if (_BASEPAD_13 != 17) throw new Exception("m_BASEPAD_13");
                if (_BASEPAD_14 != 209) throw new Exception("m_BASEPAD_14");
                if (_BASEPAD_15 != 129) throw new Exception("m_BASEPAD_15");
                if (_BASEPAD_16 != 145) throw new Exception("m_BASEPAD_16");
            }
        }

        private class Node : BaseNode
        {
            private int _PREPAD_0;
            private ulong _PREPAD_1;
            private String _PREPAD_2;
            private ushort _PREPAD_3;
            private uint _PREPAD_4;
            private uint _PREPAD_5;
            private String _PREPAD_6;
            private String _PREPAD_7;
            private ushort _PREPAD_8;
            private ulong _PREPAD_9;
            private String _PREPAD_10;
            private ulong _PREPAD_11;
            private ushort _PREPAD_12;
            private uint _PREPAD_13;
            private int _PREPAD_14;
            public Node m_leftChild;
            private char _MID1PAD_0;
            private int _MID1PAD_1;
            private char _MID1PAD_2;
            private int _MID1PAD_3;
            private ulong _MID1PAD_4;
            private int _MID1PAD_5;
            private ushort _MID1PAD_6;
            private int _MID1PAD_7;
            private ulong _MID1PAD_8;
            private byte _MID1PAD_9;
            private uint _MID1PAD_10;
            private ulong _MID1PAD_11;
            private int _MID1PAD_12;
            private char _MID1PAD_13;
            private char _MID1PAD_14;
            public int m_weight;
            private uint _MID2PAD_0;
            public Node m_rightChild;
            private int _AFTERPAD_0;
            private ulong _AFTERPAD_1;
            private String _AFTERPAD_2;
            private String _AFTERPAD_3;
            private String _AFTERPAD_4;
            private ushort _AFTERPAD_5;
            private ulong _AFTERPAD_6;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 13;
                _PREPAD_1 = 130;
                _PREPAD_2 = "5130";
                _PREPAD_3 = 249;
                _PREPAD_4 = 58;
                _PREPAD_5 = 166;
                _PREPAD_6 = "8200";
                _PREPAD_7 = "864";
                _PREPAD_8 = 36;
                _PREPAD_9 = 136;
                _PREPAD_10 = "21789";
                _PREPAD_11 = 63;
                _PREPAD_12 = 49;
                _PREPAD_13 = 214;
                _PREPAD_14 = 100;
                _MID1PAD_0 = '(';
                _MID1PAD_1 = 8;
                _MID1PAD_2 = 'w';
                _MID1PAD_3 = 21;
                _MID1PAD_4 = 19;
                _MID1PAD_5 = 100;
                _MID1PAD_6 = 3;
                _MID1PAD_7 = 50;
                _MID1PAD_8 = 201;
                _MID1PAD_9 = 3;
                _MID1PAD_10 = 172;
                _MID1PAD_11 = 230;
                _MID1PAD_12 = 214;
                _MID1PAD_13 = 'z';
                _MID1PAD_14 = ';';
                _MID2PAD_0 = 153;
                _AFTERPAD_0 = 84;
                _AFTERPAD_1 = 74;
                _AFTERPAD_2 = "30023";
                _AFTERPAD_3 = "31182";
                _AFTERPAD_4 = "31631";
                _AFTERPAD_5 = 8;
                _AFTERPAD_6 = 17;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 13) throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != 130) throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != "5130") throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != 249) throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 58) throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != 166) throw new Exception("m_PREPAD_5");
                if (_PREPAD_6 != "8200") throw new Exception("m_PREPAD_6");
                if (_PREPAD_7 != "864") throw new Exception("m_PREPAD_7");
                if (_PREPAD_8 != 36) throw new Exception("m_PREPAD_8");
                if (_PREPAD_9 != 136) throw new Exception("m_PREPAD_9");
                if (_PREPAD_10 != "21789") throw new Exception("m_PREPAD_10");
                if (_PREPAD_11 != 63) throw new Exception("m_PREPAD_11");
                if (_PREPAD_12 != 49) throw new Exception("m_PREPAD_12");
                if (_PREPAD_13 != 214) throw new Exception("m_PREPAD_13");
                if (_PREPAD_14 != 100) throw new Exception("m_PREPAD_14");
                if (_MID1PAD_0 != '(') throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 8) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != 'w') throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 21) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != 19) throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 100) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 3) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 50) throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != 201) throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != 3) throw new Exception("m_MID1PAD_9");
                if (_MID1PAD_10 != 172) throw new Exception("m_MID1PAD_10");
                if (_MID1PAD_11 != 230) throw new Exception("m_MID1PAD_11");
                if (_MID1PAD_12 != 214) throw new Exception("m_MID1PAD_12");
                if (_MID1PAD_13 != 'z') throw new Exception("m_MID1PAD_13");
                if (_MID1PAD_14 != ';') throw new Exception("m_MID1PAD_14");
                if (_MID2PAD_0 != 153) throw new Exception("m_MID2PAD_0");
                if (_AFTERPAD_0 != 84) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 74) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != "30023") throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != "31182") throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != "31631") throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != 8) throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 17) throw new Exception("m_AFTERPAD_6");
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
