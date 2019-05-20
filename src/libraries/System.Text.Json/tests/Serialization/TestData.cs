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
                yield return new object[] { typeof(SimpleTestStruct), SimpleTestStruct.s_data };
                yield return new object[] { typeof(SimpleTestClass), SimpleTestClass.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNullables), SimpleTestClassWithNullables.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNulls), SimpleTestClassWithNulls.s_data };
                yield return new object[] { typeof(SimpleTestClassWithSimpleObject), SimpleTestClassWithSimpleObject.s_data };
                yield return new object[] { typeof(SimpleTestClassWithObjectArrays), SimpleTestClassWithObjectArrays.s_data };
                yield return new object[] { typeof(BasicPerson), BasicPerson.s_data };
                yield return new object[] { typeof(BasicCompany), BasicCompany.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectInner), TestClassWithNestedObjectInner.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectOuter), TestClassWithNestedObjectOuter.s_data };
                yield return new object[] { typeof(TestClassWithObjectArray), TestClassWithObjectArray.s_data };
                yield return new object[] { typeof(TestClassWithObjectIEnumerableT), TestClassWithObjectIEnumerableT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIListT), TestClassWithObjectIListT.s_data };
                yield return new object[] { typeof(TestClassWithObjectICollectionT), TestClassWithObjectICollectionT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIReadOnlyCollectionT), TestClassWithObjectIReadOnlyCollectionT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIReadOnlyListT), TestClassWithObjectIReadOnlyListT.s_data };
                yield return new object[] { typeof(TestClassWithStringArray), TestClassWithStringArray.s_data };
                yield return new object[] { typeof(TestClassWithGenericList), TestClassWithGenericList.s_data };
                yield return new object[] { typeof(TestClassWithGenericIEnumerableT), TestClassWithGenericIEnumerableT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIListT), TestClassWithGenericIListT.s_data };
                yield return new object[] { typeof(TestClassWithGenericICollectionT), TestClassWithGenericICollectionT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIReadOnlyCollectionT), TestClassWithGenericIReadOnlyCollectionT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIReadOnlyListT), TestClassWithGenericIReadOnlyListT.s_data };
                yield return new object[] { typeof(TestClassWithStringToPrimitiveDictionary), TestClassWithStringToPrimitiveDictionary.s_data };
                yield return new object[] { typeof(TestClassWithObjectIEnumerableConstructibleTypes), TestClassWithObjectIEnumerableConstructibleTypes.s_data };
                yield return new object[] { typeof(TestClassWithObjectImmutableTypes), TestClassWithObjectImmutableTypes.s_data };
                yield return new object[] { typeof(JsonElementTests.JsonElementClass), JsonElementTests.JsonElementClass.s_data };
                yield return new object[] { typeof(JsonElementTests.JsonElementArrayClass), JsonElementTests.JsonElementArrayClass.s_data };
                yield return new object[] { typeof(ClassWithComplexObjects), ClassWithComplexObjects.s_data };
            }
        }
        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                yield return new object[] { new SimpleTestStruct() };
                yield return new object[] { new SimpleTestClass() };
                yield return new object[] { new SimpleTestClassWithNullables() };
                yield return new object[] { new SimpleTestClassWithNulls() };
                yield return new object[] { new SimpleTestClassWithSimpleObject() };
                yield return new object[] { new SimpleTestClassWithObjectArrays() };
                yield return new object[] { new BasicPerson() };
                yield return new object[] { new BasicCompany() };
                yield return new object[] { new TestClassWithNestedObjectInner() };
                yield return new object[] { new TestClassWithNestedObjectOuter() };
                yield return new object[] { new TestClassWithObjectArray() };
                yield return new object[] { new TestClassWithObjectIEnumerableT() };
                yield return new object[] { new TestClassWithObjectIListT() };
                yield return new object[] { new TestClassWithObjectICollectionT() };
                yield return new object[] { new TestClassWithObjectIReadOnlyCollectionT() };
                yield return new object[] { new TestClassWithObjectIReadOnlyListT() };
                yield return new object[] { new TestClassWithStringArray() };
                yield return new object[] { new TestClassWithGenericList() };
                yield return new object[] { new TestClassWithGenericIEnumerableT() };
                yield return new object[] { new TestClassWithGenericIListT() };
                yield return new object[] { new TestClassWithGenericICollectionT() };
                yield return new object[] { new TestClassWithGenericIReadOnlyCollectionT() };
                yield return new object[] { new TestClassWithGenericIReadOnlyListT() };
                yield return new object[] { new TestClassWithStringToPrimitiveDictionary() };
                yield return new object[] { new TestClassWithObjectIEnumerableConstructibleTypes() };
                yield return new object[] { new TestClassWithObjectImmutableTypes() };
                yield return new object[] { new JsonElementTests.JsonElementClass() };
                yield return new object[] { new JsonElementTests.JsonElementArrayClass() };
                yield return new object[] { new ClassWithComplexObjects() };
            }
        }
    }
}
