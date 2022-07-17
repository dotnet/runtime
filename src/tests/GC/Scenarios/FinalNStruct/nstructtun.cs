// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NStruct
{
    using System;
    using System.Runtime.CompilerServices;

    internal class NStructTun
    {

        public class CreateObj
        {
            // disabling unused variable warning
#pragma warning disable 0414
            private STRMAP Strmap;
#pragma warning restore 0414
            public CreateObj(int Rep)
            {
                for (int i = 0; i < Rep; i++)
                {
                    Strmap = new STRMAP();
                }
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public void DestroyStrmap()
            {
                Strmap = null;
            }

            public bool RunTest()
            {
                DestroyStrmap();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.Out.WriteLine(FinalizeCount.icCreat + " NStruct Objects were deleted and " + FinalizeCount.icFinal + " finalized.");

                return (FinalizeCount.icCreat == FinalizeCount.icFinal);
            }

        }

        public static int Main(String[] Args)
        {
            int iRep = 0;

            Console.Out.WriteLine("Test should return with ExitCode 100 ...");

            if (Args.Length == 1)
            {
                if (!Int32.TryParse(Args[0], out iRep))
                {
                    iRep = 10000;
                }
            }
            else
            {
                iRep = 10000;
            }
            Console.Out.WriteLine("iRep = " + iRep);

            CreateObj temp = new CreateObj(iRep);

            if (temp.RunTest())
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.WriteLine("Test Failed");
            return 1;

        }
    }


}
