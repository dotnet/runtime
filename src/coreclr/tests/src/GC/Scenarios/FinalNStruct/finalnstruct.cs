// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//***************************************************************************/
//* Test:   FinalNStruct
//* Coverage:   1. check if GC can collect NStruct memeory (externally_allocated
//*     memory correctly.
//*     2. If all NStruct's finalize() get called after they lose ref.
//****************************************************************************/

namespace NStruct {
    using System;
    using System.Runtime.CompilerServices;

    internal class FinalNStruct
    {

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void CreateObj(int iObj)
        {
            STRMAP []strmap = new STRMAP[iObj];
            for (int i=0; i< iObj; i++ ) //allocate 3100KB
            {
                strmap[i] = new STRMAP();
            }
            for( int i=0; i< iObj; i++ )
            {
                strmap[i] = null;
            }
        }

        public static bool RunTest()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return ( FinalizeCount.icFinal == FinalizeCount.icCreat );
        }

        public static int Main(String [] args){
            int iObj = 100;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            CreateObj(iObj);

            if (RunTest())
            {
                Console.WriteLine( "Created objects number is same with finalized objects." );
                Console.WriteLine( "Test Passed !" );
                return 100;
            }

            Console.WriteLine( "Created objects number is not same with finalized objects (" + FinalizeCount.icFinal + " of " + FinalizeCount.icCreat + ")");
            Console.WriteLine( "Test failed !" );
            return 1;

        }

    }
}
