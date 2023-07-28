// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;
    public class BB
    {
        public static uint m_uStatic3 = 205u;

        internal static void Static1()
        {
            try
            {
                GC.Collect();
            }
            finally
            {
#pragma warning disable 1718
                while (m_uStatic3 == m_uStatic3)
#pragma warning restore
                {
                    throw new Exception();
                }
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Static1();
            }
            catch (Exception) { return 100; }
            return 1;
        }
    }
}
