// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System;
    using System.IO;

    public class MasterThread
    {
        internal int iNum = 0;

        public MasterThread( int Children )
        {
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
            iNum = Children;
            runTest();
        }

        public void runTest()
        {

            LivingObject [ ]Mv_LivingObject = new LivingObject[ 25 ];
            int iTotal = Mv_LivingObject.Length;
            for ( int i = 0; i < iNum; i++ )
            {
                for ( int j = 0; j < iTotal; j++ )
                {
                    Console.Out.WriteLine( "{0} Object Created", j );
                    Console.Out.WriteLine();

                    Mv_LivingObject[ j ] = new LivingObject( );
                }

                Console.Out.WriteLine( "+++++++++++++++++++++++++++++++++++Nest {0} of {1}", i, iNum );
                Console.Out.WriteLine();
            }

            Console.Out.WriteLine( "******************************* FinalRest" );
        }


    }
}
