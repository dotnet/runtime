// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class IncrementDecrementTests
    {
        public struct Decrementable
        {
            public Decrementable(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public static Decrementable operator --(Decrementable operand) => new Decrementable(unchecked(operand.Value - 1));
        }

        public struct Incrementable
        {
            public Incrementable(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public static Incrementable operator ++(Incrementable operand) => new Incrementable(unchecked(operand.Value + 1));
        }

        public static Incrementable DoublyIncrement(Incrementable operand) => new Incrementable(unchecked(operand.Value + 2));

        public static int DoublyIncrementInt32(int operand) => unchecked(operand + 2);

        public static Decrementable DoublyDecrement(Decrementable operand) => new Decrementable(unchecked(operand.Value - 2));

        public static int DoublyDecrementInt32(int operand) => unchecked(operand - 2);

        public static IEnumerable<object[]> NonArithmeticObjects(bool includeReferenceTypes)
        {
            if (includeReferenceTypes)
            {
                yield return new object[] {"One"};
            }

            yield return new object[] {DateTime.MaxValue};
            yield return new object[] {ExpressionType.Decrement};
        }

        public static IEnumerable<object[]> IncrementableValues(bool includeNulls)
        {
            yield return new object[] {new Incrementable(0), new Incrementable(1)};
            yield return new object[] {new Incrementable(-1), new Incrementable(0)};
            yield return new object[] {new Incrementable(int.MinValue), new Incrementable(int.MinValue + 1)};
            yield return new object[] {new Incrementable(int.MaxValue), new Incrementable(int.MinValue)};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }

        public static IEnumerable<object[]> DoublyIncrementedIncrementableValues(bool includeNulls)
        {
            yield return new object[] {new Incrementable(0), new Incrementable(2)};
            yield return new object[] {new Incrementable(-1), new Incrementable(1)};
            yield return new object[] {new Incrementable(int.MinValue), new Incrementable(int.MinValue + 2)};
            yield return new object[] {new Incrementable(int.MaxValue), new Incrementable(int.MinValue + 1)};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }

        public static IEnumerable<object[]> DoublyIncrementedInt32s(bool includeNulls)
        {
            yield return new object[] {0, 2};
            yield return new object[] {-1, 1};
            yield return new object[] {int.MinValue, int.MinValue + 2};
            yield return new object[] {int.MaxValue, int.MinValue + 1};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }

        public static IEnumerable<object[]> DecrementableValues(bool includeNulls)
        {
            yield return new object[] {new Decrementable(0), new Decrementable(-1)};
            yield return new object[] {new Decrementable(1), new Decrementable(0)};
            yield return new object[] {new Decrementable(int.MaxValue), new Decrementable(int.MaxValue - 1)};
            yield return new object[] {new Decrementable(int.MinValue), new Decrementable(int.MaxValue)};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }

        public static IEnumerable<object[]> DoublyDecrementedDecrementableValues(bool includeNulls)
        {
            yield return new object[] {new Decrementable(0), new Decrementable(-2)};
            yield return new object[] {new Decrementable(1), new Decrementable(-1)};
            yield return new object[] {new Decrementable(int.MaxValue), new Decrementable(int.MaxValue - 2)};
            yield return new object[] {new Decrementable(int.MinValue), new Decrementable(int.MaxValue - 1)};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }

        public static IEnumerable<object[]> DoublyDecrementedInt32s(bool includeNulls)
        {
            yield return new object[] {0, -2};
            yield return new object[] {1, -1};
            yield return new object[] {int.MaxValue, int.MaxValue - 2};
            yield return new object[] {int.MinValue, int.MaxValue - 1};

            if (includeNulls)
            {
                yield return new object[] {null, null};
            }
        }
    }
}
