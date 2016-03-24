// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*******************************************************************/
/* Test: RootMem
/* Purpose: Test if Root class manage memory correctly against GC
/* Coverage:    Root.Alloc(), Root.Free(), Root.Get()
/*******************************************************************/

namespace DefaultNamespace {
    using System;
    using System.Runtime.InteropServices;

    internal class RootMem
    {
        internal long [] l;
        internal static GCHandle [] root;
        internal static int n;

        public static int Main( String [] args )
        {
            int iSize = 1000;
            Object [] arVar = new Object[iSize];
            root = new GCHandle[iSize];
            RootMem rm_obj;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            for( n=0; n< iSize; n++ )
            {
                 rm_obj = new RootMem( n );
                 root[n] = GCHandle.Alloc(rm_obj );
            }
            //Console.WriteLine("After save objects to Root and before GCed: "+GC.GetTotalMemory(false) );
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            //Console.WriteLine("After save objects to Root and after GCed: "+GC.GetTotalMemory(false) );

            Object v;
            for( int i=0; i< iSize; i++)
            {
                v = ( root[i]) ;
            }
            //Console.WriteLine("After Get objects from root and before GCed: "+GC.GetTotalMemory(false) );
            GC.Collect();
            //Console.WriteLine("After Get objects from root and after GCed: "+GC.GetTotalMemory(false) );

            for( int i=0; i<iSize; i++ )
            {
                root[i].Free();
            }
            //Console.WriteLine("After free root and before GCed: "+GC.GetTotalMemory(false) );
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            //Console.WriteLine("After free root and after GCed: "+GC.GetTotalMemory(false) );
            try
            {
                for( int i=0; i<iSize; i++ )
                {
                    arVar[i]= ( root[i].Target  );
                }
            }
            catch(System.InvalidOperationException)
            {
                //expected exception is throw after gchandles were free
                Console.WriteLine("test Passed");
                return 100;
            }

            Console.WriteLine("test failed");
            return 1;
        }

        public RootMem( int i )
        {
            if( i> 0)
            {
                l = new long[i];
                l[0] = 0;
                l[i-1] = i;
            }
        }

        ~RootMem()
        {
        }
    }
}
