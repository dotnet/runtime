// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************
/*ByteObject is a class to allocate small size of memory. this test creates
/*two Large size of ByteObject arrays, then delete all the reference in the
/* arrays to make millions(iObj) of small leaks to see if GC can handle so
/*many leaks at same time. ByteObject's size is variable.
/****************************************************************************/
namespace DefaultNamespace {
    using System;

    public class MinLeakGen
    {
        internal static ByteObject []Mv_Obj = new ByteObject[1024*5];
        internal static ByteObject []Mv_Obj1 = new ByteObject[1024*5];
        public static int Main(System.String [] Args)
        {
            int iRep = 0;
            int iObj = 0; //the number of Mb will be allocated in MakeLeak();

            Console.WriteLine("Test should return with ExitCode 100 ...");
            switch( Args.Length )
            {
                case 1:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                break;

                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 5;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 1024*5;
                    }
                break;

                default:
                    iRep = 5;
                    iObj = 1024*5;
                break;

            }

            MinLeakGen Mv_Leak = new MinLeakGen();
            if(Mv_Leak.runTest(iRep, iObj ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test Failed");
                return 1;
            }
        }


        public bool runTest(int iRep, int iObj)
        {
            for(int i = 0; i<iRep; i++)
            {
                MakeLeak(iObj);
            }
            return true;
        }

        public void MakeLeak(int iObj)
        {
            for(int i=0; i<iObj; i++)
            {
                Mv_Obj[i] = new ByteObject(i/10+1);
                Mv_Obj1[i] = new ByteObject(i/10+1);
            }
            for(int i=0; i<iObj; i++)
            {
                Mv_Obj[i] = null;
                Mv_Obj1[i] = null;
            }
        }

    }

    public class ByteObject
    {
        internal byte[] min;
        public ByteObject(int size)
        {
            min = new byte[size];
            min[0] = 1;
            min[size - 1] = 2;
        }
    }
}
