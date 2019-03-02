// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ArrayTests
    {
        [Fact]
        public static void WriteClassWithStringArray()
        {
            string json;

            {
                TestClassWithStringArray obj = new TestClassWithStringArray();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.ToString(obj);
            }

            {
                TestClassWithStringArray obj = JsonSerializer.Parse<TestClassWithStringArray>(json);
                obj.Verify();
            }

            {
                TestClassWithStringArray obj = JsonSerializer.Parse<TestClassWithStringArray>(TestClassWithStringArray.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithObjectArray()
        {
            string json;

            {
                TestClassWithGenericList obj = new TestClassWithGenericList();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.ToString(obj);
            }

            {
                TestClassWithGenericList obj = JsonSerializer.Parse<TestClassWithGenericList>(json);
                obj.Verify();
            }

            {
                TestClassWithGenericList obj = JsonSerializer.Parse<TestClassWithGenericList>(TestClassWithGenericList.s_data);
                obj.Verify();
            }
        }

        [Fact]
        public static void WriteClassWithGenericList()
        {
            string json;

            {
                TestClassWithObjectList obj = new TestClassWithObjectList();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.ToString(obj);
            }

            {
                TestClassWithObjectList obj = JsonSerializer.Parse<TestClassWithObjectList>(json);
                obj.Verify();
            }

            {
                TestClassWithObjectList obj = JsonSerializer.Parse<TestClassWithObjectList>(TestClassWithObjectList.s_data);
                obj.Verify();
            }
        }
    }
}
