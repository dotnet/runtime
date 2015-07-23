// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentStackTrace
    {
        static string s_stackTrace;

        [Fact]
        public void StackTraceTest()
        {
            //arrange
            List<string> knownFrames = new List<string>()
            {
                "System.Runtime.Extensions.Tests.EnvironmentStackTrace.StaticFrame(Object obj)",
                "System.Runtime.Extensions.Tests.EnvironmentStackTrace.TestClass..ctor()",
                "System.Runtime.Extensions.Tests.EnvironmentStackTrace.GenericFrame[T1,T2](T1 t1, T2 t2)",
                "System.Runtime.Extensions.Tests.EnvironmentStackTrace.TestFrame()"
            };

            //act
            Task.Run(() => TestFrame()).Wait();

            //assert
            int index = 0;
            foreach(string frame in knownFrames)
            {
                index = s_stackTrace.IndexOf(frame, index);
                Assert.True(index > -1);
                index += frame.Length;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TestFrame()
        {
            GenericFrame<DateTime, StringBuilder>(DateTime.Now, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericFrame<T1, T2>(T1 t1, T2 t2)
        {
            var test = new TestClass();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticFrame(object obj)
        {
            s_stackTrace = Environment.StackTrace;
        }

        class TestClass
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public TestClass()
            {
                EnvironmentStackTrace.StaticFrame(null);
            }
        }
    }
}
