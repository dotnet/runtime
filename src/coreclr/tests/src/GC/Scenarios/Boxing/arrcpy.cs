// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System;

    internal class ArrCpy
    {
        public static int Main(String [] str)
        {
            int iSize = 100;
            int iRep = 10;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            ArrCpy mv_obj = new ArrCpy();
            Object [] ObjAry = new Object[iSize];
            for( int j=0; j< iRep; j++ )
            {
                for(int i=0; i< iSize; i++)
                {
                    if( i==0 )
                        ObjAry[i] = new int[1];
                    else if( i==1 )
                        ObjAry[i] = new Object[i];
                    else
                        ObjAry[i] = mv_obj.CreatAry( i-1, ObjAry );
                }
                GC.Collect();
            }
            return 100;

        }


        public Object CreatAry( int iSize, Object [] ObjAry)
        {
            Object [] ary = new Object[iSize];
            if( ary.Length > 1 )
            {
                Array.Copy( ObjAry, ary, ary.Length-2 );
                if( ary.Length-1 == 1 )
                {
                    ary[ary.Length-1] = new byte[ary.Length];
                }
                else
                {
                    ary[ary.Length-1] = CreatAry( ary.Length-1, ary );
                }
            }
            else
            {
                ary[0] = new Object[0];
            }
            return ary;
        }
    }
}
