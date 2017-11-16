// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System
{
    public class Perf_Convert
    {
        private byte[] InitializeBinaryDataCollection(int size)
        {
            var random = new Random(30000);
            byte[] binaryData = new byte[size];
            random.NextBytes(binaryData);

            return binaryData;
        }

        [Benchmark(InnerIterationCount = 20000000)]
        public void GetTypeCode()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            object value = "Hello World!";

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        Convert.GetTypeCode(value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 6000000)]
        public void ChangeType()
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            object value = 1000;
            Type type = typeof(string);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        Convert.ChangeType(value, type);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100000)]
        [InlineData(1024, Base64FormattingOptions.InsertLineBreaks)]
        [InlineData(1024, Base64FormattingOptions.None)]
        public void ToBase64CharArray(int binaryDataSize, Base64FormattingOptions formattingOptions)
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            byte[] binaryData = InitializeBinaryDataCollection(binaryDataSize);
            int arraySize = Convert.ToBase64String(binaryData, formattingOptions).Length;
            char[] base64CharArray = new char[arraySize];

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        Convert.ToBase64CharArray(binaryData, 0, binaryDataSize, base64CharArray, 0, formattingOptions);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100000)]
        [InlineData(Base64FormattingOptions.InsertLineBreaks)]
        [InlineData(Base64FormattingOptions.None)]
        public void ToBase64String(Base64FormattingOptions formattingOptions)
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;
            byte[] binaryData = InitializeBinaryDataCollection(1024);

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        Convert.ToBase64String(binaryData, formattingOptions);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 400000)]
        [InlineData("Fri, 27 Feb 2009 03:11:21 GMT")]
        [InlineData("Thursday, February 26, 2009")]
        [InlineData("February 26, 2009")]
        [InlineData("12/31/1999 11:59:59 PM")]
        [InlineData("12/31/1999")]
        public void ToDateTime_String(string value)
        {
            int innerIterationCount = (int)Benchmark.InnerIterationCount;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterationCount; i++)
                    {
                        Convert.ToDateTime(value);
                    }
                }
            }
        }
    }
}
