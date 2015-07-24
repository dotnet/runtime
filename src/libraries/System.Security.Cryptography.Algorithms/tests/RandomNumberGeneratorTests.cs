// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.RNG.Tests
{
    public class RandomNumberGeneratorTests
    {
        [Fact]
        public static void DifferentSequential_10()
        {
            DifferentSequential(10);
        }

        [Fact]
        public static void DifferentSequential_256()
        {
            DifferentSequential(256);
        }

        [Fact]
        public static void DifferentSequential_65536()
        {
            DifferentSequential(65536);
        }

        [Fact]
        public static void DifferentParallel_10()
        {
            DifferentParallel(10);
        }

        [Fact]
        public static void DifferentParallel_256()
        {
            DifferentParallel(256);
        }

        [Fact]
        public static void DifferentParallel_65536()
        {
            DifferentParallel(65536);
        }

        [Fact]
        public static void NeutralParity()
        {
            byte[] random = new byte[2048];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }

            AssertNeutralParity(random);
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
                // Middle ground is to assert that each of the chunks has neutral(ish) bit parity.
                AssertNeutralParity(taskArrays[i]);
            }
        }

        private static void DifferentSequential(int arraySize)
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

        private static void DifferentParallel(int arraySize)
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

        private static void AssertNeutralParity(byte[] random)
        {
            int oneCount = 0;
            int zeroCount = 0;

            for (int i = 0; i < random.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (((random[i] >> j) & 1) == 1)
                    {
                        oneCount++;
                    }
                    else
                    {
                        zeroCount++;
                    }
                }
            }

            int totalCount = zeroCount + oneCount;
            float bitDifference = (float)Math.Abs(zeroCount - oneCount) / totalCount;

            // Over the long run there should be about as many 1s as 0s.
            // This isn't a guarantee, just a statistical observation.
            // Allow a 7% tolerance band before considering it to have gotten out of hand.
            const double AllowedTolerance = 0.07;
            Assert.True(bitDifference < AllowedTolerance, 
                "Expected bitDifference < " + AllowedTolerance + ", got " + bitDifference + ".");
        }
    }
}
