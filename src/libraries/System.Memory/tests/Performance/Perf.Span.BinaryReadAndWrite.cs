// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;
using System.Net;

using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Binary.Tests
{
    public class BinaryReadAndWriteTests
    {
        private const int InnerCount = 100000;

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructAndReverseBE()
        {
            Span<byte> spanBE = TestHelpers.GetSpanBE();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = ReadMachineEndian<TestHelpers.TestStructExplicit>(spanBE);
                        if (BitConverter.IsLittleEndian)
                        {
                            readStruct.S0 = ReverseEndianness(readStruct.S0);
                            readStruct.I0 = ReverseEndianness(readStruct.I0);
                            readStruct.L0 = ReverseEndianness(readStruct.L0);
                            readStruct.US0 = ReverseEndianness(readStruct.US0);
                            readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                            readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                            readStruct.S1 = ReverseEndianness(readStruct.S1);
                            readStruct.I1 = ReverseEndianness(readStruct.I1);
                            readStruct.L1 = ReverseEndianness(readStruct.L1);
                            readStruct.US1 = ReverseEndianness(readStruct.US1);
                            readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                            readStruct.UL1 = ReverseEndianness(readStruct.UL1);
                        }
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructAndReverseLE()
        {
            Span<byte> spanLE = TestHelpers.GetSpanLE();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = ReadMachineEndian<TestHelpers.TestStructExplicit>(spanLE);
                        if (!BitConverter.IsLittleEndian)
                        {
                            readStruct.S0 = ReverseEndianness(readStruct.S0);
                            readStruct.I0 = ReverseEndianness(readStruct.I0);
                            readStruct.L0 = ReverseEndianness(readStruct.L0);
                            readStruct.US0 = ReverseEndianness(readStruct.US0);
                            readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                            readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                            readStruct.S1 = ReverseEndianness(readStruct.S1);
                            readStruct.I1 = ReverseEndianness(readStruct.I1);
                            readStruct.L1 = ReverseEndianness(readStruct.L1);
                            readStruct.US1 = ReverseEndianness(readStruct.US1);
                            readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                            readStruct.UL1 = ReverseEndianness(readStruct.UL1);
                        }
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructFieldByFieldBE()
        {
            Span<byte> spanBE = TestHelpers.GetSpanBE();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = new TestHelpers.TestStructExplicit
                        {
                            S0 = ReadInt16BigEndian(spanBE),
                            I0 = ReadInt32BigEndian(spanBE.Slice(2)),
                            L0 = ReadInt64BigEndian(spanBE.Slice(6)),
                            US0 = ReadUInt16BigEndian(spanBE.Slice(14)),
                            UI0 = ReadUInt32BigEndian(spanBE.Slice(16)),
                            UL0 = ReadUInt64BigEndian(spanBE.Slice(20)),
                            S1 = ReadInt16BigEndian(spanBE.Slice(28)),
                            I1 = ReadInt32BigEndian(spanBE.Slice(30)),
                            L1 = ReadInt64BigEndian(spanBE.Slice(34)),
                            US1 = ReadUInt16BigEndian(spanBE.Slice(42)),
                            UI1 = ReadUInt32BigEndian(spanBE.Slice(44)),
                            UL1 = ReadUInt64BigEndian(spanBE.Slice(48))
                        };
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructFieldByFieldLE()
        {
            Span<byte> spanLE = TestHelpers.GetSpanLE();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = new TestHelpers.TestStructExplicit
                        {
                            S0 = ReadInt16LittleEndian(spanLE),
                            I0 = ReadInt32LittleEndian(spanLE.Slice(2)),
                            L0 = ReadInt64LittleEndian(spanLE.Slice(6)),
                            US0 = ReadUInt16LittleEndian(spanLE.Slice(14)),
                            UI0 = ReadUInt32LittleEndian(spanLE.Slice(16)),
                            UL0 = ReadUInt64LittleEndian(spanLE.Slice(20)),
                            S1 = ReadInt16LittleEndian(spanLE.Slice(28)),
                            I1 = ReadInt32LittleEndian(spanLE.Slice(30)),
                            L1 = ReadInt64LittleEndian(spanLE.Slice(34)),
                            US1 = ReadUInt16LittleEndian(spanLE.Slice(42)),
                            UI1 = ReadUInt32LittleEndian(spanLE.Slice(44)),
                            UL1 = ReadUInt64LittleEndian(spanLE.Slice(48))
                        };
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructFieldByFieldUsingBitConverterLE()
        {
            Span<byte> spanLE = TestHelpers.GetSpanLE();
            byte[] arrayLE = spanLE.ToArray();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = new TestHelpers.TestStructExplicit
                        {
                            S0 = BitConverter.ToInt16(arrayLE, 0),
                            I0 = BitConverter.ToInt32(arrayLE, 2),
                            L0 = BitConverter.ToInt64(arrayLE, 6),
                            US0 = BitConverter.ToUInt16(arrayLE, 14),
                            UI0 = BitConverter.ToUInt32(arrayLE, 16),
                            UL0 = BitConverter.ToUInt64(arrayLE, 20),
                            S1 = BitConverter.ToInt16(arrayLE, 28),
                            I1 = BitConverter.ToInt32(arrayLE, 30),
                            L1 = BitConverter.ToInt64(arrayLE, 34),
                            US1 = BitConverter.ToUInt16(arrayLE, 42),
                            UI1 = BitConverter.ToUInt32(arrayLE, 44),
                            UL1 = BitConverter.ToUInt64(arrayLE, 48),
                        };
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ReadStructFieldByFieldUsingBitConverterBE()
        {
            Span<byte> spanBE = TestHelpers.GetSpanBE();
            byte[] arrayBE = spanBE.ToArray();

            var readStruct = new TestHelpers.TestStructExplicit();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        readStruct = new TestHelpers.TestStructExplicit
                        {
                            S0 = BitConverter.ToInt16(arrayBE, 0),
                            I0 = BitConverter.ToInt32(arrayBE, 2),
                            L0 = BitConverter.ToInt64(arrayBE, 6),
                            US0 = BitConverter.ToUInt16(arrayBE, 14),
                            UI0 = BitConverter.ToUInt32(arrayBE, 16),
                            UL0 = BitConverter.ToUInt64(arrayBE, 20),
                            S1 = BitConverter.ToInt16(arrayBE, 28),
                            I1 = BitConverter.ToInt32(arrayBE, 30),
                            L1 = BitConverter.ToInt64(arrayBE, 34),
                            US1 = BitConverter.ToUInt16(arrayBE, 42),
                            UI1 = BitConverter.ToUInt32(arrayBE, 44),
                            UL1 = BitConverter.ToUInt64(arrayBE, 48),
                        };
                        if (BitConverter.IsLittleEndian)
                        {
                            readStruct.S0 = ReverseEndianness(readStruct.S0);
                            readStruct.I0 = ReverseEndianness(readStruct.I0);
                            readStruct.L0 = ReverseEndianness(readStruct.L0);
                            readStruct.US0 = ReverseEndianness(readStruct.US0);
                            readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                            readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                            readStruct.S1 = ReverseEndianness(readStruct.S1);
                            readStruct.I1 = ReverseEndianness(readStruct.I1);
                            readStruct.L1 = ReverseEndianness(readStruct.L1);
                            readStruct.US1 = ReverseEndianness(readStruct.US1);
                            readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                            readStruct.UL1 = ReverseEndianness(readStruct.UL1);
                        }
                    }
                }
            }

            Assert.Equal(TestHelpers.s_testExplicitStruct, readStruct);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void MeasureReverseEndianness()
        {
            var myArray = new int[1000];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < myArray.Length; j++)
                        {
                            myArray[j] = ReverseEndianness(myArray[j]);
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void MeasureReverseUsingNtoH()
        {
            var myArray = new int[1000];

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < myArray.Length; j++)
                        {
                            myArray[j] = IPAddress.NetworkToHostOrder(myArray[j]);
                        }
                    }
                }
            }
        }
    }
}
