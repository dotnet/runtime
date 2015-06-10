// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        public bool m_bField1 = false;
        public static double m_dStatic2 = -127.46;

        public int Method1()
        {
            double[] local4 = new double[2];
            double local3 = 35.40;

            while (m_bField1)
                return 0;

            do
            {
                do
                {
                    if (local3 < 0.0)
                        GC.Collect();

                    m_dStatic2 = local4[2];		//fire IndexOutOfRangeException	

                } while (new AA().m_bField1);

                while (m_bField1) { }

            } while (new AA().m_bField1);

            do
            {
            } while (0.0 <= local4[100]);		//fire IndexOutOfRangeException	

            return 1;
        }

        static int Main()
        {
            try
            {
                Console.WriteLine("Testing AA::Method1");
                new AA().Method1();
            }
            catch (Exception)
            {
                Console.WriteLine("Exception handled.");
            }
            return 100;
        }
    }
}
