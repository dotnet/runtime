// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace DefaultNamespace
{
    public class Bug
    {
        public static readonly String s_strActiveBugNums = "";
        public static readonly String s_strDtTmVer = "";
        public static readonly String s_strClassMethod = "";
        public static readonly String s_strTFName = "";
        public static readonly String s_strTFAbbrev = "";
        public static readonly String s_strTFPath = "";


        internal static readonly Type[] ClassTypes = {
             Type.GetType("System.Empty"),
          Type.GetType("System.Void"),
          Type.GetType("System.Boolean"),
          Type.GetType("System.Char"),
          Type.GetType("System.SByte"),
          Type.GetType("System.Byte"),
          Type.GetType("System.Int16"),
          Type.GetType("System.UInt16"),
          Type.GetType("System.Int32"),
          Type.GetType("System.UInt32"),
          Type.GetType("System.Int64"),
          Type.GetType("System.UInt64"),
          Type.GetType("System.Single"),
          Type.GetType("System.Double"),
          Type.GetType("System.String"),
          Type.GetType("System.DateTime"),
          Type.GetType("System.TimeSpan"),
          Type.GetType("System.Decimal"),
          Type.GetType("System.Currency"),
          Type.GetType("System.Object"),
          Type.GetType("System.Missing"),
          Type.GetType("System.Null"),
          Type.GetType("System.Object"),
          Type.GetType("Simple"),
          Type.GetType("System.Empty[]"),
          Type.GetType("System.Boolean[]"),
          Type.GetType("System.Char[]"),
          Type.GetType("System.SByte[]"),
          Type.GetType("System.Byte[]"),
          Type.GetType("System.Int16[]"),
          Type.GetType("System.UInt16[]"),
          Type.GetType("System.Int32[]"),
          Type.GetType("System.UInt32[]"),
          Type.GetType("System.Int64[]"),
          Type.GetType("System.UInt64[]"),
          Type.GetType("System.Single[]"),
          Type.GetType("System.Double[]"),
          Type.GetType("System.String[]"),
          Type.GetType("System.DateTime[]"),
          Type.GetType("System.TimeSpan[]"),
          Type.GetType("System.Decimal[]"),
          Type.GetType("System.Currency[]"),
          Type.GetType("System.Object[]"),
          Type.GetType("System.Missing[]"),
          Type.GetType("System.Null[]"),
          Type.GetType("System.Object[]"),
          Type.GetType("Simple[]"),
          Type.GetType("System.Empty[][]"),
          Type.GetType("System.Boolean[][]"),
          Type.GetType("System.Char[][]"),
          Type.GetType("System.SByte[][]"),
          Type.GetType("System.Byte[][]"),
          Type.GetType("System.Int16[][]"),
          Type.GetType("System.UInt16[][]"),
          Type.GetType("System.Int32[][]"),
          Type.GetType("System.UInt32[][]"),
          Type.GetType("System.Int64[][]"),
          Type.GetType("System.UInt64[][]"),
          Type.GetType("System.Single[][]"),
          Type.GetType("System.Double[][]"),
          Type.GetType("System.String[][]"),
          Type.GetType("System.DateTime[][]"),
          Type.GetType("System.TimeSpan[][]"),
          Type.GetType("System.Decimal[][]"),
          Type.GetType("System.Currency[][]"),
          Type.GetType("System.Object[][]"),
          Type.GetType("System.Missing[][]"),
          Type.GetType("System.Null[][]"),
          Type.GetType("System.Object[][]"),
          Type.GetType("Simple[][]"),
          Type.GetType("System.Empty[][][]"),
          Type.GetType("System.Boolean[][][]"),
          Type.GetType("System.Char[][][]"),
          Type.GetType("System.SByte[][][]"),
          Type.GetType("System.Byte[][][]"),
          Type.GetType("System.Int16[][][]"),
          Type.GetType("System.UInt16[][][]"),
          Type.GetType("System.Int32[][][]"),
          Type.GetType("System.UInt32[][][]"),
          Type.GetType("System.Int64[][][]"),
          Type.GetType("System.UInt64[][][]"),
          Type.GetType("System.Single[][][]"),
          Type.GetType("System.Double[][][]"),
          Type.GetType("System.String[][][]"),
          Type.GetType("System.DateTime[][][]"),
          Type.GetType("System.TimeSpan[][][]"),
          Type.GetType("System.Decimal[][][]"),
          Type.GetType("System.Currency[][][]"),
          Type.GetType("System.Object[][][]"),
          Type.GetType("System.Missing[][][]"),
          Type.GetType("System.Null[][][]"),
          Type.GetType("System.Object[][][]"),
          Type.GetType("Simple[][][]"),
    };

        internal static readonly Boolean[,] bArr = {{true, true, true, true, true, false, false, false, false, false},
                                                                             {true, true, true, true, true, false, false, false, false, false}};
        internal static readonly Char[,] cArr = {{'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j'},
                                                                             {'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't'}};
        internal static readonly SByte[,] sbtArr = {{SByte.MinValue, (SByte)(-100), (SByte)(-5), (SByte)0, (SByte)5, (SByte)100, SByte.MaxValue},
                                                                             {SByte.MinValue, (SByte)(-100), (SByte)(-5), (SByte)0, (SByte)5, (SByte)100, SByte.MaxValue}};
        internal static readonly Byte[,] btArr = {{Byte.MinValue, 0, 5, 100, Byte.MaxValue},
                                                                             {Byte.MinValue, 0, 5, 100, Byte.MaxValue}};
        internal static readonly Int16[,] i16Arr = {{19, 238, 317, 6, 565, 0, -52, 60, -563, 753},
                                                                             {19, 238, 317, 6, 565, 0, -52, 60, -563, 753}};
        internal static readonly Int32[,] i32Arr = {{19, 238, 317, 6, 565, 0, -52, 60, -563, 753},
                                                                             {19, 238, 317, 6, 565, 0, -52, 60, -563, 753}};
        internal static readonly Int64[,] i64Arr = {{-530, Int64.MinValue, Int32.MinValue, Int16.MinValue, -127, 0, Int64.MaxValue, Int32.MaxValue, Int16.MaxValue, 0},
                                                                             {-530, Int64.MinValue, Int32.MinValue, Int16.MinValue, -127, 0, Int64.MaxValue, Int32.MaxValue, Int16.MaxValue, 0}};
        internal static readonly Single[,] fArr = {{-1.2e23f, 1.2e-32f, -1.23f, 0.0f, -1.0f, -1.2e23f, 1.2e-32f, -1.23f, 0.0f, -1.0f},
                                                                             {1.23e23f, 1.23f, 0.0f, 2.45f, 35.0f, 1.23e23f, 1.23f, 0.0f, 2.45f, 35.0f}};
        internal static readonly Double[,] dArr = {{-1.2e23, 1.2e-32, -1.23, 0.0, 56.0, -1.2e23, 1.2e-32, -1.23, 0.0, 56.0,},
                                                                             {1.23e23, 1.23, 0.0, 2.45, 635.0, -1.2e23, 1.2e-32, -1.23, 0.0, 56.0}};
        internal static readonly String[,] strArr = {{"This", " ", "a", " ", "test", " ", "of", " ", "patience", "."},
                                                                             {"This", " ", "a", " ", "test", " ", "of", " ", "patience", "."}};

        [Fact]
        public static void TestEntryPoint()
        {
            new Bug();
        }
    }

    internal class Simple
    {
        internal Simple() { m_oObject = ("Hello World"); }
        internal Object m_oObject;
    }
}
