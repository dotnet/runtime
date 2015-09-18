// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [MemberData("TestStringSizes")]
        public void GetChars(int size)
        {
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.ToCharArray();
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Concat_str_str(int size)
        {
            string testString1 = PerfUtils.CreateString(size);
            string testString2 = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    string.Concat(testString1, testString2);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Concat_str_str_str(int size)
        {
            string testString1 = PerfUtils.CreateString(size);
            string testString2 = PerfUtils.CreateString(size);
            string testString3 = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    string.Concat(testString1, testString2, testString3);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Concat_str_str_str_str(int size)
        {
            string testString1 = PerfUtils.CreateString(size);
            string testString2 = PerfUtils.CreateString(size);
            string testString3 = PerfUtils.CreateString(size);
            string testString4 = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    string.Concat(testString1, testString2, testString3, testString4);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Contains(int size)
        {
            string testString = PerfUtils.CreateString(size);
            string subString = testString.Substring(testString.Length / 2, testString.Length / 4);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Contains(subString);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Equals(int size)
        {
            string testString1 = PerfUtils.CreateString(size);
            string testString2 = new string(testString1.ToCharArray());
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
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
            // Setup the format string and the list of objects to format
            StringBuilder formatter = new StringBuilder();
            List<string> objects = new List<string>();
            for (int i = 0; i < numberOfObjects; i++)
            {
                formatter.Append("%s, ");
                objects.Add(PerfUtils.CreateString(10));
            }
            string format = formatter.ToString();
            string[] objectArr = objects.ToArray();

            // Perform the actual formatting
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    string.Format(format, objectArr);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void GetLength(int size)
        {
            int result;
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    result = testString.Length; result = testString.Length; result = testString.Length;
                    result = testString.Length; result = testString.Length; result = testString.Length;
                    result = testString.Length; result = testString.Length; result = testString.Length;
                }
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void op_Equality(int size)
        {
            bool result;
            string testString1 = PerfUtils.CreateString(size);
            string testString2 = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                {
                    result = testString1 == testString2; result = testString1 == testString2;
                    result = testString1 == testString2; result = testString1 == testString2;
                    result = testString1 == testString2; result = testString1 == testString2;
                }
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Replace(int size)
        {
            string testString = PerfUtils.CreateString(size);
            string existingValue = testString.Substring(testString.Length / 2, 1);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Replace(existingValue, "1");
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Split(int size)
        {
            string testString = PerfUtils.CreateString(size);
            string existingValue = testString.Substring(testString.Length / 2, 1);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Split(existingValue);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void StartsWith(int size)
        {
            string testString = PerfUtils.CreateString(size);
            string subString = testString.Substring(0, testString.Length / 4);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.StartsWith(subString);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Substring_int(int size)
        {
            int startIndex = size / 2;
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Substring(startIndex);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Substring_int_int(int size)
        {
            int startIndex = size / 2;
            int length = size / 4;
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Substring(startIndex, length);
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void ToLower(int size)
        {
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.ToLower();
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void ToUpper(int size)
        {
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.ToUpper();
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Trim_WithWhitespace(int size)
        {
            string testString = "   " + PerfUtils.CreateString(size) + "   ";
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Trim();
        }

        [Benchmark]
        [MemberData("TestStringSizes")]
        public void Trim_NothingToDo(int size)
        {
            string testString = PerfUtils.CreateString(size);
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    testString.Trim();
        }
    }
}
