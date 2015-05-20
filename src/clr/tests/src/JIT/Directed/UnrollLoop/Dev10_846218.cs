// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace Test
{
    internal struct IntVec
    {
        public int x;
        public int y;
    }

    internal interface IDoSomething
    {
        void Do(IntVec o);
    }

    internal class DoSomething : IDoSomething
    {
        public string output = "";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Do(IntVec o)
        {
            output = output + o.x.ToString() + " " + o.y.ToString() + ",";
        }
    }

    internal class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Test(IDoSomething oDoesSomething)
        {
            IntVec oVec = new IntVec();
            for (oVec.x = 0; oVec.x < 2; oVec.x++)
            {
                for (oVec.y = 0; oVec.y < 2; oVec.y++)
                {
                    oDoesSomething.Do(oVec);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Main(string[] args)
        {
            DoSomething doSomething = new DoSomething();
            Test(doSomething);
            if (doSomething.output == "0 0,0 1,1 0,1 1,")
            {
                Console.WriteLine("PASS");
                return 100;
            }
            Console.WriteLine("Expected :{0} but found :{1}", "0 0,0 1,1 0,1 1,", doSomething.output);
            return 101;
        }
    }
}

