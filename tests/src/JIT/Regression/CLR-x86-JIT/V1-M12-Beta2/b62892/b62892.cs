// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
