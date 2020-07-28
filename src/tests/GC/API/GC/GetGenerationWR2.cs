// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/********************************************************************/
/* Test: GetGeneration
/* Purpose: Test GC.GetGeneration() works
/* Note: This test is not an absolute test. If it passes, it doesn't
/* Gerantee that GetGeneration works fine, because GC.GetGeneration(Object)
/* and GC.GetGeneration(WeakReference) may break in same way. If it failed,
/* it needs investigation.
/********************************************************************/

namespace DefaultNamespace {
    using System;

    internal class GetGeneration
    {
        public static int Main( String [] str )
        {
            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            Object o = new int[10];
            WeakReference wf = new WeakReference( o );
            bool result = false;

            try
            {

                result = ( GC.GetGeneration( o ) == GC.GetGeneration( wf ));

                GC.KeepAlive(o);

            }
            catch (ArgumentNullException)
            {
                Console.Out.WriteLine( "Caught ArgumentNullException!" );
                result = false;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine( "Caught unexpected exception!" );
                Console.Out.WriteLine(e.Message);
                result = false;
            }


            if (result)
            {
                Console.Out.WriteLine( "Test Passed" );
                return 100;
            }
            Console.Out.WriteLine( "Test Failed" );

            return 1;

        }
    }
}
