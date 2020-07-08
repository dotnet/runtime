// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/**
 * Description:
 *  A Test which tests the GC using Variants. It is a test which creates a bunch of Variant Objects
 *  with mainly INT datatype.
*/

namespace DefaultNamespace {
    using System;

    internal class VariantInt
    {

        internal Object [] m_aVar;
        internal Object [] m_aVar1;

        public static int Main(String [] Args)
        {
            int iRep = 0;
            int iObj = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            if (Args.Length==2)
            {
                if (!Int32.TryParse( Args[0], out iRep ) ||
                    !Int32.TryParse( Args[0], out iObj ))
                {
                    iRep = 20;
                    iObj = 100;
                }
            }
            else
            {
                iRep = 20;
                iObj = 100;
            }

            VariantInt Mv_Obj = new VariantInt();
            if(Mv_Obj.runTest(iRep, iObj))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool runTest(int iRep, int iObj)
        {
            for(int i = 0; i < iRep; i++)
            {
                m_aVar = new Object[iObj];
                for(int j = 0; j < iObj; j++)
                {
                    if(j%2 == 1)
                    {
                        m_aVar[j] = i;
                    }
                    else
                    {
                        m_aVar[j] = null;
                    }
                }
                MakeLeak(iRep, iObj);
                Console.WriteLine(i);
            }
            return true;
        }


        public void MakeLeak(int iRep, int iObj)
        {
            Object [] L_Vart1 = new Object[iObj];
            Object [] L_Vart2;
            m_aVar1 = new Object[iObj];
            for(int i = 0; i < iRep; i++)
            {
                L_Vart2 = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    L_Vart1[j] = m_aVar[j];
                    L_Vart2[j] = (m_aVar[j]);
                    m_aVar1[j] = (new int[10]);
                    m_aVar1[j] = (m_aVar[j]);
                }
            }
            m_aVar1 = null;
        }
    }
}
