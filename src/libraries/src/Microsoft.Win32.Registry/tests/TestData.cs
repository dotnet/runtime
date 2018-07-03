// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Win32.RegistryTests
{
    internal static class TestData
    {
        private static readonly object[][] s_testValueTypes;

        private static readonly object[][] s_testObjects;

        private static readonly object[][] s_testEnvironment;

        private static readonly object[][] s_testExpandableStrings;

        private static readonly object[][] s_testValueNames;

        internal const string DefaultValue = "default";

        static TestData()
        {
            var rand = new Random(-55);

            s_testValueTypes = new[]
            {
                new object[] { "Test_01", (byte)rand.Next(byte.MinValue, sbyte.MaxValue) },
                new object[] { "Test_02", (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue) },
                new object[] { "Test_03", (short)rand.Next(short.MinValue, short.MaxValue) },
                new object[] { "Test_04", (ushort)rand.Next(ushort.MinValue, ushort.MaxValue) },
                new object[] { "Test_05", (int)rand.Next(int.MinValue, int.MaxValue) },
                new object[] { "Test_06", (uint)rand.Next(0, int.MaxValue) },
                new object[] { "Test_07", (long)rand.Next(int.MinValue, int.MaxValue) },
                new object[] { "Test_08", (ulong)rand.Next(0, int.MaxValue) },
                new object[] { "Test_09", new decimal(((double)decimal.MaxValue) * rand.NextDouble()) },
                new object[] { "Test_10", new decimal(((double)decimal.MinValue) * rand.NextDouble()) },
                new object[] { "Test_11", new decimal(((double)decimal.MinValue) * rand.NextDouble()) },
                new object[] { "Test_12", new decimal(((double)decimal.MaxValue) * rand.NextDouble()) },
                new object[] { "Test_13", int.MaxValue *rand.NextDouble() },
                new object[] { "Test_14", int.MinValue * rand.NextDouble() },
                new object[] { "Test_15", int.MaxValue * (float)rand.NextDouble() },
                new object[] { "Test_16", int.MinValue * (float)rand.NextDouble() }
            };

            var bytes = new byte[rand.Next(0, 100)];
            rand.NextBytes(bytes);
            var obj = new object();
            s_testObjects = new[]
            {
                // Standard Random Numbers
                new object[] { 0, (byte)rand.Next(byte.MinValue, byte.MaxValue), RegistryValueKind.String },
                new object[] { 1, (sbyte)rand.Next(sbyte.MinValue, sbyte.MaxValue), RegistryValueKind.String },
                new object[] { 2, (short)rand.Next(short.MinValue, short.MaxValue), RegistryValueKind.String },
                new object[] { 3, (ushort)rand.Next(ushort.MinValue, ushort.MaxValue), RegistryValueKind.String },
                new object[] { 4, (char)rand.Next(char.MinValue, char.MaxValue), RegistryValueKind.String },
                new object[] { 5, (int)rand.Next(int.MinValue, int.MaxValue), RegistryValueKind.DWord },
                // Random Numbers that can fit into Int32
                new object[] { 6, (uint)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 7, (long)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 8, (long)(rand.NextDouble() * int.MinValue), RegistryValueKind.String },
                new object[] { 9, (ulong)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 10, (decimal)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 11, (decimal)(rand.NextDouble() * int.MinValue), RegistryValueKind.String },
                new object[] { 12, (float)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 13, (float)(rand.NextDouble() * int.MinValue), RegistryValueKind.String },
                new object[] { 14, (double)(rand.NextDouble() * int.MaxValue), RegistryValueKind.String },
                new object[] { 15, (double)(rand.NextDouble() * int.MinValue), RegistryValueKind.String },
                // Random Numbers that can't fit into Int32 but can fit into Int64
                new object[] { 16, (uint)(rand.NextDouble() * (uint.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 17, (long)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 18, (long)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String },
                new object[] { 19, (ulong)(rand.NextDouble() * (long.MaxValue - (ulong)int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 20, (decimal)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 21, (decimal)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String },
                new object[] { 22, (float)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 23, (float)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String },
                new object[] { 24, (double)(rand.NextDouble() * (long.MaxValue - int.MaxValue) + int.MaxValue), RegistryValueKind.String },
                new object[] { 25, (double)(rand.NextDouble() * (long.MinValue - int.MinValue) + int.MinValue), RegistryValueKind.String },
                // Random Numbers that can't fit into Int32 or Int64
                new object[] { 26, (ulong)(rand.NextDouble() * (ulong.MaxValue - long.MaxValue) + long.MaxValue), RegistryValueKind.String },
                new object[] { 27, decimal.MaxValue, RegistryValueKind.String },
                new object[] { 28, decimal.MinValue, RegistryValueKind.String },
                new object[] { 29, float.MaxValue, RegistryValueKind.String },
                new object[] { 30, float.MinValue, RegistryValueKind.String },
                new object[] { 31, double.MaxValue, RegistryValueKind.String },
                new object[] { 32, double.MinValue, RegistryValueKind.String },
                // Various other types
                new object[] { 33, "Hello World", RegistryValueKind.String },
                new object[] { 34, "Hello %path5% World", RegistryValueKind.String },
                new object[] { 35, new[] { "Hello World", "Hello %path% World" }, RegistryValueKind.MultiString },
                new object[] { 36, obj, RegistryValueKind.String },
                new object[] { 37, bytes, RegistryValueKind.Binary }
            };

            var envs = new List<object[]>();
            int counter = 0;
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                envs.Add(new[] { "ExpandedTest_" + counter.ToString(), entry.Key, entry.Value });
                ++counter;
            }
            s_testEnvironment = envs.ToArray();

            const string sysRootVar = "%Systemroot%";
            const string pathVar = "%path%";
            const string tmpVar = "%tmp%";
            s_testExpandableStrings = new[]
            {
                new object[]
                {
                    sysRootVar + @"\mydrive\mydirectory\myfile.xxx",
                    Environment.ExpandEnvironmentVariables(sysRootVar) + @"\mydrive\mydirectory\myfile.xxx"
                },
                new object[]
                {
                    tmpVar + @"\gfdhghdfgk\fsdfds\dsd.yyy",
                    Environment.ExpandEnvironmentVariables(tmpVar) + @"\gfdhghdfgk\fsdfds\dsd.yyy"
                },
                new object[]
                {
                    pathVar + @"\rwerew.zzz",
                    Environment.ExpandEnvironmentVariables(pathVar) + @"\rwerew.zzz"
                },
                new object[]
                {
                    sysRootVar + @"\mydrive\" + pathVar + @"\myfile.xxx",
                    Environment.ExpandEnvironmentVariables(sysRootVar) + @"\mydrive\" +
                    Environment.ExpandEnvironmentVariables(pathVar) + @"\myfile.xxx"
                }
            };

            s_testValueNames = new[]
            {
                new object[] { string.Empty },
                new object[] { null },
                new object[] { new string('a', 256) } // the name length limit is 255 but prior to V4 the limit is 16383
            };
        }

        public static IEnumerable<object[]> TestValueTypes { get { return s_testValueTypes; } }

        public static IEnumerable<object[]> TestObjects { get { return s_testObjects; } }

        public static IEnumerable<object[]> TestEnvironment { get { return s_testEnvironment; } }

        public static IEnumerable<object[]> TestExpandableStrings { get { return s_testExpandableStrings; } }

        public static IEnumerable<object[]> TestValueNames { get { return s_testValueNames; } }
    }
}
