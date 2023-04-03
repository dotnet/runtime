// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static uint[] m_au = new uint[2];
        static void Main1()
        {
            int D = 18;
            do
            {
                m_au[0] = 0;
            } while (D == 0);
            throw new Exception();
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (Exception) { return 100; }
            return -1;
        }
    }
}
