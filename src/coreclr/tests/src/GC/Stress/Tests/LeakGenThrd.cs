// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Threading;
using System;
using System.IO;


// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace LGen
{
    public class LeakGenThrd
    {
        internal int myObj;
        internal int Cv_iCounter = 0;
        internal int Cv_iRep;

        public static int Main(System.String[] Args)
        {
            int iRep = 2;
            int iObj = 15; //the number of MB memory will be allocted in MakeLeak()

            switch (Args.Length)
            {
                case 1:
                    if (!Int32.TryParse(Args[0], out iRep))
                    {
                        iRep = 2;
                    }
                    break;
                case 2:
                    if (!Int32.TryParse(Args[0], out iRep))
                    {
                        iRep = 2;
                    }
                    if (!Int32.TryParse(Args[1], out iObj))
                    {
                        iObj = 15;
                    }
                    break;
                default:
                    iRep = 2;
                    iObj = 15;
                    break;
            }

            LeakGenThrd Mv_Leak = new LeakGenThrd();
            if (Mv_Leak.runTest(iRep, iObj))
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
            Cv_iRep = iRep;
            myObj = iObj;

            Thread Mv_Thread = new Thread(new ThreadStart(this.ThreadStart));
            Mv_Thread.Start();

            for (int i = 0; i < iRep; i++)
            {
                MakeLeak(iObj);
            }

            return true;
        }



        public void ThreadStart()
        {
            if (Cv_iCounter < Cv_iRep)
            {
                LeakObject[] Mv_Obj = new LeakObject[myObj];
                for (int i = 0; i < myObj; i++)
                {
                    Mv_Obj[i] = new LeakObject(i);
                }

                Cv_iCounter += 1;

                Thread Mv_Thread = new Thread(new ThreadStart(this.ThreadStart));
                Mv_Thread.Start();
            }
        }

        public void MakeLeak(int iObj)
        {
            LeakObject[] Mv_Obj = new LeakObject[iObj];
            for (int i = 0; i < iObj; i++)
            {
                Mv_Obj[i] = new LeakObject(i);
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
