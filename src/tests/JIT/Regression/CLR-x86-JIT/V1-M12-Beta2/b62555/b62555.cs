// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct AA
    {
        public static byte[] m_abStatic1;
    }

    public struct DD
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return 101;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
        public static void Main1()
        {
            do
            {
                byte local1 = AA.m_abStatic1[0];
                DD local12 = ((DD)((new object[1])[0] = new object[] { null }[0]));
                DD[] local4 = (new DD[((new byte[1])[0] & ((sbyte)local1))]);
                try
                {
                    continue;
                }
                catch (DivideByZeroException)
                {
                    if (((bool)(new object[] { null }[0] = ((object)(new uint[53])))))
                        throw new IndexOutOfRangeException();
                }
                float f = ((float[])((object)new AA()))[local1];
            } while (true);
        }
    }
}
