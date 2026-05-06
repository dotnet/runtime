// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit3_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private byte _BASEPAD_0;
            private ulong _BASEPAD_1;
            private char _BASEPAD_2;
            private int _BASEPAD_3;
            private char _BASEPAD_4;
            private uint _BASEPAD_5;
            private byte _BASEPAD_6;
            private ushort _BASEPAD_7;
            private byte _BASEPAD_8;
            private ulong _BASEPAD_9;
            private char _BASEPAD_10;
            private ulong _BASEPAD_11;
            private ushort _BASEPAD_12;
            private ushort _BASEPAD_13;
            private byte _BASEPAD_14;
            private uint _BASEPAD_15;
            private int _BASEPAD_16;

            public BaseNode()
            {
                _BASEPAD_0 = 104;
                _BASEPAD_1 = 181;
                _BASEPAD_2 = '@';
                _BASEPAD_3 = 187;
                _BASEPAD_4 = '|';
                _BASEPAD_5 = 142;
                _BASEPAD_6 = 225;
                _BASEPAD_7 = 33;
                _BASEPAD_8 = 254;
                _BASEPAD_9 = 177;
                _BASEPAD_10 = '}';
                _BASEPAD_11 = 251;
                _BASEPAD_12 = 151;
                _BASEPAD_13 = 171;
                _BASEPAD_14 = 13;
                _BASEPAD_15 = 23;
                _BASEPAD_16 = 116;
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 104) throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != 181) throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != '@') throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != 187) throw new Exception("m_BASEPAD_3");
                if (_BASEPAD_4 != '|') throw new Exception("m_BASEPAD_4");
                if (_BASEPAD_5 != 142) throw new Exception("m_BASEPAD_5");
                if (_BASEPAD_6 != 225) throw new Exception("m_BASEPAD_6");
                if (_BASEPAD_7 != 33) throw new Exception("m_BASEPAD_7");
                if (_BASEPAD_8 != 254) throw new Exception("m_BASEPAD_8");
                if (_BASEPAD_9 != 177) throw new Exception("m_BASEPAD_9");
                if (_BASEPAD_10 != '}') throw new Exception("m_BASEPAD_10");
                if (_BASEPAD_11 != 251) throw new Exception("m_BASEPAD_11");
                if (_BASEPAD_12 != 151) throw new Exception("m_BASEPAD_12");
                if (_BASEPAD_13 != 171) throw new Exception("m_BASEPAD_13");
                if (_BASEPAD_14 != 13) throw new Exception("m_BASEPAD_14");
                if (_BASEPAD_15 != 23) throw new Exception("m_BASEPAD_15");
                if (_BASEPAD_16 != 116) throw new Exception("m_BASEPAD_16");
            }
        }

        private class Node : BaseNode
        {
            private String _PREPAD_0;
            private ulong _PREPAD_1;
            private byte _PREPAD_2;
            private String _PREPAD_3;
            private ushort _PREPAD_4;
            private int _PREPAD_5;
            private uint _PREPAD_6;
            private int _PREPAD_7;
            private ulong _PREPAD_8;
            private byte _PREPAD_9;
            private byte _PREPAD_10;
            private byte _PREPAD_11;
            private ulong _PREPAD_12;
            private char _PREPAD_13;
            private int _PREPAD_14;
            public Node m_leftChild;
            private ushort _MID1PAD_0;
            private byte _MID1PAD_1;
            private char _MID1PAD_2;
            private ushort _MID1PAD_3;
            private ulong _MID1PAD_4;
            private uint _MID1PAD_5;
            private ushort _MID1PAD_6;
            private byte _MID1PAD_7;
            public int m_weight;
            private ulong _MID2PAD_0;
            private char _MID2PAD_1;
            private ushort _MID2PAD_2;
            private ulong _MID2PAD_3;
            private byte _MID2PAD_4;
            private ushort _MID2PAD_5;
            private int _MID2PAD_6;
            private uint _MID2PAD_7;
            private ulong _MID2PAD_8;
            private char _MID2PAD_9;
            private int _MID2PAD_10;
            private uint _MID2PAD_11;
            private char _MID2PAD_12;
            private ushort _MID2PAD_13;
            public Node m_rightChild;
            private uint _AFTERPAD_0;
            private ushort _AFTERPAD_1;
            private char _AFTERPAD_2;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = "3928";
                _PREPAD_1 = 111;
                _PREPAD_2 = 35;
                _PREPAD_3 = "27914";
                _PREPAD_4 = 158;
                _PREPAD_5 = 157;
                _PREPAD_6 = 55;
                _PREPAD_7 = 186;
                _PREPAD_8 = 161;
                _PREPAD_9 = 58;
                _PREPAD_10 = 50;
                _PREPAD_11 = 201;
                _PREPAD_12 = 137;
                _PREPAD_13 = 'e';
                _PREPAD_14 = 115;
                _MID1PAD_0 = 86;
                _MID1PAD_1 = 146;
                _MID1PAD_2 = 'o';
                _MID1PAD_3 = 76;
                _MID1PAD_4 = 215;
                _MID1PAD_5 = 206;
                _MID1PAD_6 = 230;
                _MID1PAD_7 = 232;
                _MID2PAD_0 = 204;
                _MID2PAD_1 = '1';
                _MID2PAD_2 = 27;
                _MID2PAD_3 = 217;
                _MID2PAD_4 = 220;
                _MID2PAD_5 = 123;
                _MID2PAD_6 = 85;
                _MID2PAD_7 = 142;
                _MID2PAD_8 = 63;
                _MID2PAD_9 = '+';
                _MID2PAD_10 = 40;
                _MID2PAD_11 = 235;
                _MID2PAD_12 = 'v';
                _MID2PAD_13 = 173;
                _AFTERPAD_0 = 160;
                _AFTERPAD_1 = 193;
                _AFTERPAD_2 = '>';
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != "3928") throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != 111) throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != 35) throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != "27914") throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 158) throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != 157) throw new Exception("m_PREPAD_5");
                if (_PREPAD_6 != 55) throw new Exception("m_PREPAD_6");
                if (_PREPAD_7 != 186) throw new Exception("m_PREPAD_7");
                if (_PREPAD_8 != 161) throw new Exception("m_PREPAD_8");
                if (_PREPAD_9 != 58) throw new Exception("m_PREPAD_9");
                if (_PREPAD_10 != 50) throw new Exception("m_PREPAD_10");
                if (_PREPAD_11 != 201) throw new Exception("m_PREPAD_11");
                if (_PREPAD_12 != 137) throw new Exception("m_PREPAD_12");
                if (_PREPAD_13 != 'e') throw new Exception("m_PREPAD_13");
                if (_PREPAD_14 != 115) throw new Exception("m_PREPAD_14");
                if (_MID1PAD_0 != 86) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 146) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != 'o') throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 76) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != 215) throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 206) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 230) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 232) throw new Exception("m_MID1PAD_7");
                if (_MID2PAD_0 != 204) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != '1') throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 27) throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 217) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != 220) throw new Exception("m_MID2PAD_4");
                if (_MID2PAD_5 != 123) throw new Exception("m_MID2PAD_5");
                if (_MID2PAD_6 != 85) throw new Exception("m_MID2PAD_6");
                if (_MID2PAD_7 != 142) throw new Exception("m_MID2PAD_7");
                if (_MID2PAD_8 != 63) throw new Exception("m_MID2PAD_8");
                if (_MID2PAD_9 != '+') throw new Exception("m_MID2PAD_9");
                if (_MID2PAD_10 != 40) throw new Exception("m_MID2PAD_10");
                if (_MID2PAD_11 != 235) throw new Exception("m_MID2PAD_11");
                if (_MID2PAD_12 != 'v') throw new Exception("m_MID2PAD_12");
                if (_MID2PAD_13 != 173) throw new Exception("m_MID2PAD_13");
                if (_AFTERPAD_0 != 160) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 193) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != '>') throw new Exception("m_AFTERPAD_2");
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
