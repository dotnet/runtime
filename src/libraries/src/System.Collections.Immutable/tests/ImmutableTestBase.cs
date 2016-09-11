// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public abstract class ImmutablesTestBase
    {
        /// <summary>
        /// Gets the number of operations to perform in randomized tests.
        /// </summary>
        protected int RandomOperationsCount
        {
            get { return 100; }
        }

        internal static void AssertAreSame<T>(T expected, T actual, string message = null, params object[] formattingArgs)
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                Assert.Equal(expected, actual); //, message, formattingArgs);
            }
            else
            {
                Assert.Same(expected, actual); //, message, formattingArgs);
            }
        }

        internal static void CollectionAssertAreEquivalent<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var value in expected)
            {
                Assert.Contains(value, actual);
            }
        }

        protected static string ToString(System.Collections.IEnumerable sequence)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            int count = 0;
            foreach (object item in sequence)
            {
                if (count > 0)
                {
                    sb.Append(',');
                }

                if (count == 10)
                {
                    sb.Append("...");
                    break;
                }

                sb.Append(item);
                count++;
            }

            sb.Append('}');
            return sb.ToString();
        }

        protected static object ToStringDeferred(System.Collections.IEnumerable sequence)
        {
            return new DeferredToString(() => ToString(sequence));
        }

        protected static void ManuallyEnumerateTest<T>(IList<T> expectedResults, IEnumerator<T> enumerator)
        {
            T[] manualArray = new T[expectedResults.Count];
            int i = 0;

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext())
            {
                manualArray[i++] = enumerator.Current;
            }

            enumerator.MoveNext();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.MoveNext();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            Assert.Equal(expectedResults.Count, i); //, "Enumeration did not produce enough elements.");
            Assert.Equal<T>(expectedResults, manualArray);
        }

        /// <summary>
        /// Generates an array of unique values.
        /// </summary>
        /// <param name="length">The desired length of the array.</param>
        /// <returns>An array of doubles.</returns>
        protected double[] GenerateDummyFillData(int length = 1000)
        {
            Assert.InRange(length, 0, int.MaxValue);

            int seed = unchecked((int)DateTime.Now.Ticks);

            Debug.WriteLine("Random seed {0}", seed);

            var random = new Random(seed);
            var inputs = new double[length];
            var ensureUniqueness = new HashSet<double>();
            for (int i = 0; i < inputs.Length; i++)
            {
                double input;
                do
                {
                    input = random.NextDouble();
                }
                while (!ensureUniqueness.Add(input));
                inputs[i] = input;
            }

            Assert.NotNull(inputs);
            Assert.Equal(length, inputs.Length);

            return inputs;
        }

        /// <summary>
        /// Tests the EqualsStructurally public method and the IStructuralEquatable.Equals method.
        /// </summary>
        /// <typeparam name="TCollection">The type of tested collection.</typeparam>
        /// <typeparam name="TElement">The type of element stored in the collection.</typeparam>
        /// <param name="objectUnderTest">An instance of the collection to test, which must have at least two elements.</param>
        /// <param name="additionalItem">A unique item that does not already exist in <paramref name="objectUnderTest" />.</param>
        /// <param name="equalsStructurally">A delegate that invokes the EqualsStructurally method.</param>
        protected static void StructuralEqualityHelper<TCollection, TElement>(TCollection objectUnderTest, TElement additionalItem, Func<TCollection, IEnumerable<TElement>, bool> equalsStructurally)
            where TCollection : class, IEnumerable<TElement>
        {
            Requires.NotNull(objectUnderTest, nameof(objectUnderTest));
            Requires.Argument(objectUnderTest.Count() >= 2, nameof(objectUnderTest), "Collection must contain at least two elements.");
            Requires.NotNull(equalsStructurally, nameof(equalsStructurally));

            var structuralEquatableUnderTest = objectUnderTest as IStructuralEquatable;
            var enumerableUnderTest = (IEnumerable<TElement>)objectUnderTest;

            var equivalentSequence = objectUnderTest.ToList();
            var shorterSequence = equivalentSequence.Take(equivalentSequence.Count() - 1);
            var longerSequence = equivalentSequence.Concat(new[] { additionalItem });
            var differentSequence = shorterSequence.Concat(new[] { additionalItem });
            var nonUniqueSubsetSequenceOfSameLength = shorterSequence.Concat(shorterSequence.Take(1));

            var testValues = new IEnumerable<TElement>[] {
                objectUnderTest,
                null,
                Enumerable.Empty<TElement>(),
                equivalentSequence,
                longerSequence,
                shorterSequence,
                nonUniqueSubsetSequenceOfSameLength,
            };

            foreach (var value in testValues)
            {
                bool expectedResult = value != null && Enumerable.SequenceEqual(objectUnderTest, value);

                if (structuralEquatableUnderTest != null)
                {
                    Assert.Equal(expectedResult, structuralEquatableUnderTest.Equals(value, null));

                    if (value != null)
                    {
                        Assert.Equal(
                            expectedResult,
                            structuralEquatableUnderTest.Equals(new NonGenericEnumerableWrapper(value), null));
                    }
                }

                Assert.Equal(expectedResult, equalsStructurally(objectUnderTest, value));
            }
        }

        private class DeferredToString
        {
            private readonly Func<string> _generator;

            internal DeferredToString(Func<string> generator)
            {
                Debug.Assert(generator != null);
                _generator = generator;
            }

            public override string ToString()
            {
                return _generator();
            }
        }

        private class NonGenericEnumerableWrapper : IEnumerable
        {
            private readonly IEnumerable _enumerable;

            internal NonGenericEnumerableWrapper(IEnumerable enumerable)
            {
                Requires.NotNull(enumerable, nameof(enumerable));
                _enumerable = enumerable;
            }

            public IEnumerator GetEnumerator()
            {
                return _enumerable.GetEnumerator();
            }
        }
    }
}
