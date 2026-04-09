// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;

    public class StringCreator
    {
        internal String m_String;
        internal String[] m_aString, m_aString1;
        internal const int MAX_LENGTH = 2000;

        public static int Main(String [] Args)
        {
            int iNofObjects = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            if (Args.Length==1)
            {
                if (!Int32.TryParse( Args[0], out iNofObjects ))
                {
                    iNofObjects = 2;
                }
            }
            else
            {
                iNofObjects = 2;
            }


            RunTest(iNofObjects);

            return 100;
        }

        public static void RunTest(int iNofObjects)
        {
            for (int i = 0; i < iNofObjects; i++)
            {
                StringCreator sc = new StringCreator();
                int slicer = 0;
                String str;
                sc.CreateString();
                do
                {
                    slicer = sc.SplitString(slicer);
                    str = sc.RotateStrings();
                } while(String.Compare(String.Empty, "") != 0);

                Console.WriteLine("\nslicer = {0}", slicer);
            }
        }

        public void CreateString()
        {
            m_String = String.Empty;
            Console.WriteLine("Creating Strings..");
            for (int i = 0; i < MAX_LENGTH; i++)
            {
                if ( i%100 == 0)
                {
                    Console.WriteLine("Created Strings: {0} : {1}", i, GC.GetTotalMemory(false));
                }
                m_String =  m_String + Convert.ToString(i);
            }
        }


        public int SplitString(int slicer)
        {
            char [] Sep = new char[1];
            Sep[0] = (slicer.ToString())[0];
            m_aString = m_String.Split(Sep);
            slicer += 1;
            if( slicer >= 10 )
            {
                slicer -=10;
            }
            return slicer;
        }


        public String RotateStrings()
        {
            m_String = String.Empty;
            m_aString1 = new String[m_aString.Length];
            Console.WriteLine("Creating More Strings..");
            for (int i = 0; i < m_aString.Length; i++)
            {
                if (i%100 == 0)
                {
                    Console.WriteLine("Created Strings: {0} : {1}", i, GC.GetTotalMemory(false));
                }
                m_aString1[i] = (m_aString[(m_aString.Length - 1) - i]);
                m_String = m_String + m_aString[i];
                m_aString1[i] = m_aString1[i % m_aString.Length] + m_aString1[i % m_aString.Length];
            }

            return m_String;
        }
    }
}
