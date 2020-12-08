// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
/* Test:    NStructResur
/* Coverage:    NStruct objects' finalize can be called and the objects can be
/*      resurrected in finalize correctly. (verify it by accessing the
/*      objects after finalization.
/******************************************************************************/

namespace NStruct {
    using System;
    using System.Collections.Generic;

    internal class NStructResur
    {
        internal static List<STRMAP> alstrmap;

        public static void CreateObj(int iObj)
        {
            alstrmap = new List<STRMAP>();
            Console.WriteLine("Test should return with ExitCode 100 ...");

            for( int i=0; i< iObj; i++ ) //allocat 3100KB
            {
                alstrmap.Add(new STRMAP() );
            }

            alstrmap = new List<STRMAP>();

        }

        public static bool RunTest()
        {

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            for(int i=0; i<alstrmap.Count; i++)
            {
                alstrmap[i].AccessElement();
            }

            Console.WriteLine("Created object: {0}, Finalized objects: {1}", FinalizeCount.icCreat, FinalizeCount.icFinal);
            return ( FinalizeCount.icFinal == FinalizeCount.icCreat );
        }

        public static int Main(String [] args)
        {
            CreateObj(100);
            if (RunTest())
            {
                Console.WriteLine( "Test Passed" );
                return 100;
            }
            Console.WriteLine( "Test failed" );
            return 1;
        }

    }
}
