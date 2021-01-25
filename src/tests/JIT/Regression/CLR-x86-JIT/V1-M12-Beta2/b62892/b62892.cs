// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace Test
{
    using System;

    public struct AA
    {
        public static float[] Static1()
        {
            CC.m_xStatic3 = null;
            return null;
        }
    }

    public class BB
    {
        static int Main()
        {
            AA.Static1();
            return 100;
        }
    }

    public class CC
    {
        public static float[] m_afStatic1 = AA.Static1();
        public static BB m_xStatic3 = null;
    }
}
