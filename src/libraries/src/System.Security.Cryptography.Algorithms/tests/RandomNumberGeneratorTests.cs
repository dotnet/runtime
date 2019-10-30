// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.RNG.Tests
{
    public partial class RandomNumberGeneratorTests
    {
        [Fact]
        public static void RandomDistribution()
        {
            byte[] random = new byte[2048];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            RandomDataGenerator.VerifyRandomDistribution(random);
        }

        [Fact]
        public static void IdempotentDispose()
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            for (int i = 0; i < 10; i++)
            {
                rng.Dispose();
            }
        }

        [Fact]
        public static void NullInput()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                Assert.Throws<ArgumentNullException>(() => rng.GetBytes(null));
            }
        }

        [Fact]
        public static void ZeroLengthInput()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                // While this will do nothing, it's not something that throws.
                rng.GetBytes(Array.Empty<byte>());
            }
        }

        [Fact]
        public static void ConcurrentAccess()
        {
            const int ParallelTasks = 3;
            const int PerTaskIterationCount = 20;
            const int RandomSize = 1024;

            Task[] tasks = new Task[ParallelTasks];
            byte[][] taskArrays = new byte[ParallelTasks][];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            using (ManualResetEvent sync = new ManualResetEvent(false))
            {
                for (int iTask = 0; iTask < ParallelTasks; iTask++)
                {
                    taskArrays[iTask] = new byte[RandomSize];
                    byte[] taskLocal = taskArrays[iTask];

                    tasks[iTask] = Task.Run(
                        () =>
                        {
                            sync.WaitOne();

                            for (int i = 0; i < PerTaskIterationCount; i++)
                            {
                                rng.GetBytes(taskLocal);
                            }
                        });
                }

                // Ready? Set() Go!
                sync.Set();
                Task.WaitAll(tasks);
            }

            for (int i = 0; i < ParallelTasks; i++)
            {
                // The Real test would be to ensure independence of data, but that's difficult.
                // The other end of the spectrum is to test that they aren't all just new byte[RandomSize].
                // Middle ground is to assert that each of the chunks has random data.
                RandomDataGenerator.VerifyRandomDistribution(taskArrays[i]);
            }
        }

        [Fact]
        public static void GetNonZeroBytes_Array()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => rng.GetNonZeroBytes(null));

                // Array should not have any zeros
                byte[] rand = new byte[65536];
                rng.GetNonZeroBytes(rand);
                Assert.Equal(-1, Array.IndexOf<byte>(rand, 0));
            }
        }

        [Fact]
        public static void GetBytes_Offset()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] rand = new byte[400];

                // Set canary bytes
                rand[99] = 77;
                rand[399] = 77;

                rng.GetBytes(rand, 100, 200);

                // Array should not have been touched outside of 100-299
                Assert.Equal(99, Array.IndexOf<byte>(rand, 77, 0));
                Assert.Equal(399, Array.IndexOf<byte>(rand, 77, 300));

                // Ensure 100-300 has random bytes; not likely to ever fail here by chance (256^200)
                Assert.True(rand.Skip(100).Take(200).Sum(b => b) > 0);
            }
        }

        [Fact]
        public static void GetBytes_Array_Offset_ZeroCount()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] rand = new byte[1] { 1 };

                // A count of 0 should not do anything
                rng.GetBytes(rand, 0, 0);
                Assert.Equal(1, rand[0]);

                // Having an offset of Length is allowed if count is 0
                rng.GetBytes(rand, rand.Length, 0);
                Assert.Equal(1, rand[0]);

                // Zero-length array should not throw
                rand = Array.Empty<byte>();
                rng.GetBytes(rand, 0, 0);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(256)]
        [InlineData(65536)]
        public static void DifferentSequential_Array(int arraySize)
        {
            // Ensure that the RNG doesn't produce a stable set of data.
            byte[] first = new byte[arraySize];
            byte[] second = new byte[arraySize];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(first);
                rng.GetBytes(second);
            }

            // Random being random, there is a chance that it could produce the same sequence.
            // The smallest test case that we have is 10 bytes.
            // The probability that they are the same, given a Truly Random Number Generator is:
            // Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
            // = 1/256 * 1/256 * ... * 1/256
            // = 1/(256^10)
            // = 1/1,208,925,819,614,629,174,706,176
            Assert.NotEqual(first, second);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(256)]
        [InlineData(65536)]
        public static void DifferentParallel(int arraySize)
        {
            // Ensure that two RNGs don't produce the same data series (such as being implemented via new Random(1)).
            byte[] first = new byte[arraySize];
            byte[] second = new byte[arraySize];

            using (RandomNumberGenerator rng1 = RandomNumberGenerator.Create())
            using (RandomNumberGenerator rng2 = RandomNumberGenerator.Create())
            {
                rng1.GetBytes(first);
                rng2.GetBytes(second);
            }

            // Random being random, there is a chance that it could produce the same sequence.
            // The smallest test case that we have is 10 bytes.
            // The probability that they are the same, given a Truly Random Number Generator is:
            // Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
            // = 1/256 * 1/256 * ... * 1/256
            // = 1/(256^10)
            // = 1/1,208,925,819,614,629,174,706,176
            Assert.NotEqual(first, second);
        }

        [Fact]
        public static void GetBytes_InvalidArgs()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => rng.GetNonZeroBytes(null));
                GetBytes_InvalidArgs(rng);
            }
        }

        [Fact]
        public static void GetBytes_InvalidArgs_Base()
        {
            using (var rng = new RandomNumberGeneratorMininal())
            {
                Assert.Throws<NotImplementedException>(() => rng.GetNonZeroBytes(null));
                GetBytes_InvalidArgs(rng);
            }
        }

        private static void GetBytes_InvalidArgs(RandomNumberGenerator rng)
        {
            AssertExtensions.Throws<ArgumentNullException>("data", () => rng.GetBytes(null, 0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => rng.GetBytes(Array.Empty<byte>(), -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => rng.GetBytes(Array.Empty<byte>(), 0, -1));
            AssertExtensions.Throws<ArgumentException>(null, () => rng.GetBytes(Array.Empty<byte>(), 0, 1));
            // GetBytes(null) covered in test NullInput()
        }

        private class RandomNumberGeneratorMininal : RandomNumberGenerator
        {
            public override void GetBytes(byte[] data)
            {
                // Empty; don't throw NotImplementedException
            }
        }
    }
}
