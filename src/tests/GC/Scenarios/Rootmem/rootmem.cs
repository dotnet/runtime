// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*******************************************************************/
/* Test: RootMem
/* Purpose: Test if Root class manage memory correctly against GC
/* Coverage:    Root.Alloc(), Root.Free(), Root.Get()
/*******************************************************************/

namespace DefaultNamespace {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal class RootMem
    {
        internal long [] l;
        internal static GCHandle [] root;
        internal static int n;


        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void AllocRoot()
        {
        }

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

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Object v;
            for( int i=0; i< iSize; i++)
            {
                v = ( root[i]) ;
            }

            GC.Collect();

            for( int i=0; i<iSize; i++ )
            {
                root[i].Free();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

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
