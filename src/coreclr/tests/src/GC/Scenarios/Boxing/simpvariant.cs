// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Basic Object Test that uses tests the following types:
//  int
//  long
//  String
//  float
// as Objects. This is very Basic - uses arrays of 500 and if it does not crash
// that is a valid test pass.
//


namespace DefaultNamespace {
    using System;

    public class SimpObject
    {
        public static int Main ()
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");
            SimpObject sv = new SimpObject( );
            sv.RunTest( );
            return 100;
        }

        public void RunTest()
        {
            int Size = 500;

            Console.WriteLine( "Simple Object GC Test" );
            Console.WriteLine( "Starting int & String Test..." );

            Object [] tempArray = new Object[ Size ];

            for( int i = 0; i < Size; i++ )
            {
                if( i % 2 == 1 )
                {
                    tempArray[i] = i;
                }
                else
                {
                    char[] carr = new char[1];
                    carr[0] = (char)i;
                    tempArray[i] = new string(carr);
                }

                GC.Collect( );
            }

            tempArray = null;

            GC.Collect( );

            Console.WriteLine( "int & String Test Complete." );
            Console.WriteLine();
            Console.WriteLine( "Starting float, long, & String Test..." );

            tempArray = new Object[ Size ];

            for( int i = 0; i < Size; i++ )
            {
                if( i % 2 == 1 )
                {

                    if( i < Size / 2 )
                    {
                        float foo = i;
                        tempArray[ i ] = foo;
                    }
                    else
                    {
                        long foo = i;
                        tempArray[ i ] = foo;
                    }
                }
                else
                {
                    char[] carr = new char[1];
                    carr[0] = (char)i;
                    tempArray[i] = new string(carr);
                }

                GC.Collect( );
            }

            tempArray = null;

            GC.Collect( );

            Console.WriteLine( "float, long, & String Test complete." );

        }
    }
}
