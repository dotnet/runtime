// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************/
/*Test:     NDPinFinal
/*Purpose:  check if GC works fine with PInvoke Pinned Object APIs
/*coverage: GetPinnedHandle(); FreePinnedHandle(); GetPinnedObject()
/*          Finalize
/******************************************************************/

namespace DefaultNamespace {

    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;


    internal class NDPinFinal
    {
        internal Object p;
        internal GCHandle handle;

        internal NDPinFinal (Object p, GCHandle h)
        {
            this.p = p;
            handle = h;
            NDPinFinal.cCreatObj++;
        }

        ~NDPinFinal()
        {
            if (handle.IsAllocated)
            {
                NDPinFinal.pinList[cFinalObj] = handle.Target;
            }
            handle.Free();
            NDPinFinal.cFinalObj++;
        }

        internal static NDPinFinal m_n;
        internal static Object m_o;
        internal static Object[] pinList = null;
        internal static int cFinalObj = 0;
        internal static int cCreatObj = 0;


        public static void CreateObj(int iObj) {

            pinList = new Object[iObj];
            m_o = new int[100];
            for (int i = 0; i < iObj; i++)
            {
                m_o = new int[100];
                m_n = new NDPinFinal (m_o, GCHandle.Alloc(m_o, GCHandleType.Pinned));
            }
        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void RemoveN()
        {
            m_n = null;
        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void RemovePinList(int iObj)
        {
            for ( int i=0; i< iObj; i++ )
            {
                pinList[i] = null;
            }
        }


        public static bool RunTest(int iObj)
        {

            GC.Collect();
            if (m_o != m_n.p)
            {
                return false;
            }

            RemoveN();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            RemovePinList(iObj);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
           
            if( cFinalObj == cCreatObj )
            {
                return true;
            }
            else
            {
                Console.Write(cCreatObj-cFinalObj);
                Console.WriteLine (" objects have been finalized!" );
                return false;
            }

        }

        public static int Main( String [] args )
        {
            int iObj = 1000;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            if( args.Length >= 1 )
            {
                try
                {
                    iObj = Int32.Parse( args[0] );
                }
                catch (FormatException)
                {
                    Console.WriteLine("Format exception");
                    return 1;
                }
            }
            


            CreateObj(iObj);
            if (RunTest(iObj))
            {
                Console.WriteLine("Test Passed!");
                return 100;
            }

            Console.WriteLine("Test Failed!");
            return 1;

        }
    }
}
