// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        public byte[] Method1()
        {
            while (m_bFwd1)
            {
                while (m_bFwd1)
                {
                    try
                    {
                        throw new Exception();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return m_abFwd6;
                    }
                }
            }
            return m_abFwd6;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            new App().Method1();
            return 100;
        }
        public static bool m_bFwd1;
        public static byte[] m_abFwd6;
    }
}
