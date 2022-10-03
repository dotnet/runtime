// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit4_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private byte _BASEPAD_0;
            private char _BASEPAD_1;
            private ulong _BASEPAD_2;
            private char _BASEPAD_3;

            public BaseNode()
            {
                _BASEPAD_0 = 248;
                _BASEPAD_1 = '*';
                _BASEPAD_2 = 17;
                _BASEPAD_3 = 'Z';
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 248) throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != '*') throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != 17) throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != 'Z') throw new Exception("m_BASEPAD_3");
            }
        }

        private class Node : BaseNode
        {
            private char _PREPAD_0;
            private String _PREPAD_1;
            private ulong _PREPAD_2;
            private byte _PREPAD_3;
            private ulong _PREPAD_4;
            private int _PREPAD_5;
            public Node m_leftChild;
            private ulong _MID1PAD_0;
            private int _MID1PAD_1;
            private char _MID1PAD_2;
            private uint _MID1PAD_3;
            private byte _MID1PAD_4;
            private uint _MID1PAD_5;
            private ushort _MID1PAD_6;
            private int _MID1PAD_7;
            private ushort _MID1PAD_8;
            private ushort _MID1PAD_9;
            public int m_weight;
            public Node m_rightChild;
            private ushort _AFTERPAD_0;
            private uint _AFTERPAD_1;
            private uint _AFTERPAD_2;
            private int _AFTERPAD_3;
            private String _AFTERPAD_4;
            private char _AFTERPAD_5;
            private ushort _AFTERPAD_6;
            private int _AFTERPAD_7;
            private uint _AFTERPAD_8;
            private ulong _AFTERPAD_9;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 'p';
                _PREPAD_1 = "24797";
                _PREPAD_2 = 182;
                _PREPAD_3 = 95;
                _PREPAD_4 = 40;
                _PREPAD_5 = 60;
                _MID1PAD_0 = 17;
                _MID1PAD_1 = 16;
                _MID1PAD_2 = '=';
                _MID1PAD_3 = 127;
                _MID1PAD_4 = 237;
                _MID1PAD_5 = 248;
                _MID1PAD_6 = 61;
                _MID1PAD_7 = 22;
                _MID1PAD_8 = 48;
                _MID1PAD_9 = 157;
                _AFTERPAD_0 = 173;
                _AFTERPAD_1 = 81;
                _AFTERPAD_2 = 60;
                _AFTERPAD_3 = 132;
                _AFTERPAD_4 = "22723";
                _AFTERPAD_5 = 'm';
                _AFTERPAD_6 = 54;
                _AFTERPAD_7 = 229;
                _AFTERPAD_8 = 58;
                _AFTERPAD_9 = 165;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 'p') throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != "24797") throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != 182) throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != 95) throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != 40) throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != 60) throw new Exception("m_PREPAD_5");
                if (_MID1PAD_0 != 17) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 16) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != '=') throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != 127) throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != 237) throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 248) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 61) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 22) throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != 48) throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != 157) throw new Exception("m_MID1PAD_9");
                if (_AFTERPAD_0 != 173) throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 81) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != 60) throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != 132) throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != "22723") throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != 'm') throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 54) throw new Exception("m_AFTERPAD_6");
                if (_AFTERPAD_7 != 229) throw new Exception("m_AFTERPAD_7");
                if (_AFTERPAD_8 != 58) throw new Exception("m_AFTERPAD_8");
                if (_AFTERPAD_9 != 165) throw new Exception("m_AFTERPAD_9");
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
