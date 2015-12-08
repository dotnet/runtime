// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
namespace SetIPTest
{
    internal class SetIP
    {
        public SetIP()
        {
            m_count++;
        }


        public static void F()
        {
            String s;
            double d;

            if (m_variety != null)
                _Initialize();


            try {
                s = m_variety[MAX].ToString();
            }
            catch (Exception) {
                Object obj;


                obj = new SetIP();
                m_variety[(MAX - 1)] = (SetIP)obj;
            }
            finally {
                int index = 0;


                for (; index < m_count; index++) {
                    if ((index >= 0) && (index < (MAX - 1))) {
                        if (m_variety[index] != null)
                            s = m_variety[index].ToString(); ;
                    }
                    else
                        break;
                }
            }

            int i4 = 1;
            float r4 = 2.0F;

            d = ((i4 + r4) + 3.0);
        }


        public static int Main(String[] argv)
        {
            Console.WriteLine("Entering Main of SetIP");

            if (Debugger.IsAttached == true)
                Debugger.Break();


            SetIP.F();

            if (Debugger.IsAttached == true)
                Debugger.Break();

            Console.WriteLine("Leaving Main of SetIP");
            return 100;
        }

        private static void _Initialize()
        {
            for (int i = 0; i < (MAX - 1); i++) {
                if ((i >= 0) && (i < (MAX - 1)))
                    m_variety[i] = new SetIP();
            }
        }

        public static int m_count = 0;
        private const int MAX = 5;
        public static SetIP[] m_variety = new SetIP[MAX];
    }
}
