// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        static AA[] m_axForward3;
        static void GoToEnd() { throw new Exception(); }

        public static int Main()
        {
            bool param1 = false;
            bool[] local2 = new bool[7];
            float local3 = -40.0f;
            try
            {
                while (500.20f <= local3 + local3)
                {
                    GC.Collect();
                    AA.m_axForward3 = new AA[7];
                }
                do
                {
                    AA aa;
                    for (aa = new AA(); true; local2 = local2)
                    {
                        GC.Collect();
                        GoToEnd();
                    }
                } while (local2[2]);

                do
                {

                } while (true);
                GC.Collect();
            }
            catch (Exception)
            {
            }
            return 100;
        }
    }
}
