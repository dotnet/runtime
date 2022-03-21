// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Profiler.Tests
{
    class InliningTest
    {
        private static readonly Guid InliningGuid = new Guid("DDADC0CB-21C8-4E53-9A6C-7C65EE5800CE");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Inlinee()
        {
            Random rand = new Random();
            return rand.Next();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Inlining()
        {
            int x = Inlinee();
            Console.WriteLine($"Inlining, x={x}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BlockInlining()
        {
            int x = Inlinee();
            Console.WriteLine($"BlockInlining, x={x}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NoResponse()
        {
            int x = Inlinee();
            Console.WriteLine($"NoResponse, x={x}");
        }

        public static int RunTest(string[] args)
        {
            Inlining();
            BlockInlining();
            NoResponse();

            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "UnitTestInlining",
                                          profilerClsid: InliningGuid,
                                          profileeOptions: ProfileeOptions.OptimizationSensitive);
        }
    }
}
