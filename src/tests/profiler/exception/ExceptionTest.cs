// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Profiler.Tests
{
    class ExceptionTest
    {
        static readonly Guid ExceptionProfilerGuid = new Guid("E3C1F87D-1D20-4F1C-A35E-2C2D2E2B8F5D");

        public static void Foo()
        {
            try
            {
                
            }
            finally
            {
                try
                {
                }
                finally
                {
                    throw new Exception("Thrown from finally");
                }
            }
        }

        public static void Bar()
        {
            try
            {
                Foo();
            }
            catch (Exception)
            {
            }            
        }

        public static int RunTest(String[] args)
        {
            Bar();
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            // Verify that the function search / unwind function enter / leave events are not generated for funclets, that means
            // that the Foo occurs only once in the sequence.
            const string expectedSequence = 
                "ExceptionThrown\n"+
                "ExceptionSearchFunctionEnter: Foo\n"+
                "ExceptionSearchFunctionLeave\n"+
                "ExceptionSearchFunctionEnter: Bar\n"+
                "ExceptionSearchCatcherFound: Bar\n"+
                "ExceptionSearchFunctionLeave\n"+
                "ExceptionUnwindFunctionEnter: Foo\n"+
                "ExceptionUnwindFunctionLeave\n"+
                "ExceptionUnwindFunctionEnter: Bar\n"+
                "ExceptionCatcherEnter: Bar\n"+
                "ExceptionCatcherLeave\n";

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "ExceptionTest",
                                          profilerClsid: ExceptionProfilerGuid,
                                          envVars: new Dictionary<string, string>
                                          {
                                              { "Exception_Expected_Sequence", expectedSequence },
                                          });
        }
    }
}
