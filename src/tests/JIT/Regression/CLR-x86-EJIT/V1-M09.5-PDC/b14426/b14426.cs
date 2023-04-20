// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Xunit;
namespace SetIPTest
{
    public class SetIP
    {
        public SetIP()
        {
            m_count++;
        }


        internal static void F()
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
                            s = m_variety[index].ToString();
                    }
                    else
                        break;
                }
            }

            int i4 = 1;
            float r4 = 2.0F;

            d = ((i4 + r4) + 3.0);
        }


        [Fact]
        public static int TestEntryPoint()
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
