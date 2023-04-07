// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct AA
    {
        static Array m_a;
        static bool[] m_ab;
        static object m_x;

        static int Main1()
        {
            if (m_ab[190])
            {
                object L = (object)(double[])m_x;
                int[] L3 = new int[0x7fffffff];
                try
                {
                    if (m_a == (String[])L)
                        return L3[0x7fffffff];
                }
                catch (Exception) { }
                bool b = (bool)L;
            }
            return 0;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                return Main1();
            }
            catch (NullReferenceException)
            {
                return 100;
            }
        }
    }
}
