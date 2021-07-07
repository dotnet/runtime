// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Threading;
using Xunit;

namespace System.Runtime.Tests
{
    public class JitInfoTests
    {
        private const TestPlatforms AotPlatforms = TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.Android;

        [Fact]
        [SkipOnPlatform(AotPlatforms, "JitInfo metrics will be 0 in AOT scenarios.")]
        public void JitInfoIsPopulated()
        {
            Func<string> theFunc = () => "JIT compile this!";
            Assert.True(theFunc().Equals("JIT compile this!"));
            Assert.True(System.Runtime.JitInfo.GetCompilationTime() > TimeSpan.Zero);
            Assert.True(System.Runtime.JitInfo.GetCompiledILBytes() > 0);
            Assert.True(System.Runtime.JitInfo.GetCompiledMethodCount() > 0);
        }

        [Fact]
        [SkipOnMono("Mono does not track thread specific JIT information")]
        public void JitInfoCurrentThreadIsPopulated()
        {
            TimeSpan t1_compilationTime = TimeSpan.Zero;
            long t1_compiledILBytes = 0;
            long t1_compiledMethodCount = 0;

            TimeSpan t2_compilationTime = TimeSpan.Zero;
            long t2_compiledILBytes = 0;
            long t2_compiledMethodCount = 0;

            var t1 = new Thread(() => {
                Func<string> theFunc = () => "JIT compile this!";
                Assert.True(theFunc().Equals("JIT compile this!"));
                t1_compilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
                t1_compiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
                t1_compiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
            });

            var t2 = new Thread(() => {
                Func<string> theFunc2 = () => "Also JIT compile this!";
                Assert.True(theFunc2().Equals("Also JIT compile this!"));
                t2_compilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
                t2_compiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
                t2_compiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            Assert.True(t1_compilationTime > TimeSpan.Zero);
            Assert.True(t1_compiledILBytes > 0);
            Assert.True(t1_compiledMethodCount > 0);

            Assert.True(t2_compilationTime > TimeSpan.Zero);
            Assert.True(t2_compiledILBytes > 0);
            Assert.True(t2_compiledMethodCount > 0);
        }
    }
}
