// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

    class App
    {
        static int Main()
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
