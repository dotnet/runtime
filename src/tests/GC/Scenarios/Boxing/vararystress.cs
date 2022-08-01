// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/* Test: VarAryStress
/* Coverage:    Test GC and Variant Array
/* What:    Use SetVarAry method that calls himself to generate
/* Variant array elements. The elements are Variant arrays whose
/* elememts also are Variant array. iRep is the max embedded level
/* number of these Variant array. Check out if GC can handle these
/* objects
/**************************************************************/

namespace DefaultNamespace {
    using System;

    internal class VarAryStress
    {
        public static int Main( String [] args )
        {
            int iRep = 20;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            if( args.Length > 0 )
            {
                try
                {
                    iRep = Int32.Parse( args[0] );
                }
                catch(FormatException )
                {
                    Console.WriteLine("FormatException is caught");
                }
            }

            Object [] VarAry = new Object[1];
            VarAryStress mv_obj = new VarAryStress();
            for(int i=0; i< iRep; i++ )
            {
                if( i>1 )
                {
                    VarAry[0] = mv_obj.SetVarAry( i-1 );
                }
                else
                {
                    VarAry[0] = i;
                }
                if( i%5 == 0)
                {
                    GC.Collect();
                    // Console.WriteLine( "HeapSize after GC: {0}", GC.GetTotalMemory(false) );
                }

            }

            Console.WriteLine( "Test Passed" );
            return 100;
        }

        public Object SetVarAry( int iSize )
        {
            Object [] vary= new Object[2];
            for( int i=0; i< iSize; i++ )
            {
                if( i > 1 )
                    vary[0] = SetVarAry( i-1 );
                else
                    vary[1] = i;
            }
            return vary;
        }
    }
}
