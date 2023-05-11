// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;
    public class AA
    {
        public int[] m_anField1 = new int[7];
        internal static void Method1()
        {
            AA[] local2 = new AA[7];
            while (true)
            {
                local2[2].m_anField1 = new AA().m_anField1;	//this will blow up
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception caught.");
            }
            return 100;
        }
    }
}
