// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics.Tracing;
using System.Threading;
using Xunit;

namespace System.Buffers.ArrayPool.Tests
{
    public partial class ArrayPoolUnitTests
    {
        private const int MaxEventWaitTimeoutInMs = 200;

        private struct TestStruct
        {
            internal string InternalRef;
        }
            
        /*
            NOTE - due to test parallelism and sharing, use an instance pool for testing unless necessary
        */ 
        [Fact]
        public static void SharedInstanceCreatesAnInstanceOnFirstCall()
        {
            Assert.NotNull(ArrayPool<byte>.Shared);
        }

        [Fact]
        public static void SharedInstanceOnlyCreatesOneInstanceOfOneTypep()
        {
            ArrayPool<byte> instance = ArrayPool<byte>.Shared;
            Assert.Same(instance, ArrayPool<byte>.Shared);
        }

        [Fact]
        public static void CreateWillCreateMultipleInstancesOfTheSameType()
        {
            Assert.NotSame(ArrayPool<byte>.Create(), ArrayPool<byte>.Create());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public static void CreatingAPoolWithInvalidArrayCountThrows(int length)
        {
            Assert.Throws<ArgumentOutOfRangeException>("arraysPerBucket", () => ArrayPool<byte>.Create(maxArraysPerBucket: length, maxArrayLength: 16));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public static void CreatingAPoolWithInvalidMaximumArraySizeThrows(int length)
        {
            Assert.Throws<ArgumentOutOfRangeException>("maxLength", () => ArrayPool<byte>.Create(maxArrayLength: length, maxArraysPerBucket: 1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public static void RentingWithInvalidLengthThrows(int length)
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create();
            Assert.Throws<ArgumentOutOfRangeException>("minimumLength", () => pool.Rent(length));
        }

        [Fact]
        public static void RentingAValidSizeArraySucceeds()
        {
            Assert.NotNull(ArrayPool<byte>.Create().Rent(100));
        }

        [Fact]
        public static void RentingMultipleArraysGivesBackDifferentInstances()
        {
            ArrayPool<byte> instance = ArrayPool<byte>.Create(maxArraysPerBucket: 2, maxArrayLength: 16);
            Assert.NotSame(instance.Rent(100), instance.Rent(100));
        }

        [Fact]
        public static void RentingMoreArraysThanSpecifiedInCreateWillStillSucceed()
        {
            ArrayPool<byte> instance = ArrayPool<byte>.Create(maxArraysPerBucket: 1, maxArrayLength: 16);
            Assert.NotNull(instance.Rent(100));
            Assert.NotNull(instance.Rent(100));
        }

        [Fact]
        public static void RentCanReturnBiggerArraySizeThanRequested()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArraysPerBucket: 1, maxArrayLength: 32);
            byte[] rented = pool.Rent(27);
            Assert.NotNull(rented);
            Assert.Equal(rented.Length, 32);
        }

        [Fact]
        public static void RentingAnArrayWithLengthGreaterThanSpecifiedInCreateStillSucceeds()
        {
            Assert.NotNull(ArrayPool<byte>.Create(maxArrayLength: 100, maxArraysPerBucket: 1).Rent(200));
        }

        [Fact]
        public static void CallingReturnBufferWithNullBufferThrows()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create();
            Assert.Throws<ArgumentNullException>("buffer", () => pool.Return(null));
        }

        private static void FillArray(byte[] buffer)
        {
            for (byte i = 0; i < buffer.Length; i++)
                buffer[i] = i;
        }

        private static void CheckFilledArray(byte[] buffer, Action<byte, byte> assert)
        {
            for (byte i = 0; i < buffer.Length; i++)
            {
                assert(buffer[i], i);
            }
        }

        [Fact]
        public static void CallingReturnWithoutClearingDoesNotClearTheBuffer()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create();
            byte[] buffer = pool.Rent(4);
            FillArray(buffer);
            pool.Return(buffer, clearArray: false);
            CheckFilledArray(buffer, (byte b1, byte b2) => Assert.Equal(b1, b2));
        }

        [Fact]
        public static void CallingReturnWithClearingDoesClearTheBuffer()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create();
            byte[] buffer = pool.Rent(4);
            FillArray(buffer);

            // Note - yes this is bad to hold on to the old instance but we need to validate the contract
            pool.Return(buffer, clearArray: true);
            CheckFilledArray(buffer, (byte b1, byte b2) => Assert.Equal(b1, default(byte)));
        }

        [Fact]
        public static void CallingReturnOnReferenceTypeArrayDoesNotClearTheArray()
        {
            ArrayPool<string> pool = ArrayPool<string>.Create();
            string[] array = pool.Rent(2);
            array[0] = "foo";
            array[1] = "bar";
            pool.Return(array, clearArray: false);
            Assert.NotNull(array[0]);
            Assert.NotNull(array[1]);
        }

        [Fact]
        public static void CallingReturnOnReferenceTypeArrayAndClearingSetsTypesToNull()
        {
            ArrayPool<string> pool = ArrayPool<string>.Create();
            string[] array = pool.Rent(2);
            array[0] = "foo";
            array[1] = "bar";
            pool.Return(array, clearArray: true);
            Assert.Null(array[0]);
            Assert.Null(array[1]);
        }

