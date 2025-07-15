// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                yield return new object[] { typeof(SimpleTestStructWithFields), SimpleTestStructWithFields.s_data };
                yield return new object[] { typeof(SimpleTestClass), SimpleTestClass.s_data };
                yield return new object[] { typeof(SimpleTestClassWithFields), SimpleTestClassWithFields.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNullables), SimpleTestClassWithNullables.s_data };
                yield return new object[] { typeof(SimpleTestClassWithNulls), SimpleTestClassWithNulls.s_data };
                yield return new object[] { typeof(SimpleTestClassWithSimpleObject), SimpleTestClassWithSimpleObject.s_data };
                yield return new object[] { typeof(SimpleTestClassWithObjectArrays), SimpleTestClassWithObjectArrays.s_data };
                yield return new object[] { typeof(BasicPerson), BasicPerson.s_data };
                yield return new object[] { typeof(BasicCompany), BasicCompany.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectInner), TestClassWithNestedObjectInner.s_data };
                yield return new object[] { typeof(TestClassWithNestedObjectOuter), TestClassWithNestedObjectOuter.s_data };
                yield return new object[] { typeof(TestClassWithObjectArray), TestClassWithObjectArray.s_data };
                yield return new object[] { typeof(TestClassWithObjectIEnumerable), TestClassWithObjectIEnumerable.s_data };
                yield return new object[] { typeof(TestClassWithObjectIList), TestClassWithObjectIList.s_data };
                yield return new object[] { typeof(TestClassWithObjectICollection), TestClassWithObjectICollection.s_data };
                yield return new object[] { typeof(TestClassWithObjectIEnumerableT), TestClassWithObjectIEnumerableT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIListT), TestClassWithObjectIListT.s_data };
                yield return new object[] { typeof(TestClassWithObjectICollectionT), TestClassWithObjectICollectionT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIReadOnlyCollectionT), TestClassWithObjectIReadOnlyCollectionT.s_data };
                yield return new object[] { typeof(TestClassWithObjectIReadOnlyListT), TestClassWithObjectIReadOnlyListT.s_data };
                yield return new object[] { typeof(TestClassWithObjectISetT), TestClassWithObjectISetT.s_data };
                yield return new object[] { typeof(TestClassWithStringArray), TestClassWithStringArray.s_data };
                yield return new object[] { typeof(TestClassWithGenericList), TestClassWithGenericList.s_data };
                yield return new object[] { typeof(TestClassWithGenericIEnumerable), TestClassWithGenericIEnumerable.s_data };
                yield return new object[] { typeof(TestClassWithGenericIList), TestClassWithGenericIList.s_data };
                yield return new object[] { typeof(TestClassWithGenericICollection), TestClassWithGenericICollection.s_data };
                yield return new object[] { typeof(TestClassWithGenericIEnumerableT), TestClassWithGenericIEnumerableT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIListT), TestClassWithGenericIListT.s_data };
                yield return new object[] { typeof(TestClassWithGenericICollectionT), TestClassWithGenericICollectionT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIReadOnlyCollectionT), TestClassWithGenericIReadOnlyCollectionT.s_data };
                yield return new object[] { typeof(TestClassWithGenericIReadOnlyListT), TestClassWithGenericIReadOnlyListT.s_data };
                yield return new object[] { typeof(TestClassWithGenericISetT), TestClassWithGenericISetT.s_data };
                yield return new object[] { typeof(TestClassWithStringToPrimitiveDictionary), TestClassWithStringToPrimitiveDictionary.s_data };
                yield return new object[] { typeof(TestClassWithObjectIEnumerableConstructibleTypes), TestClassWithObjectIEnumerableConstructibleTypes.s_data };
                yield return new object[] { typeof(TestClassWithObjectImmutableTypes), TestClassWithObjectImmutableTypes.s_data };
                yield return new object[] { typeof(JsonElementClass), JsonElementClass.s_data };
                yield return new object[] { typeof(JsonElementArrayClass), JsonElementArrayClass.s_data };
                yield return new object[] { typeof(ClassWithComplexObjects), ClassWithComplexObjects.s_data };
            }
        }

        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                yield return new object[] { new SimpleTestStruct() };
                yield return new object[] { new SimpleTestStructWithFields() };
                yield return new object[] { new SimpleTestClass() };
                yield return new object[] { new SimpleTestClassWithFields() };
                yield return new object[] { new SimpleTestClassWithNullables() };
                yield return new object[] { new SimpleTestClassWithNulls() };
                yield return new object[] { new SimpleTestClassWithSimpleObject() };
                yield return new object[] { new SimpleTestClassWithObjectArrays() };
                yield return new object[] { new BasicPerson() };
                yield return new object[] { new BasicCompany() };
                yield return new object[] { new TestClassWithNestedObjectInner() };
                yield return new object[] { new TestClassWithNestedObjectOuter() };
                yield return new object[] { new TestClassWithObjectArray() };
                yield return new object[] { new TestClassWithObjectIEnumerable() };
                yield return new object[] { new TestClassWithObjectIList() };
                yield return new object[] { new TestClassWithObjectICollection() };
                yield return new object[] { new TestClassWithObjectIEnumerableT() };
                yield return new object[] { new TestClassWithObjectIListT() };
                yield return new object[] { new TestClassWithObjectICollectionT() };
                yield return new object[] { new TestClassWithObjectIReadOnlyCollectionT() };
                yield return new object[] { new TestClassWithObjectIReadOnlyListT() };
                yield return new object[] { new TestClassWithObjectISetT() };
                yield return new object[] { new TestClassWithStringArray() };
                yield return new object[] { new TestClassWithGenericList() };
                yield return new object[] { new TestClassWithGenericIEnumerable() };
                yield return new object[] { new TestClassWithGenericIList() };
                yield return new object[] { new TestClassWithGenericICollection() };
                yield return new object[] { new TestClassWithGenericIEnumerableT() };
                yield return new object[] { new TestClassWithGenericIListT() };
                yield return new object[] { new TestClassWithGenericICollectionT() };
                yield return new object[] { new TestClassWithGenericIReadOnlyCollectionT() };
                yield return new object[] { new TestClassWithGenericIReadOnlyListT() };
                yield return new object[] { new TestClassWithGenericISetT() };
                yield return new object[] { new TestClassWithStringToPrimitiveDictionary() };
                yield return new object[] { new TestClassWithObjectIEnumerableConstructibleTypes() };
                yield return new object[] { new TestClassWithObjectImmutableTypes() };
                yield return new object[] { new JsonElementClass() };
                yield return new object[] { new JsonElementArrayClass() };
                yield return new object[] { new JsonDocumentClass() };
                yield return new object[] { new JsonDocumentArrayClass() };
                yield return new object[] { new ClassWithComplexObjects() };
            }
        }

        public static IEnumerable<object[]> DuplicatePropertyJsonPayloads => field ??=
        [
            [$$"""{"p0":0,"p0":42}"""],
            [$$"""{"p0":0,"p1":1,"p1":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p2":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p3":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p4":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p5":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p6":42}"""],

            [$$"""{"p0":0,"p1":1,"p0":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p0":42}"""],

            // First occurrence escaped
            [$$"""{"p0":0,"p\u0031":1,"p1":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p\u0036":6,"p6":42}"""],
            [$$"""{"p\u0030":0,"p1":1,"p0":42}"""],
            [$$"""{"p\u0030":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p0":42}"""],

            // Last occurrence escaped
            [$$"""{"p0":0,"p1":1,"p\u0031":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p\u0036":42}"""],
            [$$"""{"p0":0,"p1":1,"p\u0030":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p\u0030":42}"""],

            // Both occurrences escaped
            [$$"""{"p0":0,"p\u0031":1,"p\u0031":42}"""],
            [$$"""{"p0":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p\u0036":6,"p\u0036":42}"""],
            [$$"""{"p\u0030":0,"p1":1,"p\u0030":42}"""],
            [$$"""{"p\u0030":0,"p1":1,"p2":2,"p3":3,"p4":4,"p5":5,"p6":6,"p\u0030":42}"""],

            [$$"""{"A":[],"A":1}"""],
            [$$"""{"A":{"A":1},"A":1}"""],
            [$$"""{"A":{"B":1},"A":1}"""],

            // No error
            [$$"""{"A":{"A":1} }""", true],
            [$$"""{"A":{"B":1},"B":1}""", true],
        ];
    }
}
