// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal class Test
    {
        private static bool s_globalFlag = false;

        private static bool TestTryCatch(int recurseLevel)
        {
            if (recurseLevel > 1)
            {
                try
                {
                    if (s_globalFlag = !s_globalFlag)
                    {
                        //call recursively
                        return TestTryFinally(recurseLevel - 2);
                    }
                    else
                    {
                        //raise an exception and allow the handler work
                        int[] p = null;
                        p[0] = 0;

                        //paranoid check
                        Console.WriteLine("Shouldn't have reached here.");
                        throw new Exception();
                    }
                }
                catch (NullReferenceException)
                {
                    return TestTryCatch(recurseLevel);
                }
            }
            return recurseLevel == 0;
        }

        private static bool TestTryFinally(int recurseLevel)
        {
            if (recurseLevel > 1)
            {
                bool ret = false;
                try
                {
                    bool runHandler = false;
                    try
                    {
                        if (s_globalFlag = !s_globalFlag)
                        {
                            //call recursively
                            return TestTryCatch(recurseLevel - 2);
                        }
                        else
                        {
                            runHandler = true;

                            //raise an exception and allow the handler work
                            int[] p = null;
                            p[0] = 0;

                            //paranoid check
                            Console.WriteLine("Shouldn't have reached here.");
                            throw new Exception();
                        }
                    }
                    finally
                    {
                        if (runHandler)
                        {
                            ret = TestTryFinally(recurseLevel);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                }
                return ret;
            }
            return recurseLevel == 0;
        }

        private static int Main()
        {
            try
            {
                if (TestTryCatch(15) || !TestTryCatch(18))
                {
                    Console.WriteLine("try...catch test failed.");
                    return 1;
                }
                if (TestTryFinally(19) || !TestTryFinally(12))
                {
                    Console.WriteLine("try...finally test failed.");
                    return 2;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed w/ exception");
                return -1;
            }
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
