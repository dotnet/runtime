// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System;
    using System.Runtime.CompilerServices;

    internal class GetTotalMemory
    {
        // margin of error, since GetTotalMemory is an approximation
        // a discrepancy of more than 50 bytes should be investigated
        public const int padding = 50;

        public static bool AllocAndDealloc(int i, int MB, long heapSizeBeforeAlloc)
        {
            byte[] bary = new byte[i*MB];  //allocate iMB memory
            bary[0] = 1;
            bary[i*MB-1] = 1;

            long heapSizeAfterAlloc = GC.GetTotalMemory(false);
            Console.WriteLine( "HeapSize after allocated {0} MB memory: {1}", i, heapSizeAfterAlloc);
            if( (heapSizeAfterAlloc - heapSizeBeforeAlloc)+i*padding<= i*MB || (heapSizeAfterAlloc - heapSizeBeforeAlloc) > (i+1)*MB )
            {
                Console.WriteLine( "Test Failed" );
                return false;
            }
            bary[0] = 2;
            bary[i*MB-1] = 2;
            bary = null;
            return true;
        }

        public static int Main(String [] args )
        {
            int MB = 1024*1024;
            int iRep = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            if (args.Length==0)
            {
                iRep = 10;
            }
            else if (args.Length == 1)
            {
                if (!Int32.TryParse( args[0], out iRep ))
                {
                    iRep = 10;
                }
            }
            else
            {
                Console.WriteLine("usage: GetTotalMemory arg, good arg range is 5--50. Default value is 10." );
                return 1;
            }

            // clean up memory before measuring
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long heapSizeBeforeAlloc = GC.GetTotalMemory(false);

            Console.WriteLine( "HeapSize before allocating any memory: {0}", heapSizeBeforeAlloc );

            for(int i=1; i<=iRep; i++ )
            {
                if(!AllocAndDealloc(i, MB, heapSizeBeforeAlloc))
                {
                    return 1;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                heapSizeBeforeAlloc = GC.GetTotalMemory(false);
                Console.WriteLine( "HeapSize after delete all objects: {0}", heapSizeBeforeAlloc );
            }

            Console.WriteLine( "Test Passed!" );
            return 100;
        }
    }
}
