// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace CoreXml.Test.XLinq
{
    public partial class TestFailedException : Exception
    {
        /// <summary>
        /// Construct a unit test exception.
        /// </summary>
        /// <param name="message">Description of the exception.</param>
        public TestFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Construct a unit test exception.
        /// </summary>
        /// <param name="message">Description of the exception.</param>
        /// <param name="innerException">Exception that caused or is related to this exception.
        /// </param>
        public TestFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Class containing common methods for validating runtime behavior.
    /// </summary>
    public static class Validate
    {
        /// <summary>
        /// Compares two lists and throws if they are not the same.
        /// </summary>
        /// <param name="actualList">Actual errors detected.</param>
        /// <param name="expectedList">Erros expected.</param>
        public static void CompareLists<T>(
            ICollection<T> actualList,
            ICollection<T> expectedList) where T : IComparable<T>
        {
            T[] sortedActual = new T[actualList.Count];
            T[] sortedExpected = new T[expectedList.Count];

            actualList.CopyTo(sortedActual, 0);
            expectedList.CopyTo(sortedExpected, 0);

            Array.Sort<T>(sortedActual);
            Array.Sort<T>(sortedExpected);

            Collection<T> underReported = new Collection<T>();
            Collection<T> overReported = new Collection<T>();

            int actualIndex = 0;
            int expectedIndex = 0;

            while ((actualIndex < sortedActual.Length)
                   && (expectedIndex < sortedExpected.Length))
            {
                T actual = sortedActual[actualIndex];
                T expected = sortedExpected[expectedIndex];

                int compare = actual.CompareTo(expected);

                if (compare == 0)
                {
                    // Actual and expected match; move on.
                    actualIndex++;
                    expectedIndex++;
                }
                else if (compare < 0)
                {
                    // An unexpected item was reported.
                    overReported.Add(actual);
                    actualIndex++;
                }
                else
                {
                    // An expected item was not reported.
                    underReported.Add(expected);
                    expectedIndex++;
                }
            }

            while (actualIndex < sortedActual.Length)
            {
                // An unexpected violation was reported.
                T actualItem = sortedActual[actualIndex];
                overReported.Add(actualItem);
                actualIndex++;
            }

            while (expectedIndex < sortedExpected.Length)
            {
                // An expected violation was not reported.
                T expectedItem = sortedExpected[expectedIndex];
                underReported.Add(expectedItem);
                expectedIndex++;
            }

            foreach (T underReportedItem in underReported)
            {
                Console.WriteLine(
                    "=== An expected item was not reported:\n{0}\n",
                    underReportedItem.ToString());
            }

            foreach (T overReportedItem in overReported)
            {
                Console.WriteLine(
                    "=== An unexpected item was reported:\n{0}\n",
                    overReportedItem.ToString());
            }

            if ((underReported.Count != 0) || (overReported.Count != 0))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "List mismatch: {0} expected item were not reported, "
                        + "and {1} unexpected item were reported.",
                    underReported.Count,
                    overReported.Count));
            }
        }

        /// <summary>
        /// Validates an attribute name and value against expected values.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="expectedExpandedName"></param>
        /// <param name="expectedValue"></param>
        public static void AttributeNameAndValue(
            XAttribute attribute,
            string expectedExpandedName,
            string expectedValue)
        {
            if (attribute == null)
            {
                throw new TestFailedException("Expected non-null attribute");
            }

            if (attribute.Name.ToString() != expectedExpandedName)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Attribute name '{0}'; expected '{1}'",
                    attribute.Name.ToString(),
                    expectedExpandedName));
            }

            if (attribute.Value != expectedValue)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Attribute value '{0}'; expected '{1}'",
                    attribute.Value,
                    expectedValue));
            }
        }

        /// <summary>
        /// Validates that the names of a set of attributes
        /// match expected names.
        /// </summary>
        /// <param name="attributes">Attributes whose names are checked.</param>
        /// <param name="expectedNames">Names expected for each attribute.</param>
        public static void AttributeNames(
            IEnumerable<XAttribute> attributes,
            IList<string> expectedNames)
        {
            AttributeNames(attributes.GetEnumerator(), expectedNames);
        }

        /// <summary>
        /// Validates that the names of a set of attributes
        /// match expected names.
        /// </summary>
        /// <param name="attributes">Attributes whose names are checked.</param>
        /// <param name="expectedNames">Names expected for each attribute.</param>
        public static void AttributeNames(
            IEnumerator<XAttribute> attributes,
            IList<string> expectedNames)
        {
            int i = 0;
            int expectedCount = expectedNames.Count;

            while (attributes.MoveNext())
            {
                if (i >= expectedCount)
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "There are more attributes than the expected {0}.",
                        expectedCount));
                }

                if (attributes.Current.Name.ToString() != expectedNames[i])
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Attribute {0} has name '{1}'; expected '{2}'",
                        i,
                        attributes.Current.Name.ToString(),
                        expectedNames[i]));
                }

                i++;
            }

            if (i < expectedCount)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Only the first {0} of the {1} expected attributes exist.",
                    i,
                    expectedCount));
            }
        }

        /// <summary>
        /// Validates that the values of a set of attributes
        /// match expected values.
        /// </summary>
        /// <param name="attributes">Attributes whose values are checked.</param>
        /// <param name="expectedValues">Values expected for each attribute.</param>
        public static void AttributeValues(
            IEnumerable<XAttribute> attributes,
            IList expectedValues)
        {
            AttributeValues(attributes.GetEnumerator(), expectedValues);
        }

        /// <summary>
        /// Validates that the values of a set of attributes
        /// match expected values.
        /// </summary>
        /// <param name="attributes">Attributes whose values are checked.</param>
        /// <param name="expectedValues">Values expected for each attribute.</param>
        public static void AttributeValues(
            IEnumerator<XAttribute> attributes,
            IList expectedValues)
        {
            int i = 0;
            int expectedCount = expectedValues.Count;

            while (attributes.MoveNext())
            {
                if (i >= expectedCount)
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "There are more attributes than the expected {0}.",
                        expectedCount));
                }

                if (!attributes.Current.Value.Equals(expectedValues[i]))
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Attribute {0} has value '{1}'; expected '{2}'",
                        i,
                        attributes.Current.Value,
                        expectedValues[i]));
                }

                i++;
            }

            if (i < expectedCount)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Only the first {0} of the {1} expected attributes exist.",
                    i,
                    expectedCount));
            }
        }

        /// <summary>
        /// Validates that the name of an element matches an expected value.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expectedExpandedName"></param>
        public static void ElementName(XElement element, string expectedExpandedName)
        {
            int i = string.Compare(
                element.Name.ToString(),
                expectedExpandedName);
            //,StringComparison.InvariantCulture);

            if (i != 0)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Element name '{0}' does not match expected value '{1}'.",
                    element.Name.ToString(),
                    expectedExpandedName));
            }
        }

        /// <summary>
        /// Validates the textual content of an element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="expectedValue"></param>
        public static void ElementValue(XElement element, string expectedValue)
        {
            if (element.Value != expectedValue)
            {
                throw new TestFailedException(string.Format(
                    "Element '{0}' has textual value '{1}' vs. expected value '{2}'",
                    element.ToString(),
                    element.Value,
                    expectedValue));
            }
        }

        /// <summary>
        /// Validates that an element has no content.
        /// </summary>
        /// <param name="element"></param>
        public static void IsEmptyElement(XElement element)
        {
            if (!element.IsEmpty)
            {
                throw new TestFailedException(string.Format(
                    "Element '{0}' is expected to be empty, but has content [{1}]",
                    element.ToString()));
            }
        }

        /// <summary>
        /// Validates that the names of a set of elements
        /// match expected names.
        /// </summary>
        /// <param name="elements">Elements whose names are checked.</param>
        /// <param name="expectedExpandedNames">Names expected for each element.</param>
        public static void ElementNames(
            IEnumerable<XElement> elements,
            IList<string> expectedExpandedNames)
        {
            ElementNames(elements.GetEnumerator(), expectedExpandedNames);
        }

        /// <summary>
        /// Validates that the names of a set of elements
        /// match expected names.
        /// </summary>
        /// <param name="elements">Elements whose names are checked.</param>
        /// <param name="expectedExpandedNames">Names expected for each element.</param>
        public static void ElementNames(
            IEnumerator<XElement> elements,
            IList<string> expectedExpandedNames)
        {
            int i = 0;
            int expectedCount = expectedExpandedNames.Count;

            while (elements.MoveNext())
            {
                if (i >= expectedCount)
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "There are more elements than the expected {0}.",
                        expectedCount));
                }

                if (elements.Current.Name.ToString() != expectedExpandedNames[i])
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Element {0} has name '{1}'; expected '{2}'",
                        i,
                        elements.Current.Name.ToString(),
                        expectedExpandedNames[i]));
                }

                i++;
            }

            if (i < expectedCount)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Only the first {0} of the {1} expected attributes exist.",
                    i,
                    expectedCount));
            }
        }

        /// <summary>
        /// Throw if the given collection does not contain the specified number of items.
        /// </summary>
        /// <typeparam name="T">Type of item in the collection.</typeparam>
        /// <param name="collection">Collection to inspect.</param>
        /// <param name="expectedCount">Expected value of collection.Count.</param>
        public static void Count<T>(IEnumerable<T> collection, int expectedCount)
        {
            int count = 0;
            IEnumerator<T> enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }

            if (count != expectedCount)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1}s found; expected {2}.",
                    count,
                    typeof(T).Name,
                    expectedCount));
            }
        }

        /// <summary>
        /// Throw if the enumerator does not produce the given list of values.
        /// </summary>
        /// <typeparam name="T">Type of value being enumerated.</typeparam>
        /// <param name="enumerable">Enumerable object to inspect.</param>
        /// <param name="expectedValues">Expected values for the enumerator to produce.</param>
        public static void Enumerator<T>(IEnumerable<T> enumerable, IList<T> expectedValues)
        {
            Enumerator<T>(enumerable.GetEnumerator(), expectedValues, (x, y) => x.Equals(y));
        }

        public static void EnumeratorDeepEquals<T>(IEnumerable<T> enumerable, IList<T> expectedValues) where T : XNode
        {
            Enumerator<T>(enumerable.GetEnumerator(), expectedValues, (x, y) => XNode.DeepEquals(x, y));
        }

        public static void EnumeratorAttributes<T>(IEnumerable<T> enumerable, IList<T> expectedValues) where T : XAttribute
        {
            Enumerator<T>(enumerable.GetEnumerator(), expectedValues, (x, y) => x.Name.Equals(y.Name) && x.Value.Equals(y.Value));
        }

        /// <summary>
        /// Throw if the enumerator does not produce the exact given list of values.
        /// </summary>
        /// <typeparam name="T">Type of value being enumerated.</typeparam>
        /// <param name="enumerator">Enumerator object to inspect.</param>
        /// <param name="expectedValues">Expected values for the enumerator to produce.</param>
        public static void Enumerator<T>(IEnumerator<T> enumerator, IList<T> expectedValues, Func<T, T, bool> compare)
        {
            int i;

            for (i = 0; enumerator.MoveNext(); i++)
            {
                if (i >= expectedValues.Count)
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Enumerator returned more than the {0} expected items.",
                        expectedValues.Count));
                }
                else if (!compare(enumerator.Current, expectedValues[i]))
                {   //!enumerator.Current.Equals(expectedValues[i]
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Enumerator returned an unexpected value on iteration {0}. Expected: {1}, Actual: {2}", i, expectedValues[i], enumerator.Current));
                }
            }

            if (i < expectedValues.Count)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Enumerator returned only the first {0} of the {1} expected items.",
                    i,
                    expectedValues.Count));
            }
        }


        /// <summary>
        /// Throw if the enumerator does not contain the given list of values.
        /// </summary>
        /// <typeparam name="T">Type of value being enumerated.</typeparam>
        /// <param name="enumerable">Enumerable object to inspect.</param>
        /// <param name="expectedValues">A unique list of Expected values for the enumerator to produce.</param>
        public static void EnumeratorContains<T>(IEnumerable<T> enumerable, IList<T> expectedValues)
        {
            Dictionary<T, object> dict = new Dictionary<T, object>();

            foreach (T item in expectedValues)
            {
                dict.Add(item, null);
            }

            foreach (T item in enumerable)
            {
                if (dict.ContainsKey(item))
                {
                    dict.Remove(item);
                }
            }

            if (dict.Count > 0)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Enumerator returned did not contain {0} of the expected values.",
                    dict.Count));
            }
        }

        /// <summary>
        /// Throw if the enumerator contains the given list of values.
        /// </summary>
        /// <typeparam name="T">Type of value being enumerated.</typeparam>
        /// <param name="enumerable">Enumerable object to inspect.</param>
        /// <param name="expectedValues">A unique list of Expected values for the enumerator to produce.</param>
        public static void EnumeratorDoesNotContain<T>(IEnumerable<T> enumerable, IList<T> expectedValues)
        {
            Dictionary<T, object> dict = new Dictionary<T, object>();

            foreach (T item in expectedValues)
            {
                dict.Add(item, null);
            }

            foreach (T item in enumerable)
            {
                if (dict.ContainsKey(item))
                {
                    throw new TestFailedException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Enumerator contain unexpected value {0}.",
                        item.ToString()));
                }
            }
        }

        /// <summary>
        /// Throw if the given collection does not have the expected value for IsReadOnly.
        /// </summary>
        /// <typeparam name="T">Type of item in the collection.</typeparam>
        /// <param name="collection">Collection to inspect.</param>
        /// <param name="expectedValue">Expected value of collection.ReadOnly.</param>
        public static void IsReadOnly<T>(ICollection<T> collection, bool expectedValue)
        {
            if (collection.IsReadOnly != expectedValue)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.IsReadOnly is {1}; expected value is {2}.",
                    typeof(T).Name,
                    collection.IsReadOnly,
                    expectedValue));
            }
        }

        /// <summary>
        /// Throw if IndexOf does not return the expected value.
        /// </summary>
        /// <typeparam name="T">Type of value in the list.</typeparam>
        /// <param name="values">The list of values to inspect.</param>
        /// <param name="value">The value to look for.</param>
        /// <param name="expectedIndex">The expected index of value.</param>
        public static void IndexOf<T>(IList<T> values, T value, int expectedIndex)
        {
            int index = values.IndexOf(value);

            if (index != expectedIndex)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.IndexOf() returned {1}; expected value is {2}.",
                    typeof(T).Name,
                    index,
                    expectedIndex));
            }
        }

        /// <summary>
        /// Validate that the caught exception is of the expected type.
        /// </summary>
        /// <param name="caught">Exception caught.</param>
        /// <param name="expectedException">Expected value of ex.GetType().</param>
        public static void Catch(Exception caught, Type expectedException)
        {
            if (!(caught.GetType() == expectedException))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Caught exception of type {0}; expected {1}.",
                    caught.GetType().FullName,
                    expectedException.FullName));
            }
        }

        /// <summary>
        /// Validate that the caught exception is of the expected type with
        /// an inner exception of an expected type.
        /// </summary>
        /// <param name="caught">Exception caught.</param>
        /// <param name="expectedException">Expected value of ex.GetType().</param>
        /// <param name="expectedInnerException">Expected value of ex.Inner.GetType().</param>
        public static void Catch(Exception caught, Type expectedException, Type expectedInnerException)
        {
            if (!(caught.GetType().DeclaringType == expectedException))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Caught exception of type {0}; expected {1}.",
                    caught.GetType().FullName,
                    expectedException.FullName));
            }

            if (caught.InnerException == null)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Caught exception of type {0} with null inner exception; expected inner exception {1}.",
                    caught.GetType().FullName,
                    expectedInnerException.FullName));
            }

            if (!(caught.InnerException.GetType().DeclaringType == expectedException))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Caught exception of type {0} with inner exception of type {1}; expected inner exception of type {2}.",
                    caught.GetType().FullName,
                    caught.InnerException.GetType().FullName,
                    expectedInnerException.FullName));
            }
        }

        /// <summary>
        /// Throw because an exception was not expected.
        /// </summary>
        /// <param name="exception">The unexpectd exception that was thrown.</param>
        public static void UnexpectedThrow(Exception exception)
        {
            throw new TestFailedException(string.Format(
                CultureInfo.InvariantCulture,
                "Unexpected exception thrown {0}.",
                exception.Message));
        }

        /// <summary>
        /// Throw because an exception was expected but not thrown.
        /// </summary>
        /// <param name="expectedException">Type of exception expected.</param>
        public static void ExpectedThrow(Type expectedException)
        {
            throw new TestFailedException(string.Format(
                CultureInfo.InvariantCulture,
                "No exception thrown; expected {0}.",
                expectedException.FullName));
        }

        /// <summary>
        /// Throw if two strings are not equal.
        /// </summary>
        /// <param name="actualValue">The actual value.</param>
        /// <param name="expectedValue">The expected value.</param>
        public static void String(string actualValue, string expectedValue)
        {
            if (string.Compare(actualValue, expectedValue) != 0)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "String '{0}' does not match the expected value '{1}'.",
                    actualValue,
                    expectedValue));
            }
        }

        /// <summary>
        /// Throw if two objects are null or not equal.
        /// </summary>
        /// <param name="actual">The actual value.</param>
        /// <param name="expected">The expected value.</param>
        public static void IsEqual(object actual, object expected)
        {
            if ((actual == null)
                || (expected == null)
                || !actual.Equals(expected))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Value '{0}' does not match the expected value '{1}'.",
                    (actual == null) ? "null" : actual.ToString(),
                    (expected == null) ? "null" : expected.ToString()));
            }
        }

        public static void DeepEquals(object actual, object expected)
        {
            if (!XNode.DeepEquals(actual as XNode, expected as XNode))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Value '{0}' does not match the expected value (DeepEquals) '{1}'.",
                    (actual == null) ? "null" : actual.ToString(),
                    (expected == null) ? "null" : expected.ToString()));
            }
        }

        /// <summary>
        /// Throw if two objects are not non-null or not exactly the same object.
        /// </summary>
        /// <param name="actual">First object to compare.</param>
        /// <param name="expected">First object to compare.</param>
        public static void IsReferenceEqual(object actual, object expected)
        {
            if ((actual == null) || (expected == null) || !object.ReferenceEquals(actual, expected))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "One or both of 2 objects are null, or not the same reference (got '{0}' but expected '{1}').",
                    (actual == null) ? "null" : actual.ToString(),
                    (expected == null) ? "null" : expected.ToString()));
            }
        }

        /// <summary>
        /// Throw if two objects are not non-null or exactly the same object.
        /// </summary>
        /// <param name="obj1">First object to compare.</param>
        /// <param name="obj2">First object to compare.</param>
        public static void IsNotReferenceEqual(object obj1, object obj2)
        {
            if ((obj1 == null) || (obj2 == null) || object.ReferenceEquals(obj1, obj2))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "One or both of 2 objects are null, or are the same reference ('{0}' vs '{1}').",
                    (obj1 == null) ? "null" : obj1.ToString(),
                    (obj2 == null) ? "null" : obj2.ToString()));
            }
        }

        /// <summary>
        /// Throw if the value is not true;
        /// </summary>
        /// <param name="value">a boolean value to check</param>
        public static void IsTrue(bool value)
        {
            if (!value)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected true boolean value."));
            }
        }

        /// <summary>
        /// Throw if the value is not false.
        /// </summary>
        /// <param name="value">a boolean value to check</param>
        public static void IsFalse(bool value)
        {
            if (value)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected false boolean value."));
            }
        }

        /// <summary>
        /// Throw if an object is not null.
        /// </summary>
        /// <param name="actual"></param>
        public static void IsNull(object actual)
        {
            if (!object.ReferenceEquals(actual, null))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Value '{0}' is non-null; expected null",
                    actual.ToString()));
            }
        }

        /// <summary>
        /// Throw if an object is null.
        /// </summary>
        /// <param name="actual"></param>
        public static void IsNotNull(object actual)
        {
            if (object.ReferenceEquals(actual, null))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Value '{0}' is null; expected non-null",
                    actual.ToString()));
            }
        }

        /// <summary>
        /// Throw if the collection is not empty.
        /// </summary>
        /// <param name="collection">the collection to check</param>
        public static void IsEmpty(IEnumerable collection)
        {
            IEnumerator e = collection.GetEnumerator();

            if (e.MoveNext())
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected empty collection."));
            }
        }

        /// <summary>
        /// Throw if the collection is empty.
        /// </summary>
        /// <param name="collection">the collection to check</param>
        public static void IsNotEmpty(IEnumerable collection)
        {
            IEnumerator e = collection.GetEnumerator();

            if (!e.MoveNext())
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected non-empty collection."));
            }
        }

        /// <summary>
        /// Throw if a string does not contain an expected substring.
        /// </summary>
        /// <param name="s">The string to inspect.</param>
        /// <param name="expectedSubstring">The substring that s is expected to contain.</param>
        public static void Contains(string s, string expectedSubstring)
        {
            if (s.IndexOf(expectedSubstring) == -1)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "String '{0}' does not contain the expected substring '{1}'.",
                    s,
                    expectedSubstring));
            }
        }

        /// <summary>
        /// Throw if an object's type does not match an expected type,
        /// or if the object is null.
        /// </summary>
        /// <param name="o">Object to check.</param>
        /// <param name="expectedType">Expected type of the object.
        /// The object's type must match this type exactly.</param>
        public static void Type(object o, Type expectedType)
        {
            if (o == null)
            {
                throw new TestFailedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Object is null; expected object of type '{0}'.",
                        expectedType.FullName));
            }

            if (o.GetType() != expectedType)
            {
                throw new TestFailedException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Type of object is '{0}'; expected '{1}'.",
                        o.GetType().FullName,
                        expectedType.FullName));
            }
        }

        /// <summary>
        /// checks if two document versions are the same
        /// </summary>
        /// <param name="actual">Actual document version.</param>
        /// <param name="expected">Expected document version.</param>
        public static void SameVersion(
            long actual,
            long expected)
        {
            if (actual != expected)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected document version {0} instead of {1}.",
                    expected.ToString(),
                    actual.ToString()));
            }
        }

        /// <summary>
        /// checks if two docPaths are the same
        /// </summary>
        /// <param name="actual">Actual docPath.</param>
        /// <param name="expected">Expected docPath.</param>
        public static void SameUri(
            Uri actual,
            Uri expected)
        {
            if (!actual.Equals(expected))
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected docPath {0} instead of {1}.",
                    expected,
                    actual));
            }
        }

        /// <summary>
        /// checks if two guid are the same
        /// </summary>
        /// <param name="actual">Actual guid.</param>
        /// <param name="expected">Expected guid.</param>
        public static void SameGuid(Guid actual, Guid expected)
        {
            if (actual != expected)
            {
                throw new TestFailedException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected guid {0} instead of guid {1}.",
                    expected.ToString(),
                    actual.ToString()));
            }
        }
    }
}
