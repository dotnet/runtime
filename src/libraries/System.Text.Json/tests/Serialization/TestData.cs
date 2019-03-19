// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public static class TestData
    {
        public static IEnumerable<object[]> ReadSuccessCases
        {
            get
            {
                yield return new object[] { typeof(SimpleTestClass), SimpleTestClass.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNullables), SimpleTestClassWithNullables.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNulls), SimpleTestClassWithNulls.s_data };
                yield return new object[] { typeof(BasicJson), BasicJson.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectInner), TestClassWithNestedObjectInner.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectOuter), TestClassWithNestedObjectOuter.s_data };
                yield return new object[] { typeof(TestClassWithObjectArray), TestClassWithObjectArray.s_data };
                yield return new object[] { typeof(TestClassWithStringArray), TestClassWithStringArray.s_data };
                yield return new object[] { typeof(TestClassWithGenericList), TestClassWithGenericList.s_data };
            }
        }
        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                yield return new object[] { new SimpleTestClass() };
                yield return new object[] { new SimpleTestClassWithNullables() };
                yield return new object[] { new SimpleTestClassWithNulls() };
                yield return new object[] { new BasicJson() };
                yield return new object[] { new TestClassWithNestedObjectInner() };
                yield return new object[] { new TestClassWithNestedObjectOuter() };
                yield return new object[] { new TestClassWithObjectArray() };
                yield return new object[] { new TestClassWithStringArray() };
                yield return new object[] { new TestClassWithGenericList() };
            }
        }
    }
}
