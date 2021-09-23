// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

class Program
{
    // Normalybehavior is to not print anything on success.
    //
    static bool verbose = false;

    // Repeatedly execute a test case's Main method so that methods jitted
    // by the test can get rejitted at Tier1.
    //
    static int Main(string[] args)
    {
        string testAssemblyName = args[0];

        // We'll stop iterating if total test time exceeds this value (in ms).
        //
        int timeout = 10_000;

        // Some tests return zero for success.
        //
        int expectedResult = 100;

        string[][] zeroReturnValuePatterns = {
            new string[] { "JIT", "jit64", "regress", "vsw", "102754", "test1"},
            new string[] { "JIT", "Regression", "CLR-x86-JIT", "V1-M09", "b16102", "b16102"},
        };

        foreach (string[] pattern in zeroReturnValuePatterns)
        {
            if (testAssemblyName.IndexOf(Path.Join(pattern)) > 0)
            {
                expectedResult = 0;
                break;
            }
        }

        // Exclude tests that seem to be incompatible.
        // Todo: root cause these and fix tests if possible.
        //
        // With Full PGO:
        // RngchkStress2_o can hit a jit assert: '!m_failedToConverge' in 'SimpleArray_01.Test:Test1()' during 'Profile incorporation'
        // GitHub_25027 can hit a jit assert: 'verCurrentState.esStackDepth == 0' in 'X:Main():int' during 'Morph - Inlining'
        //
        string[][] exclusionPatterns = {
            new string[] { "JIT", "jit64", "opt", "cse", "VolatileTest_op" },
            new string[] { "JIT", "jit64", "opt", "rngchk", "ArrayWithThread_o" },
            new string[] { "baseservices", "threading", "threadstatic", "ThreadStatic01" },
            new string[] { "GC", "Scenarios", "ReflectObj", "reflectobj"},
            new string[] { "baseservices", "threading", "mutex", "openexisting", "openmutexpos4"},
            new string[] { "GC", "Scenarios", "NDPin", "ndpinfinal"},
            new string[] { "JIT", "Regression", "JitBlue", "GitHub_4044", "GitHub_4044"},
            new string[] { "JIT", "HardwareIntrinsics", "X86", "Regression", "GitHub_21666", "GitHub_21666_ro"},
            new string[] { "Interop", "NativeLibrary", "API", "NativeLibraryTests"},
            new string[] { "baseservices", "compilerservices", "FixedAddressValueType", "FixedAddressValueType"},
            new string[] { "GC", "LargeMemory", "API", "gc", "gettotalmemory" },

            new string[] { "JIT", "jit64", "opt", "rngchk", "RngchkStress2_o" },
            new string[] { "JIT", "Regression", "JitBlue", "GitHub_25027", "GitHub_25027" },
        };

        foreach (string[] pattern in exclusionPatterns)
        {
            if (testAssemblyName.IndexOf(Path.Join(pattern)) > 0)
            {
                if (verbose)
                {
                    Console.WriteLine($"Test {Path.Join(pattern)} excluded; marked as incompatible");
                }
                return expectedResult;
            }
        }

        AssemblyLoadContext alc = AssemblyLoadContext.Default;
        Assembly testAssembly = alc.LoadFromAssemblyPath(testAssemblyName);
        MethodInfo main = testAssembly.EntryPoint;

        if (main == null)
        {
            Console.WriteLine($"Can't find entry point in {Path.GetFileName(args[0])}");
            return -1;
        }

        string[] mainArgs = new string[args.Length - 1];
        Array.Copy(args, 1, mainArgs, 0, mainArgs.Length);

        // Console.WriteLine($"Found entry point {main.Name} in {Path.GetFileName(args[0])}");

        // See if main wants any args.
        //
        var mainParams = main.GetParameters();

        int result = 0;

        // Repeatedly invoke main to get things to pass through Tier0 and rejit at Tier1
        //
        int warmup = 40;
        int final = 5;
        int total = warmup + final;
        int i = 0;
        int sleepInterval = 5;
        Stopwatch s = new Stopwatch();
        s.Start();

        // We might decide to give up on this test, for reasons.
        //
        // If the test fails at iteration 0, assume it's incompatible with the way we're running it
        // and don't report as a failure.
        //
        // If the test fails at iteration 1, assume it's got some bit of state that carries over
        // from one call to main to the next, and so don't report it as failure.
        //
        // If the test takes too long, just give up on iterating it.
        //
        bool giveUp = false;

        try
        {

            for (; i < warmup && !giveUp; i++)
            {
                if (mainParams.Length == 0)
                {
                    result = (int)main.Invoke(null, new object[] { });
                }
                else
                {
                    result = (int)main.Invoke(null, new object[] { mainArgs });
                }

                if (result != expectedResult)
                {
                    if (i < 2)
                    {
                        Console.WriteLine($"[tieringtest] test failed at iteration {i} with result {result}. Test is likely incompatible.");
                        result = expectedResult;
                    }
                    else
                    {
                        Console.WriteLine($"[tieringtest] test failed at iteration {i}: {result} (expected {expectedResult})");
                    }
                    giveUp = true;
                    break;
                }

                // Don't iterate if test is running long.
                //
                if (s.ElapsedMilliseconds > timeout)
                {
                    Console.WriteLine($"[tieringtest] test running long ({s.ElapsedMilliseconds / (i + 1)} ms/iteration). Giving up at iteration {i}");
                    giveUp = true;
                    break;
                }

                // Give TC a chance to jit some things.
                //
                Thread.Sleep(sleepInterval);
            }

            for (; i < total && !giveUp; i++)
            {
                if (mainParams.Length == 0)
                {
                    result = (int)main.Invoke(null, new object[] { });
                }
                else
                {
                    result = (int)main.Invoke(null, new object[] { mainArgs });
                }

                if (result != expectedResult)
                {
                    Console.WriteLine($"[tieringtest] failed at iteration {i}: {result} (expected {expectedResult})");
                    giveUp = true;
                    break;
                }

                // Don't iterate if test is running long.
                //
                if (s.ElapsedMilliseconds > timeout)
                {
                    Console.WriteLine($"[tieringtest] test running long ({s.ElapsedMilliseconds / (i + 1)} ms/iteration). Giving up at iteration {i}");
                    giveUp = true;
                    break;
                }
            }

            if (result == expectedResult)
            {
                if (verbose)
                {
                    Console.WriteLine($"[tieringtest] ran {total} test iterations sucessfully");
                }
            }
        }
        catch (Exception e)
        {
            if (i < 2)
            {
                if (verbose)
                {
                    Console.WriteLine($"[tieringtest] test failed at iteration {i} with exception {e.Message}. Test is likely incompatible.");
                }
                result = expectedResult;
            }
            else
            {
                Console.WriteLine($"[tieringtest] test failed at iteration {i} with exception {e.Message}");
                result = -1;
            }
        }

        return result;
    }
}
