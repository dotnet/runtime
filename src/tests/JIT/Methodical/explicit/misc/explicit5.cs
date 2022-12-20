// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit5_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            public BaseNode()
            {
            }

            public virtual void VerifyValid()
            {
            }
        }

        private class Node : BaseNode
        {
            private ulong _PREPAD_0;
            private char _PREPAD_1;
            private byte _PREPAD_2;
            private char _PREPAD_3;
            private uint _PREPAD_4;
            private int _PREPAD_5;
            private ulong _PREPAD_6;
            private ulong _PREPAD_7;
            private ulong _PREPAD_8;
            private uint _PREPAD_9;
            private ushort _PREPAD_10;
            private byte _PREPAD_11;
            private String _PREPAD_12;
            private char _PREPAD_13;
            private ushort _PREPAD_14;
            public Node m_leftChild;
            private int _MID1PAD_0;
            private ulong _MID1PAD_1;
            private String _MID1PAD_2;
            private ulong _MID1PAD_3;
            private String _MID1PAD_4;
            private char _MID1PAD_5;
            private String _MID1PAD_6;
            private uint _MID1PAD_7;
            private uint _MID1PAD_8;
            private uint _MID1PAD_9;
            private uint _MID1PAD_10;
            private ushort _MID1PAD_11;
            public int m_weight;
            private ushort _MID2PAD_0;
            private ulong _MID2PAD_1;
            private ushort _MID2PAD_2;
            private ulong _MID2PAD_3;
            private char _MID2PAD_4;
            public Node m_rightChild;
            private int _AFTERPAD_0;
            private ushort _AFTERPAD_1;
            private byte _AFTERPAD_2;
            private ushort _AFTERPAD_3;
            private int _AFTERPAD_4;
            private String _AFTERPAD_5;
            private uint _AFTERPAD_6;
            private char _AFTERPAD_7;
            private char _AFTERPAD_8;
            private ushort _AFTERPAD_9;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 49;
                _PREPAD_1 = 'R';
                _PREPAD_2 = 202;
                _PREPAD_3 = '_';
                _PREPAD_4 = 133;
                _PREPAD_5 = 51;
                _PREPAD_6 = 80;
                _PREPAD_7 = 250;
                _PREPAD_8 = 38;
                _PREPAD_9 = 20;
                _PREPAD_10 = 41;
                _PREPAD_11 = 202;
                _PREPAD_12 = "10482";
                _PREPAD_13 = '9';
                _PREPAD_14 = 37;
                _MID1PAD_0 = 81;
                _MID1PAD_1 = 28;
                _MID1PAD_2 = "13921";
                _MID1PAD_3 = 128;
                _MID1PAD_4 = "14428";
                _MID1PAD_5 = 'Z';
                _MID1PAD_6 = "702";
                _MID1PAD_7 = 94;
                _MID1PAD_8 = 198;
                _MID1PAD_9 = 179;
                _MID1PAD_10 = 31;
                _MID1PAD_11 = 47;
                _MID2PAD_0 = 141;
                _MID2PAD_1 = 22;
                _MID2PAD_2 = 214;
                _MID2PAD_3 = 135;
                _MID2PAD_4 = '$';
                _AFTERPAD_0 = 47;
                _AFTERPAD_1 = 237;
                _AFTERPAD_2 = 202;
                _AFTERPAD_3 = 177;
                _AFTERPAD_4 = 177;
                _AFTERPAD_5 = "28735";
                _AFTERPAD_6 = 97;
                _AFTERPAD_7 = '5';
                _AFTERPAD_8 = '=';
                _AFTERPAD_9 = 76;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 49) throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != 'R') throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != 202) throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != '_') throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 133) throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != 51) throw new Exception("m_PREPAD_5");
                if (_PREPAD_6 != 80) throw new Exception("m_PREPAD_6");
                if (_PREPAD_7 != 250) throw new Exception("m_PREPAD_7");
                if (_PREPAD_8 != 38) throw new Exception("m_PREPAD_8");
                if (_PREPAD_9 != 20) throw new Exception("m_PREPAD_9");
                if (_PREPAD_10 != 41) throw new Exception("m_PREPAD_10");
                if (_PREPAD_11 != 202) throw new Exception("m_PREPAD_11");
                if (_PREPAD_12 != "10482") throw new Exception("m_PREPAD_12");
                if (_PREPAD_13 != '9') throw new Exception("m_PREPAD_13");
                if (_PREPAD_14 != 37) throw new Exception("m_PREPAD_14");
                if (_MID1PAD_0 != 81) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 28) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != "13921") throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 128) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != "14428") throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 'Z') throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != "702") throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 94) throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != 198) throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != 179) throw new Exception("m_MID1PAD_9");
                if (_MID1PAD_10 != 31) throw new Exception("m_MID1PAD_10");
                if (_MID1PAD_11 != 47) throw new Exception("m_MID1PAD_11");
                if (_MID2PAD_0 != 141) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != 22) throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 214) throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 135) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != '$') throw new Exception("m_MID2PAD_4");
                if (_AFTERPAD_0 != 47) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 237) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != 202) throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != 177) throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != 177) throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != "28735") throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 97) throw new Exception("m_AFTERPAD_6");
                if (_AFTERPAD_7 != '5') throw new Exception("m_AFTERPAD_7");
                if (_AFTERPAD_8 != '=') throw new Exception("m_AFTERPAD_8");
                if (_AFTERPAD_9 != 76) throw new Exception("m_AFTERPAD_9");
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
