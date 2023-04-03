// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        static uint[] m_au = new uint[10];
        static void Method1(uint param1) { }
        [Fact]
        public static int TestEntryPoint()
        {
            int a = 98;
            try
            {
#pragma warning disable 1718
                if (a < a)
                {
#pragma warning restore 1718
                    try
                    {
                        GC.Collect();
                    }
                    catch (Exception) { }
                }
                Method1(m_au[0]);
            }
            catch (Exception) { }
            return 100;
        }
    }
}
