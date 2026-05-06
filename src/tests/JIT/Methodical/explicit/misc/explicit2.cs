// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Rotate_explicit2_cs
{
    public class App
    {
        public static int s_weightCount = 1;

        private class BaseNode
        {
            private byte _BASEPAD_0;
            private char _BASEPAD_1;
            private uint _BASEPAD_2;
            private String _BASEPAD_3;
            private char _BASEPAD_4;
            private String _BASEPAD_5;

            public BaseNode()
            {
                _BASEPAD_0 = 63;
                _BASEPAD_1 = 'f';
                _BASEPAD_2 = 64;
                _BASEPAD_3 = "20808";
                _BASEPAD_4 = '*';
                _BASEPAD_5 = "11051";
            }

            public virtual void VerifyValid()
            {
                if (_BASEPAD_0 != 63) throw new Exception("m_BASEPAD_0");
                if (_BASEPAD_1 != 'f') throw new Exception("m_BASEPAD_1");
                if (_BASEPAD_2 != 64) throw new Exception("m_BASEPAD_2");
                if (_BASEPAD_3 != "20808") throw new Exception("m_BASEPAD_3");
                if (_BASEPAD_4 != '*') throw new Exception("m_BASEPAD_4");
                if (_BASEPAD_5 != "11051") throw new Exception("m_BASEPAD_5");
            }
        }

        private class Node : BaseNode
        {
            private uint _PREPAD_0;
            private String _PREPAD_1;
            private char _PREPAD_2;
            private byte _PREPAD_3;
            private char _PREPAD_4;
            private char _PREPAD_5;
            private char _PREPAD_6;
            private int _PREPAD_7;
            private int _PREPAD_8;
            public Node m_leftChild;
            private uint _MID1PAD_0;
            private int _MID1PAD_1;
            private int _MID1PAD_2;
            private String _MID1PAD_3;
            private String _MID1PAD_4;
            private uint _MID1PAD_5;
            private int _MID1PAD_6;
            private uint _MID1PAD_7;
            private String _MID1PAD_8;
            private String _MID1PAD_9;
            private ushort _MID1PAD_10;
            private uint _MID1PAD_11;
            private char _MID1PAD_12;
            private uint _MID1PAD_13;
            private String _MID1PAD_14;
            private int _MID1PAD_15;
            private char _MID1PAD_16;
            public int m_weight;
            private byte _MID2PAD_0;
            private char _MID2PAD_1;
            private ulong _MID2PAD_2;
            private ushort _MID2PAD_3;
            private String _MID2PAD_4;
            private String _MID2PAD_5;
            private byte _MID2PAD_6;
            private ushort _MID2PAD_7;
            private String _MID2PAD_8;
            private String _MID2PAD_9;
            private char _MID2PAD_10;
            private int _MID2PAD_11;
            private byte _MID2PAD_12;
            private String _MID2PAD_13;
            private char _MID2PAD_14;
            private ushort _MID2PAD_15;
            private String _MID2PAD_16;
            public Node m_rightChild;
            private String _AFTERPAD_0;
            private ushort _AFTERPAD_1;
            private uint _AFTERPAD_2;
            private String _AFTERPAD_3;
            private int _AFTERPAD_4;
            private char _AFTERPAD_5;
            private ulong _AFTERPAD_6;
            private byte _AFTERPAD_7;
            private byte _AFTERPAD_8;
            private String _AFTERPAD_9;
            private ushort _AFTERPAD_10;

            public Node()
            {
                m_weight = s_weightCount++;
                _PREPAD_0 = 33;
                _PREPAD_1 = "26819";
                _PREPAD_2 = 'l';
                _PREPAD_3 = 220;
                _PREPAD_4 = '^';
                _PREPAD_5 = '`';
                _PREPAD_6 = 'o';
                _PREPAD_7 = 162;
                _PREPAD_8 = 171;
                _MID1PAD_0 = 98;
                _MID1PAD_1 = 90;
                _MID1PAD_2 = 121;
                _MID1PAD_3 = "9109";
                _MID1PAD_4 = "6459";
                _MID1PAD_5 = 124;
                _MID1PAD_6 = 74;
                _MID1PAD_7 = 113;
                _MID1PAD_8 = "1720";
                _MID1PAD_9 = "15021";
                _MID1PAD_10 = 39;
                _MID1PAD_11 = 133;
                _MID1PAD_12 = 'N';
                _MID1PAD_13 = 235;
                _MID1PAD_14 = "22271";
                _MID1PAD_15 = 55;
                _MID1PAD_16 = 'G';
                _MID2PAD_0 = 173;
                _MID2PAD_1 = '';
                _MID2PAD_2 = 94;
                _MID2PAD_3 = 229;
                _MID2PAD_4 = "13459";
                _MID2PAD_5 = "8381";
                _MID2PAD_6 = 54;
                _MID2PAD_7 = 215;
                _MID2PAD_8 = "14415";
                _MID2PAD_9 = "30092";
                _MID2PAD_10 = 'S';
                _MID2PAD_11 = 250;
                _MID2PAD_12 = 247;
                _MID2PAD_13 = "3600";
                _MID2PAD_14 = 'k';
                _MID2PAD_15 = 229;
                _MID2PAD_16 = "18373";
                _AFTERPAD_0 = "18816";
                _AFTERPAD_1 = 98;
                _AFTERPAD_2 = 25;
                _AFTERPAD_3 = "3802";
                _AFTERPAD_4 = 217;
                _AFTERPAD_5 = '*';
                _AFTERPAD_6 = 140;
                _AFTERPAD_7 = 74;
                _AFTERPAD_8 = 91;
                _AFTERPAD_9 = "18469";
                _AFTERPAD_10 = 77;
            }

            public override void VerifyValid()
            {
                base.VerifyValid();
                if (_PREPAD_0 != 33) throw new Exception("m_PREPAD_0");
                if (_PREPAD_1 != "26819") throw new Exception("m_PREPAD_1");
                if (_PREPAD_2 != 'l') throw new Exception("m_PREPAD_2");
                if (_PREPAD_3 != 220) throw new Exception("m_PREPAD_3");
                if (_PREPAD_4 != '^') throw new Exception("m_PREPAD_4");
                if (_PREPAD_5 != '`') throw new Exception("m_PREPAD_5");
                if (_PREPAD_6 != 'o') throw new Exception("m_PREPAD_6");
                if (_PREPAD_7 != 162) throw new Exception("m_PREPAD_7");
                if (_PREPAD_8 != 171) throw new Exception("m_PREPAD_8");
                if (_MID1PAD_0 != 98) throw new Exception("m_MID1PAD_0");
                if (_MID1PAD_1 != 90) throw new Exception("m_MID1PAD_1");
                if (_MID1PAD_2 != 121) throw new Exception("m_MID1PAD_2");
                if (_MID1PAD_3 != "9109") throw new Exception("m_MID1PAD_3");
                if (_MID1PAD_4 != "6459") throw new Exception("m_MID1PAD_4");
                if (_MID1PAD_5 != 124) throw new Exception("m_MID1PAD_5");
                if (_MID1PAD_6 != 74) throw new Exception("m_MID1PAD_6");
                if (_MID1PAD_7 != 113) throw new Exception("m_MID1PAD_7");
                if (_MID1PAD_8 != "1720") throw new Exception("m_MID1PAD_8");
                if (_MID1PAD_9 != "15021") throw new Exception("m_MID1PAD_9");
                if (_MID1PAD_10 != 39) throw new Exception("m_MID1PAD_10");
                if (_MID1PAD_11 != 133) throw new Exception("m_MID1PAD_11");
                if (_MID1PAD_12 != 'N') throw new Exception("m_MID1PAD_12");
                if (_MID1PAD_13 != 235) throw new Exception("m_MID1PAD_13");
                if (_MID1PAD_14 != "22271") throw new Exception("m_MID1PAD_14");
                if (_MID1PAD_15 != 55) throw new Exception("m_MID1PAD_15");
                if (_MID1PAD_16 != 'G') throw new Exception("m_MID1PAD_16");
                if (_MID2PAD_0 != 173) throw new Exception("m_MID2PAD_0");
                if (_MID2PAD_1 != '') throw new Exception("m_MID2PAD_1");
                if (_MID2PAD_2 != 94) throw new Exception("m_MID2PAD_2");
                if (_MID2PAD_3 != 229) throw new Exception("m_MID2PAD_3");
                if (_MID2PAD_4 != "13459") throw new Exception("m_MID2PAD_4");
                if (_MID2PAD_5 != "8381") throw new Exception("m_MID2PAD_5");
                if (_MID2PAD_6 != 54) throw new Exception("m_MID2PAD_6");
                if (_MID2PAD_7 != 215) throw new Exception("m_MID2PAD_7");
                if (_MID2PAD_8 != "14415") throw new Exception("m_MID2PAD_8");
                if (_MID2PAD_9 != "30092") throw new Exception("m_MID2PAD_9");
                if (_MID2PAD_10 != 'S') throw new Exception("m_MID2PAD_10");
                if (_MID2PAD_11 != 250) throw new Exception("m_MID2PAD_11");
                if (_MID2PAD_12 != 247) throw new Exception("m_MID2PAD_12");
                if (_MID2PAD_13 != "3600") throw new Exception("m_MID2PAD_13");
                if (_MID2PAD_14 != 'k') throw new Exception("m_MID2PAD_14");
                if (_MID2PAD_15 != 229) throw new Exception("m_MID2PAD_15");
                if (_MID2PAD_16 != "18373") throw new Exception("m_MID2PAD_16");
                if (_AFTERPAD_0 != "18816") throw new Exception("m_AFTERPAD_0");
                if (_AFTERPAD_1 != 98) throw new Exception("m_AFTERPAD_1");
                if (_AFTERPAD_2 != 25) throw new Exception("m_AFTERPAD_2");
                if (_AFTERPAD_3 != "3802") throw new Exception("m_AFTERPAD_3");
                if (_AFTERPAD_4 != 217) throw new Exception("m_AFTERPAD_4");
                if (_AFTERPAD_5 != '*') throw new Exception("m_AFTERPAD_5");
                if (_AFTERPAD_6 != 140) throw new Exception("m_AFTERPAD_6");
                if (_AFTERPAD_7 != 74) throw new Exception("m_AFTERPAD_7");
                if (_AFTERPAD_8 != 91) throw new Exception("m_AFTERPAD_8");
                if (_AFTERPAD_9 != "18469") throw new Exception("m_AFTERPAD_9");
                if (_AFTERPAD_10 != 77) throw new Exception("m_AFTERPAD_10");
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