        [Fact]
        public static void CallingReturnOnValueTypeWithInternalReferenceTypesAndClearingSetsValueTypeToDefault()
        {
            ArrayPool<TestStruct> pool = ArrayPool<TestStruct>.Create();
            TestStruct[] array = pool.Rent(2);
            array[0].InternalRef = "foo";
            array[1].InternalRef = "bar";
            pool.Return(array, clearArray: true);
            Assert.Equal(array[0], default(TestStruct));
            Assert.Equal(array[1], default(TestStruct));
        }

        [Fact]
        public static void TakingAllBuffersFromABucketPlusAnAllocatedOneShouldAllowReturningAllBuffers()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            byte[] rented = pool.Rent(16);
            byte[] allocated = pool.Rent(16);
            pool.Return(rented);
            pool.Return(allocated);
        }

        [Fact]
        public static void NewDefaultArrayPoolWithSmallBufferSizeRoundsToOurSmallestSupportedSize()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 8, maxArraysPerBucket: 1);
            byte[] rented = pool.Rent(8);
            Assert.True(rented.Length == 16);
        }

        [Fact]
        public static void ReturningABufferGreaterThanMaxSizeDoesNotThrow()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            byte[] rented = pool.Rent(32);
            pool.Return(rented);
        }

        [Fact]
        public static void RentingAllBuffersAndCallingRentAgainWillAllocateBufferAndReturnIt()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            byte[] rented1 = pool.Rent(16);
            byte[] rented2 = pool.Rent(16);
            Assert.NotNull(rented1);
            Assert.NotNull(rented2);
        }

        [Fact]
        public static void RentingReturningThenRentingABufferShouldNotAllocate()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            byte[] bt = pool.Rent(16);
            int id = bt.GetHashCode();
            pool.Return(bt);
            bt = pool.Rent(16);
            Assert.Equal(id, bt.GetHashCode());
        }

        [Fact]
        public static void CanRentManySizedBuffers()
        {
            var pool = ArrayPool<byte>.Create();
            for (int i = 1; i < 10000; i++)
            {
                byte[] buffer = pool.Rent(i);
                Assert.Equal(i <= 16 ? 16 : RoundUpToPowerOf2(i), buffer.Length);
                pool.Return(buffer);
            }
        }

        private static int RoundUpToPowerOf2(int i)
        {
            // http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            --i;
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }

        [Theory]
        [InlineData(1, 16)]
        [InlineData(15, 16)]
        [InlineData(16, 16)]
        [InlineData(1023, 1024)]
        [InlineData(1024, 1024)]
        [InlineData(4096, 4096)]
        [InlineData(1024 * 1024, 1024 * 1024)]
        [InlineData(1024 * 1024 + 1, 1024 * 1024 + 1)]
        [InlineData(1024 * 1024 * 2, 1024 * 1024 * 2)]
        public static void RentingSpecificLengthsYieldsExpectedLengths(int requestedMinimum, int expectedLength)
        {
            byte[] buffer = ArrayPool<byte>.Create().Rent(requestedMinimum);
            Assert.NotNull(buffer);
            Assert.Equal(expectedLength, buffer.Length);
        }

        [Fact]
        public static void RentingAfterPoolExhaustionReturnsSizeForCorrespondingBucket()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 64, maxArraysPerBucket: 1);
            Assert.Equal(16, pool.Rent(15).Length);
            Assert.Equal(32, pool.Rent(15).Length);
            Assert.Equal(64, pool.Rent(15).Length);
            Assert.Equal(16, pool.Rent(15).Length);
        }

        private static void ActionFiresSpecificEvent(Action body, int eventId, AutoResetEvent are)
        {
            using (TestEventListener listener = new TestEventListener("System.Buffers.BufferPoolEventSource", EventLevel.Verbose))
            {
                listener.RunWithCallback((EventWrittenEventArgs e) =>
                {
                    if (e.EventId == eventId)
                        are.Set();
                }, body);
            }
        }

        [Fact]
        public static void RentBufferFiresRentedDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() =>
            {
                byte[] bt = pool.Rent(16);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 1, are);
        }

        [Fact]
        public static void ReturnBufferFiresDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() => 
            {
                byte[] bt = pool.Rent(16);
                pool.Return(bt);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 3, are);
        }

        [Fact]
        public static void FirstCallToRentBufferFiresCreatedDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() => 
            {
                byte[] bt = pool.Rent(16);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 2, are);
        }

        [Fact]
        public static void AllocatingABufferDueToBucketExhaustionFiresDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() => 
            {
                byte[] bt = pool.Rent(16);
                byte[] bt2 = pool.Rent(16);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 2, are);
        }

        [Fact]
        public static void RentingBufferOverConfiguredMaximumSizeFiresDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() => 
            {
                byte[] bt = pool.Rent(64);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 2, are);
        }
        
        [Fact]
        public static void ExhaustingBufferBucketFiresDiagnosticEvent()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            AutoResetEvent are = new AutoResetEvent(false);

            ActionFiresSpecificEvent(() => 
            {
                byte[] bt = pool.Rent(16);
                byte[] bt2 = pool.Rent(16);
                Assert.True(are.WaitOne(MaxEventWaitTimeoutInMs));
            }, 4, are);
        }

        [Fact]
        public static void ReturningANonPooledBufferOfDifferentSizeToThePoolThrows()
        {
            ArrayPool<byte> pool = ArrayPool<byte>.Create(maxArrayLength: 16, maxArraysPerBucket: 1);
            byte[] buffer = pool.Rent(15);
            Assert.Throws<ArgumentException>("buffer", () => pool.Return(new byte[1]));
            buffer = pool.Rent(15);
            Assert.Equal(buffer.Length, 16);
        }
    }
}
