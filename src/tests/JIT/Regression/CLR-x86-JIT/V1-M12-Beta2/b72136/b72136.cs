// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        public static object m_xStatic1 = null;
        public void Method1(ref byte param1) { }
    }

    public struct BB
    {
        public void Method1(float[] param5) { }
    }

    public class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                new AA().Method1(
                    ref new byte[] { 73 }[(new byte[16])[0] & 1]);
            }
            catch (Exception X) { }
            try
            {
                new BB().Method1(
                    new float[] { ((float[])AA.m_xStatic1)[0] }
                );
            }
            catch (Exception X) { }
            return 100;
        }
    }
}
