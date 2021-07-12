// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Runtime.Tests
{
    public class JitInfoTests
    {
        private long MakeAndInvokeDynamicSquareMethod(int input)
        {
            // example ref emit dynamic method from https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/how-to-define-and-execute-dynamic-methods
            Type[] methodArgs = {typeof(int)};

            DynamicMethod squareIt = new DynamicMethod(
                "SquareIt",
                typeof(long),
                methodArgs,
                typeof(JitInfoTests).Module);

            ILGenerator il = squareIt.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Conv_I8);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Ret);

            Func<int, long> invokeSquareIt =
                (Func<int, long>)
                squareIt.CreateDelegate(typeof(Func<int, long>));

            return invokeSquareIt(input);

        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))] // JitInfo metrics will be 0 in AOT scenarios
        public void JitInfoIsPopulated()
        {
            TimeSpan beforeCompilationTime = System.Runtime.JitInfo.GetCompilationTime();
            long beforeCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes();
            long beforeCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount();

            long square = MakeAndInvokeDynamicSquareMethod(100);
            Assert.True(square == 10000);

            TimeSpan afterCompilationTime = System.Runtime.JitInfo.GetCompilationTime();
            long afterCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes();
            long afterCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount();

            if (PlatformDetection.IsMonoInterpreter)
            {
                // special case the Mono interpreter where compilation time may be >0 but before and after will most likely be the same
                Assert.True(beforeCompilationTime >= TimeSpan.Zero, $"Compilation time not greater than 0! ({beforeCompilationTime})");
                Assert.True(beforeCompiledILBytes >= 0, $"Compiled IL bytes not greater than 0! ({beforeCompiledILBytes})");
                Assert.True(beforeCompiledMethodCount >= 0, $"Compiled method count not greater than 0! ({beforeCompiledMethodCount})");

                Assert.True(afterCompilationTime >= beforeCompilationTime, $"CompilationTime: after not greater than before! (after: {afterCompilationTime}, before: {beforeCompilationTime})");
                Assert.True(afterCompiledILBytes >= beforeCompiledILBytes, $"Compiled IL bytes: after not greater than before! (after: {afterCompiledILBytes}, before: {beforeCompiledILBytes})");
                Assert.True(afterCompiledMethodCount >= beforeCompiledMethodCount, $"Compiled method count: after not greater than before! (after: {afterCompiledMethodCount}, before: {beforeCompiledMethodCount})");
            }
            else
            {
                Assert.True(beforeCompilationTime > TimeSpan.Zero, $"Compilation time not greater than 0! ({beforeCompilationTime})");
                Assert.True(beforeCompiledILBytes > 0, $"Compiled IL bytes not greater than 0! ({beforeCompiledILBytes})");
                Assert.True(beforeCompiledMethodCount > 0, $"Compiled method count not greater than 0! ({beforeCompiledMethodCount})");

                Assert.True(afterCompilationTime > beforeCompilationTime, $"CompilationTime: after not greater than before! (after: {afterCompilationTime}, before: {beforeCompilationTime})");
                Assert.True(afterCompiledILBytes > beforeCompiledILBytes, $"Compiled IL bytes: after not greater than before! (after: {afterCompiledILBytes}, before: {beforeCompiledILBytes})");
                Assert.True(afterCompiledMethodCount > beforeCompiledMethodCount, $"Compiled method count: after not greater than before! (after: {afterCompiledMethodCount}, before: {beforeCompiledMethodCount})");
            }
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAOT))] // JitInfo metrics will be 0 in AOT scenarios
        public void JitInfoIsNotPopulated()
        {
            TimeSpan beforeCompilationTime = System.Runtime.JitInfo.GetCompilationTime();
            long beforeCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes();
            long beforeCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount();

            long square = MakeAndInvokeDynamicSquareMethod(100);
            Assert.True(square == 10000);

            TimeSpan afterCompilationTime = System.Runtime.JitInfo.GetCompilationTime();
            long afterCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes();
            long afterCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount();

            Assert.True(beforeCompilationTime == TimeSpan.Zero, $"Before Compilation time not eqeual to 0! ({beforeCompilationTime})");
            Assert.True(beforeCompiledILBytes == 0, $"Before Compiled IL bytes not eqeual to 0! ({beforeCompiledILBytes})");
            Assert.True(beforeCompiledMethodCount == 0, $"Before Compiled method count not eqeual to 0! ({beforeCompiledMethodCount})");

            Assert.True(afterCompilationTime == TimeSpan.Zero, $"After Compilation time not eqeual to 0! ({afterCompilationTime})");
            Assert.True(afterCompiledILBytes == 0, $"After Compiled IL bytes not eqeual to 0! ({afterCompiledILBytes})");
            Assert.True(afterCompiledMethodCount == 0, $"After Compiled method count not eqeual to 0! ({afterCompiledMethodCount})");
        }

        [Fact]
        [SkipOnMono("Mono does not track thread specific JIT information")]
        public void JitInfoCurrentThreadIsPopulated()
        {
            TimeSpan t1_beforeCompilationTime = TimeSpan.Zero;
            long t1_beforeCompiledILBytes = 0;
            long t1_beforeCompiledMethodCount = 0;

            TimeSpan t1_afterCompilationTime = TimeSpan.Zero;
            long t1_afterCompiledILBytes = 0;
            long t1_afterCompiledMethodCount = 0;

            TimeSpan t2_beforeCompilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
            long t2_beforeCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
            long t2_beforeCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);

            var t1 = new Thread(() => {
                t1_beforeCompilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
                t1_beforeCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
                t1_beforeCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
                long square = MakeAndInvokeDynamicSquareMethod(100);
                Assert.True(square == 10000);
                t1_afterCompilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
                t1_afterCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
                t1_afterCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
            });

            t1.Start();
            t1.Join();

            long square = MakeAndInvokeDynamicSquareMethod(100);
            Assert.True(square == 10000);

            TimeSpan t2_afterCompilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
            long t2_afterCompiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
            long t2_afterCompiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);

            Assert.True(t2_beforeCompilationTime > TimeSpan.Zero, $"Thread 2 Compilation time not greater than 0! ({t2_beforeCompilationTime})");
            Assert.True(t2_beforeCompiledILBytes > 0, $"Thread 2 Compiled IL bytes not greater than 0! ({t2_beforeCompiledILBytes})");
            Assert.True(t2_beforeCompiledMethodCount > 0, $"Thread 2 Compiled method count not greater than 0! ({t2_beforeCompiledMethodCount})");

            Assert.True(t2_afterCompilationTime > t2_beforeCompilationTime, $"CompilationTime: after not greater than before! (after: {t2_afterCompilationTime}, before: {t2_beforeCompilationTime})");
            Assert.True(t2_afterCompiledILBytes > t2_beforeCompiledILBytes, $"Compiled IL bytes: after not greater than before! (after: {t2_afterCompiledILBytes}, before: {t2_beforeCompiledILBytes})");
            Assert.True(t2_afterCompiledMethodCount > t2_beforeCompiledMethodCount, $"Compiled method count: after not greater than before! (after: {t2_afterCompiledMethodCount}, before: {t2_beforeCompiledMethodCount})");

            Assert.True(t1_beforeCompilationTime > TimeSpan.Zero, $"Thread 1 before compilation time not greater than 0! ({t1_beforeCompilationTime})");
            Assert.True(t1_beforeCompiledILBytes > 0, $"Thread 1 before compiled IL bytes not greater than 0! ({t1_beforeCompiledILBytes})");
            Assert.True(t1_beforeCompiledMethodCount > 0, $"Thread 1 before compiled method count not greater than 0! ({t1_beforeCompiledMethodCount})");

            Assert.True(t1_afterCompilationTime > t1_beforeCompilationTime, $"Thread 1 compilation time: after not greater than before! (after: {t1_afterCompilationTime}, before: {t1_beforeCompilationTime})");
            Assert.True(t1_afterCompiledILBytes > t1_beforeCompiledILBytes, $"Thread 1 compiled IL bytes: after not greater than before! (after: {t1_afterCompiledILBytes}, before: {t1_beforeCompiledILBytes})");
            Assert.True(t1_afterCompiledMethodCount > t1_beforeCompiledMethodCount, $"Thread 1 compiled method count: after not greater than before! (after: {t1_afterCompiledMethodCount}, before: {t1_beforeCompiledMethodCount})");

            Assert.True(t1_afterCompilationTime != t2_afterCompilationTime, $"Thread 1 compilation time: equal to other thread! (t1: {t1_afterCompilationTime}, t2: {t2_beforeCompilationTime})");
            Assert.True(t1_afterCompiledILBytes != t2_afterCompiledILBytes, $"Thread 1 compiled IL bytes: equal to other thread! (t1: {t1_afterCompiledILBytes}, t2: {t2_beforeCompiledILBytes})");
            Assert.True(t1_afterCompiledMethodCount != t2_afterCompiledMethodCount, $"Thread 1 compiled method count: equal to other thread! (t1: {t1_afterCompiledMethodCount}, t2: {t2_beforeCompiledMethodCount})");
        }

        [Fact]
        [SkipOnCoreClr("CoreCLR does track thread specific JIT information")]
        public void JitInfoCurrentThreadIsNotPopulated()
        {
            TimeSpan compilationTime = TimeSpan.Zero;
            long compiledILBytes = 0;
            long compiledMethodCount = 0;

            compilationTime = System.Runtime.JitInfo.GetCompilationTime(currentThread: true);
            compiledILBytes = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
            compiledMethodCount = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);

            Assert.True(compilationTime == TimeSpan.Zero, $"compilation time not equal to 0! ({compilationTime})");
            Assert.True(compiledILBytes == 0, $"compiled IL bytes not equal to 0! ({compiledILBytes})");
            Assert.True(compiledMethodCount == 0, $"compiled method count not equal to 0! ({compiledMethodCount})");
        }
    }
}
