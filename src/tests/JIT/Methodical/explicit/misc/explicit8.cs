// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit8_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private byte _BASEPAD_0;
            private ulong _BASEPAD_1;
            private int _BASEPAD_2;
            private ulong _BASEPAD_3;
            private String _BASEPAD_4;
            private byte _BASEPAD_5;
            private String _BASEPAD_6;
            private uint _BASEPAD_7;
            private ushort _BASEPAD_8;
            private byte _BASEPAD_9;
            private String _BASEPAD_10;
            private int _BASEPAD_11;
            private int _BASEPAD_12;

            public BaseNode()
            {
                _BASEPAD_0 = 124;
                _BASEPAD_1 = 42;
                _BASEPAD_2 = 114;
                _BASEPAD_3 = 8;
                _BASEPAD_4 = "8319";
                _BASEPAD_5 = 207;
                _BASEPAD_6 = "26397";
                _BASEPAD_7 = 207;
                _BASEPAD_8 = 46;
                _BASEPAD_9 = 35;
                _BASEPAD_10 = "16085";
                _BASEPAD_11 = 44;
                _BASEPAD_12 = 138;
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 124) throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != 42) throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != 114) throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != 8) throw new Exception("m_BASEPAD_3");
                if (_BASEPAD_4 != "8319") throw new Exception("m_BASEPAD_4");
                if (_BASEPAD_5 != 207) throw new Exception("m_BASEPAD_5");
                if (_BASEPAD_6 != "26397") throw new Exception("m_BASEPAD_6");
                if (_BASEPAD_7 != 207) throw new Exception("m_BASEPAD_7");
                if (_BASEPAD_8 != 46) throw new Exception("m_BASEPAD_8");
                if (_BASEPAD_9 != 35) throw new Exception("m_BASEPAD_9");
                if (_BASEPAD_10 != "16085") throw new Exception("m_BASEPAD_10");
                if (_BASEPAD_11 != 44) throw new Exception("m_BASEPAD_11");
                if (_BASEPAD_12 != 138) throw new Exception("m_BASEPAD_12");
            }
        }

        private class Node : BaseNode
        {
            private int _PREPAD_0;
            private uint _PREPAD_1;
            private char _PREPAD_2;
            private uint _PREPAD_3;
            private ushort _PREPAD_4;
            public Node m_leftChild;
            private ulong _MID1PAD_0;
            private uint _MID1PAD_1;
            private byte _MID1PAD_2;
            private int _MID1PAD_3;
            private char _MID1PAD_4;
            private ushort _MID1PAD_5;
            private ushort _MID1PAD_6;
            private int _MID1PAD_7;
            private String _MID1PAD_8;
            private byte _MID1PAD_9;
            private int _MID1PAD_10;
            private String _MID1PAD_11;
            private uint _MID1PAD_12;
            private ulong _MID1PAD_13;
            private uint _MID1PAD_14;
            private String _MID1PAD_15;
            public int m_weight;
            private uint _MID2PAD_0;
            private String _MID2PAD_1;
            private uint _MID2PAD_2;
            private byte _MID2PAD_3;
            private char _MID2PAD_4;
            private ulong _MID2PAD_5;
            private byte _MID2PAD_6;
            private ulong _MID2PAD_7;
            private ushort _MID2PAD_8;
            private byte _MID2PAD_9;
            private int _MID2PAD_10;
            private int _MID2PAD_11;
            private String _MID2PAD_12;
            private String _MID2PAD_13;
            private int _MID2PAD_14;
            private char _MID2PAD_15;
            public Node m_rightChild;
            private ulong _AFTERPAD_0;
            private int _AFTERPAD_1;
            private ulong _AFTERPAD_2;
            private int _AFTERPAD_3;
            private ulong _AFTERPAD_4;
            private ulong _AFTERPAD_5;
            private ulong _AFTERPAD_6;
            private ulong _AFTERPAD_7;
            private String _AFTERPAD_8;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 219;
                _PREPAD_1 = 230;
                _PREPAD_2 = '`';
                _PREPAD_3 = 33;
                _PREPAD_4 = 67;
                _MID1PAD_0 = 50;
                _MID1PAD_1 = 44;
                _MID1PAD_2 = 152;
                _MID1PAD_3 = 168;
                _MID1PAD_4 = '{';
                _MID1PAD_5 = 202;
                _MID1PAD_6 = 251;
                _MID1PAD_7 = 135;
                _MID1PAD_8 = "28824";
                _MID1PAD_9 = 201;
                _MID1PAD_10 = 106;
                _MID1PAD_11 = "12481";
                _MID1PAD_12 = 83;
                _MID1PAD_13 = 127;
                _MID1PAD_14 = 243;
                _MID1PAD_15 = "28096";
                _MID2PAD_0 = 107;
                _MID2PAD_1 = "22265";
                _MID2PAD_2 = 178;
                _MID2PAD_3 = 73;
                _MID2PAD_4 = 'A';
                _MID2PAD_5 = 40;
                _MID2PAD_6 = 3;
                _MID2PAD_7 = 18;
                _MID2PAD_8 = 97;
                _MID2PAD_9 = 194;
                _MID2PAD_10 = 30;
                _MID2PAD_11 = 62;
                _MID2PAD_12 = "11775";
                _MID2PAD_13 = "19219";
                _MID2PAD_14 = 176;
                _MID2PAD_15 = 'b';
                _AFTERPAD_0 = 56;
                _AFTERPAD_1 = 249;
                _AFTERPAD_2 = 153;
                _AFTERPAD_3 = 67;
                _AFTERPAD_4 = 52;
                _AFTERPAD_5 = 232;
                _AFTERPAD_6 = 164;
                _AFTERPAD_7 = 111;
                _AFTERPAD_8 = "25014";
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 219) throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != 230) throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != '`') throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != 33) throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 67) throw new Exception("m_PREPAD_4");
                if (_MID1PAD_0 != 50) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 44) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != 152) throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 168) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != '{') throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 202) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 251) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 135) throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != "28824") throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != 201) throw new Exception("m_MID1PAD_9");
                if (_MID1PAD_10 != 106) throw new Exception("m_MID1PAD_10");
                if (_MID1PAD_11 != "12481") throw new Exception("m_MID1PAD_11");
                if (_MID1PAD_12 != 83) throw new Exception("m_MID1PAD_12");
                if (_MID1PAD_13 != 127) throw new Exception("m_MID1PAD_13");
                if (_MID1PAD_14 != 243) throw new Exception("m_MID1PAD_14");
                if (_MID1PAD_15 != "28096") throw new Exception("m_MID1PAD_15");
                if (_MID2PAD_0 != 107) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != "22265") throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 178) throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 73) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != 'A') throw new Exception("m_MID2PAD_4");
                if (_MID2PAD_5 != 40) throw new Exception("m_MID2PAD_5");
                if (_MID2PAD_6 != 3) throw new Exception("m_MID2PAD_6");
                if (_MID2PAD_7 != 18) throw new Exception("m_MID2PAD_7");
                if (_MID2PAD_8 != 97) throw new Exception("m_MID2PAD_8");
                if (_MID2PAD_9 != 194) throw new Exception("m_MID2PAD_9");
                if (_MID2PAD_10 != 30) throw new Exception("m_MID2PAD_10");
                if (_MID2PAD_11 != 62) throw new Exception("m_MID2PAD_11");
                if (_MID2PAD_12 != "11775") throw new Exception("m_MID2PAD_12");
                if (_MID2PAD_13 != "19219") throw new Exception("m_MID2PAD_13");
                if (_MID2PAD_14 != 176) throw new Exception("m_MID2PAD_14");
                if (_MID2PAD_15 != 'b') throw new Exception("m_MID2PAD_15");
                if (_AFTERPAD_0 != 56) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 249) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != 153) throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != 67) throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != 52) throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != 232) throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 164) throw new Exception("m_AFTERPAD_6");
                if (_AFTERPAD_7 != 111) throw new Exception("m_AFTERPAD_7");
                if (_AFTERPAD_8 != "25014") throw new Exception("m_AFTERPAD_8");
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
