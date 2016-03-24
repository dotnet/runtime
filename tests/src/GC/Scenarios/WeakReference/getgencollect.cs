// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************************/
/* test: GetGenCollect
/* Purpose: Test GC.GetGeneration(Object/WeakRefernce) and GC.Collect( Gen )
/* How: 1.GC.Collect( gen ) should collect the object in "gen" generation.
/*  2.in Object mv_obj's finalize, the return value of GetGeneration(this)
/*  should be same with GetGeneration(wf);
/******************************************************************************/

namespace DefaultNamespace {
    using System;

    internal class GetGenCollect
    {
        internal int Gen;
        internal static WeakReference wf;
        internal static bool retVal;
        public static int Main( String [] str )
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");

            GetGenCollect mv_obj = new GetGenCollect();
            wf = new WeakReference( mv_obj, true );
            mv_obj.MakeGCBusy();
            mv_obj.Gen = GC.GetGeneration( mv_obj );
            int g = mv_obj.Gen;
            mv_obj = null;

            GC.Collect( g );
            GC.WaitForPendingFinalizers();
            GC.Collect( g );

            if (retVal)
            {
                Console.WriteLine ("Test Passed" );
                return 100;
            }
            Console.WriteLine ("Test Failed" );
            return 1;

        }


        ~GetGenCollect()
        {
            Console.WriteLine( "Verified that the object in generation {0} is finalized by calling GC.Collect({0}).",Gen);
            int g = GC.GetGeneration( this );

            int gwf = GC.GetGeneration( GetGenCollect.wf );
            Console.WriteLine( "g={0}, gwf={1}", g, gwf );

            if( g != gwf )
            {
                Console.WriteLine( "GetGeneration( WeakReferance ) may have problem!" );
                retVal = false;
                return;
            }

            Console.WriteLine( "Passed " );
            retVal = true;

        }

        public void MakeGCBusy()
        {
            Object [] vary = new Object[2];
            for( int i=0; i< 1000; i++ )
            {
                vary[0] = (new int[i]);
                vary[1] = ( vary[0] );
                if( i%20 == 0 )
                {
                    GC.Collect();
                }
            }
        }
    }
}
