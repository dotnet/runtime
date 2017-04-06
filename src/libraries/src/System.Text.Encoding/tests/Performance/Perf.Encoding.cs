// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;

namespace System.Text.Tests
{
    public class Perf_Encoding
    {
        public static IEnumerable<object[]> EncodingSizeData()
        {
            int[] sizes = new int[] { 16, 32, 64, 128, 256, 512, 10000, 1000000 };
            string[] encs = new string[] { "utf-8", "ascii" };
            foreach (int size in sizes)
                foreach (string enc in encs)
                    yield return new object[] { size, enc };
        }

        [Benchmark]
        [MemberData(nameof(EncodingSizeData))]
        public void GetBytes(int size, string encName)
        {
            const int innerIterations = 100;
            Encoding enc = Encoding.GetEncoding(encName);
            PerfUtils utils = new PerfUtils();
            string toEncode = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                        enc.GetBytes(toEncode); enc.GetBytes(toEncode); enc.GetBytes(toEncode);
                    }
        }

        [Benchmark]
        [MemberData(nameof(EncodingSizeData))]
        public void GetString(int size, string encName)
        {
            const int innerIterations = 100;
            Encoding enc = Encoding.GetEncoding(encName);
            PerfUtils utils = new PerfUtils();
            byte[] bytes = enc.GetBytes(utils.CreateString(size));
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
                        enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
                        enc.GetString(bytes); enc.GetString(bytes); enc.GetString(bytes);
                    }
        }

        [Benchmark]
        [MemberData(nameof(EncodingSizeData))]
        public void GetChars(int size, string encName)
        {
            const int innerIterations = 100;
            Encoding enc = Encoding.GetEncoding(encName);
            PerfUtils utils = new PerfUtils();
            byte[] bytes = enc.GetBytes(utils.CreateString(size));
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
                        enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
                        enc.GetChars(bytes); enc.GetChars(bytes); enc.GetChars(bytes);
                    }
        }

        [Benchmark]
        [MemberData(nameof(EncodingSizeData))]
        public void GetEncoder(int size, string encName)
        {
            const int innerIterations = 10000;
            Encoding enc = Encoding.GetEncoding(encName);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
                        enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
                        enc.GetEncoder(); enc.GetEncoder(); enc.GetEncoder();
                    }
        }

        [Benchmark]
        [MemberData(nameof(EncodingSizeData))]
        public void GetByteCount(int size, string encName)
        {
            const int innerIterations = 100;
            Encoding enc = Encoding.GetEncoding(encName);
            PerfUtils utils = new PerfUtils();
            char[] chars = utils.CreateString(size).ToCharArray();
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
                        enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
                        enc.GetByteCount(chars); enc.GetByteCount(chars); enc.GetByteCount(chars);
                    }
        }
    }
}
