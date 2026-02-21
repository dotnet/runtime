// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Threading.Tests
{
    public class VolatileTests
    {
        [Fact]
        public void ReadBarrier_DoesNotThrow()
        {
            Volatile.ReadBarrier();
        }

        [Fact]
        public void WriteBarrier_DoesNotThrow()
        {
            Volatile.WriteBarrier();
        }

        [Fact]
        public void BarriersAndVolatileOperations()
        {
            // Test ReadBarrier, WriteBarrier, and other Volatile APIs
            int value1 = 0;
            int value2 = 0;
            long value3 = 0;

            // Test direct calls
            Volatile.ReadBarrier();
            Volatile.WriteBarrier();

            // Test with Volatile.Read and Volatile.Write
            Volatile.Write(ref value1, 42);
            Volatile.WriteBarrier();
            Assert.Equal(42, value1);

            Volatile.ReadBarrier();
            int result1 = Volatile.Read(ref value1);
            Assert.Equal(42, result1);

            // Test multiple sequential calls
            Volatile.ReadBarrier();
            Volatile.ReadBarrier();
            Volatile.WriteBarrier();
            Volatile.WriteBarrier();

            // Test interleaved barriers with Volatile operations
            Volatile.Write(ref value2, 100);
            Volatile.WriteBarrier();
            Volatile.ReadBarrier();
            int result2 = Volatile.Read(ref value2);
            Assert.Equal(100, result2);

            // Test with different types
            Volatile.Write(ref value3, 123456789L);
            Volatile.ReadBarrier();
            long result3 = Volatile.Read(ref value3);
            Assert.Equal(123456789L, result3);
        }

        [Fact]
        public void BarriersViaReflection()
        {
            MethodInfo readBarrierMethod = typeof(Volatile).GetMethod(nameof(Volatile.ReadBarrier), BindingFlags.Public | BindingFlags.Static);
            MethodInfo writeBarrierMethod = typeof(Volatile).GetMethod(nameof(Volatile.WriteBarrier), BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(readBarrierMethod);
            Assert.NotNull(writeBarrierMethod);
            readBarrierMethod.Invoke(null, null);
            writeBarrierMethod.Invoke(null, null);
        }
    }
}
