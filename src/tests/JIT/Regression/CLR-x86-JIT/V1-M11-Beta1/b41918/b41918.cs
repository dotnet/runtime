// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
    {
        public static ulong m_ulStatic1 = 237u;
        [Fact]
        public static int TestEntryPoint()
        {
            try { }
            finally
            {
                try
                {
#pragma warning disable 1718
                    while (BB.m_ulStatic1 < BB.m_ulStatic1) { }
#pragma warning restore
                }
                catch (Exception) { }
            }
            return 100;
        }
    }
}
