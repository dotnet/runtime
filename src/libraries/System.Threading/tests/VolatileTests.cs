// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Threading.Tests
{
    public class VolatileTests
    {
        [Fact]
        public void Barriers_DoNotThrow()
        {
            Volatile.ReadBarrier();
            Volatile.WriteBarrier();
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

        [Fact]
        public void BarriersAndVolatileOperations()
        {
            int value1 = 0;
            int value2 = 0;
            long value3 = 0;

            Volatile.ReadBarrier();
            Volatile.WriteBarrier();

            Volatile.Write(ref value1, 42);
            Volatile.WriteBarrier();
            Assert.Equal(42, value1);

            Volatile.ReadBarrier();
            int result1 = Volatile.Read(ref value1);
            Assert.Equal(42, result1);

            Volatile.ReadBarrier();
            Volatile.ReadBarrier();
            Volatile.WriteBarrier();
            Volatile.WriteBarrier();

            Volatile.Write(ref value2, 100);
            Volatile.WriteBarrier();
            Volatile.ReadBarrier();
            int result2 = Volatile.Read(ref value2);
            Assert.Equal(100, result2);

            Volatile.Write(ref value3, 123456789L);
            Volatile.ReadBarrier();
            long result3 = Volatile.Read(ref value3);
            Assert.Equal(123456789L, result3);
        }
    }
}
