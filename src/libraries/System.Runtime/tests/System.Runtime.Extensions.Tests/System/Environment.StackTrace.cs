// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public class EnvironmentStackTrace
    {
        static string s_stackTrace;

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15315", TestRuntimes.Mono)]
        public void StackTraceTest()
        {
            //arrange
            List<string> knownFrames = new List<string>()
            {
                "System.Tests.EnvironmentStackTrace.StaticFrame(Object obj)",
                "System.Tests.EnvironmentStackTrace.TestClass..ctor()",
                "System.Tests.EnvironmentStackTrace.GenericFrame[System.DateTime,System.Text.StringBuilder](DateTime t1, StringBuilder t2)",
                "System.Tests.EnvironmentStackTrace.TestFrame()"
            };

            //act
            Task.Run(() => TestFrame()).Wait();

            //assert
            int index = 0;
            foreach (string frame in knownFrames)
            {
                index = s_stackTrace.IndexOf(frame, index);
                Assert.True(index > -1);
                index += frame.Length;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void TestFrame()
        {
            GenericFrame<DateTime, StringBuilder>(DateTime.Now, null);
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void GenericFrame<T1, T2>(T1 t1, T2 t2)
        {
            new TestClass();
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void StaticFrame(object obj)
        {
            s_stackTrace = Environment.StackTrace;
        }

        private class TestClass
        {
            [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
            public TestClass()
            {
                StaticFrame(null);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void StackTraceDoesNotStartWithInternalFrame()
        {
             string stackTrace = Environment.StackTrace;

             // Find first line of the stacktrace and verify that it is Environment.get_StackTrace itself, not an internal frame
             string firstFrame = new StringReader(stackTrace).ReadLine();

             Assert.True(firstFrame.IndexOf("System.Environment.get_StackTrace()") != -1);
        }
    }
}
