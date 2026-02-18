// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tests
{
    public class VolatileTests
    {
        [Fact]
        public void ReadBarrier_DoesNotThrow()
        {
            // ReadBarrier should not throw and should complete successfully
            Volatile.ReadBarrier();
        }

        [Fact]
        public void WriteBarrier_DoesNotThrow()
        {
            // WriteBarrier should not throw and should complete successfully
            Volatile.WriteBarrier();
        }

        [Fact]
        public void ReadBarrier_EnsuresMemoryOrdering()
        {
            // Test that ReadBarrier can be called multiple times without issues
            int x = 0;
            int y = 0;

            x = 1;
            Volatile.ReadBarrier();
            y = x;

            Assert.Equal(1, y);
        }

        [Fact]
        public void WriteBarrier_EnsuresMemoryOrdering()
        {
            // Test that WriteBarrier can be called multiple times without issues
            int x = 0;
            int y = 0;

            x = 1;
            Volatile.WriteBarrier();
            y = x;

            Assert.Equal(1, y);
        }

        [Fact]
        public void ReadBarrier_WithVolatileReads()
        {
            // Test ReadBarrier in conjunction with Volatile.Read
            int value = 42;
            Volatile.ReadBarrier();
            int result = Volatile.Read(ref value);
            Assert.Equal(42, result);
        }

        [Fact]
        public void WriteBarrier_WithVolatileWrites()
        {
            // Test WriteBarrier in conjunction with Volatile.Write
            int value = 0;
            Volatile.Write(ref value, 42);
            Volatile.WriteBarrier();
            Assert.Equal(42, value);
        }

        [Fact]
        public void ReadBarrier_MultipleCallsSequence()
        {
            // Test multiple sequential calls to ReadBarrier
            Volatile.ReadBarrier();
            Volatile.ReadBarrier();
            Volatile.ReadBarrier();
        }

        [Fact]
        public void WriteBarrier_MultipleCallsSequence()
        {
            // Test multiple sequential calls to WriteBarrier
            Volatile.WriteBarrier();
            Volatile.WriteBarrier();
            Volatile.WriteBarrier();
        }

        [Fact]
        public void ReadWriteBarrier_Interleaved()
        {
            // Test interleaved calls to both barriers
            int x = 0;
            int y = 0;

            x = 1;
            Volatile.WriteBarrier();
            y = 2;
            Volatile.ReadBarrier();
            int sum = x + y;

            Assert.Equal(3, sum);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action ReadBarrierDelegate() => Volatile.ReadBarrier;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action WriteBarrierDelegate() => Volatile.WriteBarrier;

        [Fact]
        public void ReadBarrier_CanBeCalledViaDelegate()
        {
            // Verify ReadBarrier can be called through a delegate
            ReadBarrierDelegate()();
        }

        [Fact]
        public void WriteBarrier_CanBeCalledViaDelegate()
        {
            // Verify WriteBarrier can be called through a delegate
            WriteBarrierDelegate()();
        }

        [Fact]
        public void ReadBarrier_CanBeCalledViaReflection()
        {
            // Verify ReadBarrier can be called via reflection
            MethodInfo method = typeof(Volatile).GetMethod(nameof(Volatile.ReadBarrier), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            method.Invoke(null, null);
        }

        [Fact]
        public void WriteBarrier_CanBeCalledViaReflection()
        {
            // Verify WriteBarrier can be called via reflection
            MethodInfo method = typeof(Volatile).GetMethod(nameof(Volatile.WriteBarrier), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            method.Invoke(null, null);
        }
    }
}
