// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace LGen {
    using System;
    using System.Runtime.CompilerServices;

    public class LeakGen
    {
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
                        iRep = 2;
                    }
                break;
                case 2:
                    if (!Int32.TryParse( Args[0], out iRep ))
                    {
                        iRep = 2;
                    }
                    if (!Int32.TryParse( Args[1], out iObj ))
                    {
                        iObj = 10;
                    }
                break;
                default:
                    iRep = 2;
                    iObj = 10;
                break;
            }

            LeakGen Mv_Leak = new LeakGen();
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
                /*allocate about 10MB memory include MakeLeak() */
                MakeLeak(iObj);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Console.WriteLine("~LeakObject() was called {0} times.", LeakObject.icFinal);
            return (LeakObject.icFinal == iObj*iRep);
        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void MakeLeak(int iObj)
        {
            int [] mem;

            LeakObject []Mv_Obj = new LeakObject[iObj];
            for(int i=0; i<iObj; i++)
            {
                Mv_Obj[i] = new LeakObject(i);
                mem = new int[1024*250]; //nearly 1MB memory, larger than this will get assert failure .
                mem[0] = 1;
                mem[mem.Length-1] = 1;
            }

        }


    }

    public class LeakObject
    {
        internal int[] mem;
        public static int icFinal = 0;
        public LeakObject(int num)
        {
            mem = new int[1024 * 250]; //nearly 1MB memory, larger than this will get assert failure.
            mem[0] = num;
            mem[mem.Length - 1] = num;
        }

        ~LeakObject()
        {
            LeakObject.icFinal++;
        }
    }
}
