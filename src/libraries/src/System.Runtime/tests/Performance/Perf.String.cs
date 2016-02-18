// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Runtime.Tests
{
    public class Perf_String
    {
        public static IEnumerable<object[]> TestStringSizes()
        {
            yield return new object[] { 10 };
            yield return new object[] { 100 };
            yield return new object[] { 1000 };
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void GetChars(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        testString.ToCharArray(); testString.ToCharArray(); testString.ToCharArray();
                        testString.ToCharArray(); testString.ToCharArray(); testString.ToCharArray();
                        testString.ToCharArray(); testString.ToCharArray(); testString.ToCharArray();
                    }
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Concat_str_str(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString1 = utils.CreateString(size);
            string testString2 = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        string.Concat(testString1, testString2);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Concat_str_str_str(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString1 = utils.CreateString(size);
            string testString2 = utils.CreateString(size);
            string testString3 = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        string.Concat(testString1, testString2, testString3);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Concat_str_str_str_str(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString1 = utils.CreateString(size);
            string testString2 = utils.CreateString(size);
            string testString3 = utils.CreateString(size);
            string testString4 = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        string.Concat(testString1, testString2, testString3, testString4);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Contains(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            string subString = testString.Substring(testString.Length / 2, testString.Length / 4);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Contains(subString);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Equals(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString1 = utils.CreateString(size);
            string testString2 = new string(testString1.ToCharArray());
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString1.Equals(testString2);
        }

        [Benchmark]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(100)]
        public void Format(int numberOfObjects)
        {
            PerfUtils utils = new PerfUtils();
            // Setup the format string and the list of objects to format
            StringBuilder formatter = new StringBuilder();
            List<string> objects = new List<string>();
            for (int i = 0; i < numberOfObjects; i++)
            {
                formatter.Append("%s, ");
                objects.Add(utils.CreateString(10));
            }
            string format = formatter.ToString();
            string[] objectArr = objects.ToArray();

            // Perform the actual formatting
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 5000; i++)
                        string.Format(format, objectArr);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void GetLength(int size)
        {
            PerfUtils utils = new PerfUtils();
            int result;
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        result = testString.Length; result = testString.Length; result = testString.Length;
                        result = testString.Length; result = testString.Length; result = testString.Length;
                        result = testString.Length; result = testString.Length; result = testString.Length;
                    }
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void op_Equality(int size)
        {
            PerfUtils utils = new PerfUtils();
            bool result;
            string testString1 = utils.CreateString(size);
            string testString2 = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                    {
                        result = testString1 == testString2; result = testString1 == testString2;
                        result = testString1 == testString2; result = testString1 == testString2;
                        result = testString1 == testString2; result = testString1 == testString2;
                    }
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Replace(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            string existingValue = testString.Substring(testString.Length / 2, 1);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Replace(existingValue, "1");
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Split(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            string existingValue = testString.Substring(testString.Length / 2, 1);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Split(existingValue);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void StartsWith(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            string subString = testString.Substring(0, testString.Length / 4);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.StartsWith(subString);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Substring_int(int size)
        {
            PerfUtils utils = new PerfUtils();
            int startIndex = size / 2;
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Substring(startIndex);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Substring_int_int(int size)
        {
            PerfUtils utils = new PerfUtils();
            int startIndex = size / 2;
            int length = size / 4;
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Substring(startIndex, length);
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void ToLower(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.ToLower();
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void ToUpper(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.ToUpper();
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Trim_WithWhitespace(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = "   " + utils.CreateString(size) + "   ";
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Trim();
        }

        [Benchmark]
        [MemberData(nameof(TestStringSizes))]
        public void Trim_NothingToDo(int size)
        {
            PerfUtils utils = new PerfUtils();
            string testString = utils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 10000; i++)
                        testString.Trim();
        }
    }
}
