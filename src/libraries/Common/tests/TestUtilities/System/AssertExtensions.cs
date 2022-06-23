// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using System.Linq;

namespace System
{
    public static class AssertExtensions
    {
        private static bool IsNetFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        public static void Throws<T>(Action action, string expectedMessage)
            where T : Exception
        {
            Assert.Equal(expectedMessage, Assert.Throws<T>(action).Message);
        }

        public static void ThrowsContains<T>(Action action, string expectedMessageContent)
            where T : Exception
        {
            Assert.Contains(expectedMessageContent, Assert.Throws<T>(action).Message);
        }

        public static T Throws<T>(string netCoreParamName, string netFxParamName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            if (netFxParamName == null && IsNetFramework)
            {
                // Param name varies between .NET Framework versions -- skip checking it
                return exception;
            }

            string expectedParamName =
                IsNetFramework ?
                netFxParamName : netCoreParamName;

            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        public static void Throws<T>(string netCoreParamName, string netFxParamName, Func<object> testCode)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(testCode);

            if (netFxParamName == null && IsNetFramework)
            {
                // Param name varies between .NET Framework versions -- skip checking it
                return;
            }

            string expectedParamName =
                IsNetFramework ?
                netFxParamName : netCoreParamName;

            Assert.Equal(expectedParamName, exception.ParamName);
        }

        public static T Throws<T>(string expectedParamName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            Assert.Equal(expectedParamName, exception.ParamName);

            return exception;
        }

        public static T Throws<T>(Action action)
            where T : Exception
        {
            T exception = Assert.Throws<T>(action);

            return exception;
        }

        public static TException Throws<TException, TResult>(Func<TResult> func)
            where TException : Exception
        {
            object result = null;
            bool returned = false;
            try
            {
                return
                    Assert.Throws<TException>(() =>
                    {
                        result = func();
                        returned = true;
                    });
            }
            catch (Exception ex) when (returned)
            {
                string resultStr;
                if (result == null)
                {
                    resultStr = "(null)";
                }
                else
                {
                    resultStr = result.ToString();
                    if (typeof(TResult) == typeof(string))
                    {
                        resultStr = $"\"{resultStr}\"";
                    }
                }

                throw new AggregateException($"Result: {resultStr}", ex);
            }
        }

        public static T Throws<T>(string expectedParamName, Func<object> testCode)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(testCode);

            Assert.Equal(expectedParamName, exception.ParamName);

            return exception;
        }

        public static async Task<T> ThrowsAsync<T>(string expectedParamName, Func<Task> testCode)
            where T : ArgumentException
        {
            T exception = await Assert.ThrowsAsync<T>(testCode);

            Assert.Equal(expectedParamName, exception.ParamName);

            return exception;
        }

        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string expectedParamName, Action action)
            where TNetCoreExceptionType : ArgumentException
            where TNetFxExceptionType : Exception
        {
            if (IsNetFramework)
            {
                // Support cases where the .NET Core exception derives from ArgumentException
                // but the .NET Framework exception is not.
                if (typeof(ArgumentException).IsAssignableFrom(typeof(TNetFxExceptionType)))
                {
                    Exception exception = Assert.Throws(typeof(TNetFxExceptionType), action);
                    Assert.Equal(expectedParamName, ((ArgumentException)exception).ParamName);
                }
                else
                {
                    AssertExtensions.Throws<TNetFxExceptionType>(action);
                }
            }
            else
            {
                AssertExtensions.Throws<TNetCoreExceptionType>(expectedParamName, action);
            }
        }

        public static Exception Throws<TNetCoreExceptionType, TNetFxExceptionType>(Action action)
            where TNetCoreExceptionType : Exception
            where TNetFxExceptionType : Exception
        {
            return Throws(typeof(TNetCoreExceptionType), typeof(TNetFxExceptionType), action);
        }

        public static Exception Throws(Type netCoreExceptionType, Type netFxExceptionType, Action action)
        {
            if (IsNetFramework)
            {
                return Assert.Throws(netFxExceptionType, action);
            }
            else
            {
                return Assert.Throws(netCoreExceptionType, action);
            }
        }

        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string netCoreParamName, string netFxParamName, Action action)
            where TNetCoreExceptionType : ArgumentException
            where TNetFxExceptionType : ArgumentException
        {
            if (IsNetFramework)
            {
                Throws<TNetFxExceptionType>(netFxParamName, action);
            }
            else
            {
                Throws<TNetCoreExceptionType>(netCoreParamName, action);
            }
        }

        public static void ThrowsAny(Type firstExceptionType, Type secondExceptionType, Action action)
        {
            ThrowsAnyInternal(action, firstExceptionType, secondExceptionType);
        }

        private static void ThrowsAnyInternal(Action action, params Type[] exceptionTypes)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Type exceptionType = e.GetType();
                if (exceptionTypes.Any(t => t.Equals(exceptionType)))
                    return;

                throw new XunitException($"Expected one of: ({string.Join<Type>(", ", exceptionTypes)}) -> Actual: ({exceptionType}): {e}"); // Log message and callstack to help diagnosis
            }

            throw new XunitException($"Expected one of: ({string.Join<Type>(", ", exceptionTypes)}) -> Actual: No exception thrown");
        }

        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType>(Action action)
            where TFirstExceptionType : Exception
            where TSecondExceptionType : Exception
        {
            ThrowsAnyInternal(action, typeof(TFirstExceptionType), typeof(TSecondExceptionType));
        }

        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType, TThirdExceptionType>(Action action)
            where TFirstExceptionType : Exception
            where TSecondExceptionType : Exception
            where TThirdExceptionType : Exception
        {
            ThrowsAnyInternal(action, typeof(TFirstExceptionType), typeof(TSecondExceptionType), typeof(TThirdExceptionType));
        }

        public static void ThrowsIf<T>(bool condition, Action action)
            where T : Exception
        {
            if (condition)
            {
                Assert.Throws<T>(action);
            }
            else
            {
                action();
            }
        }

        private static string AddOptionalUserMessage(string message, string userMessage)
        {
            if (userMessage == null)
                return message;
            else
                return $"{message} {userMessage}";
        }

        /// <summary>
        /// Tests whether the specified string contains the specified substring
        /// and throws an exception if the substring does not occur within the
        /// test string or if either string or substring is null.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to contain <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to occur within <paramref name="value"/>.
        /// </param>
        public static void Contains(string value, string substring)
        {
            Assert.NotNull(value);
            Assert.NotNull(substring);
            Assert.Contains(substring, value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Validate that a given value is greater than another value.
        /// </summary>
        /// <param name="actual">The value that should be greater than <paramref name="greaterThan"/>.</param>
        /// <param name="greaterThan">The value that <paramref name="actual"/> should be greater than.</param>
        public static void GreaterThan<T>(T actual, T greaterThan, string userMessage = null) where T : IComparable
        {
            if (actual == null)
                throw new XunitException(
                    greaterThan == null
                        ? AddOptionalUserMessage($"Expected: <null> to be greater than <null>.", userMessage)
                        : AddOptionalUserMessage($"Expected: <null> to be greater than {greaterThan}.", userMessage));

            if (actual.CompareTo(greaterThan) <= 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be greater than {greaterThan}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is less than another value.
        /// </summary>
        /// <param name="actual">The value that should be less than <paramref name="lessThan"/>.</param>
        /// <param name="lessThan">The value that <paramref name="actual"/> should be less than.</param>
        public static void LessThan<T>(T actual, T lessThan, string userMessage = null) where T : IComparable
        {
            if (actual == null)
            {
                if (lessThan == null)
                {
                    throw new XunitException(AddOptionalUserMessage($"Expected: <null> to be less than <null>.", userMessage));
                }
                else
                {
                    // Null is always less than non-null
                    return;
                }
            }

            if (actual.CompareTo(lessThan) >= 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be less than {lessThan}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is less than or equal to another value.
        /// </summary>
        /// <param name="actual">The value that should be less than or equal to <paramref name="lessThanOrEqualTo"/></param>
        /// <param name="lessThanOrEqualTo">The value that <paramref name="actual"/> should be less than or equal to.</param>
        public static void LessThanOrEqualTo<T>(T actual, T lessThanOrEqualTo, string userMessage = null) where T : IComparable
        {
            // null, by definition is always less than or equal to
            if (actual == null)
                return;

            if (actual.CompareTo(lessThanOrEqualTo) > 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be less than or equal to {lessThanOrEqualTo}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is greater than or equal to another value.
        /// </summary>
        /// <param name="actual">The value that should be greater than or equal to <paramref name="greaterThanOrEqualTo"/></param>
        /// <param name="greaterThanOrEqualTo">The value that <paramref name="actual"/> should be greater than or equal to.</param>
        public static void GreaterThanOrEqualTo<T>(T actual, T greaterThanOrEqualTo, string userMessage = null) where T : IComparable
        {
            // null, by definition is always less than or equal to
            if (actual == null)
            {
                if (greaterThanOrEqualTo == null)
                {
                    // We're equal
                    return;
                }
                else
                {
                    // Null is always less than non-null
                    throw new XunitException(AddOptionalUserMessage($"Expected: <null> to be greater than or equal to <null>.", userMessage));
                }
            }

            if (actual.CompareTo(greaterThanOrEqualTo) < 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be greater than or equal to {greaterThanOrEqualTo}", userMessage));
        }

        // NOTE: Consider using SequenceEqual below instead, as it will give more useful information about what
        // the actual differences are, especially for large arrays/spans.
        /// <summary>
        /// Validates that the actual array is equal to the expected array. XUnit only displays the first 5 values
        /// of each collection if the test fails. This doesn't display at what point or how the equality assertion failed.
        /// </summary>
        /// <param name="expected">The array that <paramref name="actual"/> should be equal to.</param>
        /// <param name="actual"></param>
        public static void Equal<T>(T[] expected, T[] actual) where T : IEquatable<T>
        {
            // Use the SequenceEqual to compare the arrays for better performance. The default Assert.Equal method compares
            // the arrays by boxing each element that is very slow for large arrays.
            if (!expected.AsSpan().SequenceEqual(actual.AsSpan()))
            {
                string expectedString = string.Join(", ", expected);
                string actualString = string.Join(", ", actual);
                throw new AssertActualExpectedException(expectedString, actualString, null);
            }
        }

        /// <summary>Validates that the two sets contains the same elements. XUnit doesn't display the full collections.</summary>
        public static void Equal<T>(HashSet<T> expected, HashSet<T> actual)
        {
            if (!actual.SetEquals(expected))
            {
                throw new XunitException($"Expected: {string.Join(", ", expected)}{Environment.NewLine}Actual: {string.Join(", ", actual)}");
            }
        }

        /// <summary>
        /// Validates that the actual collection contains same items as expected collection. If the test fails, this will display:
        /// 1. Count if two collection count are different;
        /// 2. Missed expected collection item when found
        /// </summary>
        /// <param name="expected">The collection that <paramref name="actual"/> should contain same items as</param>
        /// <param name="actual"></param>
        /// <param name="comparer">The comparer used to compare the items in two collections</param>
        public static void CollectionEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            var actualItemCountMapping = new Dictionary<T, ItemCount>(comparer);
            int actualCount = 0;
            foreach (T actualItem in actual)
            {
                if (actualItemCountMapping.TryGetValue(actualItem, out ItemCount countInfo))
                {
                    countInfo.Original++;
                    countInfo.Remain++;
                }
                else
                {
                    actualItemCountMapping[actualItem] = new ItemCount(1, 1);
                }

                actualCount++;
            }

            var expectedArray = expected.ToArray();
            var expectedCount = expectedArray.Length;

            if (expectedCount != actualCount)
            {
                throw new XunitException($"Expected count: {expectedCount}{Environment.NewLine}Actual count: {actualCount}");
            }

            for (var i = 0; i < expectedCount; i++)
            {
                T currentExpectedItem = expectedArray[i];
                if (!actualItemCountMapping.TryGetValue(currentExpectedItem, out ItemCount countInfo))
                {
                    throw new XunitException($"Expected: {currentExpectedItem} but not found");
                }

                if (countInfo.Remain == 0)
                {
                    throw new XunitException($"Collections are not equal.{Environment.NewLine}Totally {countInfo.Original} {currentExpectedItem} in actual collection but expect more {currentExpectedItem}");
                }

                countInfo.Remain--;
            }
        }

        /// <summary>
        /// Validates that the actual span is equal to the expected span.
        /// If this fails, determine where the differences are and create an exception with that information.
        /// </summary>
        /// <param name="expected">The array that <paramref name="actual"/> should be equal to.</param>
        /// <param name="actual"></param>
        public static void SequenceEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual) where T : IEquatable<T>
        {
            // Use the SequenceEqual to compare the arrays for better performance. The default Assert.Equal method compares
            // the arrays by boxing each element that is very slow for large arrays.
            if (!expected.SequenceEqual(actual))
            {
                if (expected.Length != actual.Length)
                {
                    throw new XunitException($"Expected: Span of length {expected.Length}{Environment.NewLine}Actual: Span of length {actual.Length}");
                }
                else
                {
                    const int MaxDiffsToShow = 10;      // arbitrary; enough to be useful, hopefully, but still manageable

                    int diffCount = 0;
                    string message = $"Showing first {MaxDiffsToShow} differences{Environment.NewLine}";
                    for (int i = 0; i < expected.Length; i++)
                    {
                        if (!expected[i].Equals(actual[i]))
                        {
                            diffCount++;

                            // Add up to 10 differences to the exception message
                            if (diffCount <= MaxDiffsToShow)
                            {
                                message += $"  Position {i}: Expected: {expected[i]}, Actual: {actual[i]}{Environment.NewLine}";
                            }
                        }
                    }

                    message += $"Total number of differences: {diffCount} out of {expected.Length}";

                    throw new XunitException(message);
                }
            }
        }

        public static void FilledWith<T>(T expected, ReadOnlySpan<T> actual)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < actual.Length; i++)
            {
                if (!comparer.Equals(expected, actual[i]))
                {
                    throw new XunitException($"Expected {expected?.ToString() ?? "null"} at position {i}; actual {actual[i]?.ToString() ?? "null"}");
                }
            }
        }

        public static void SequenceEqual<T>(Span<T> expected, Span<T> actual) where T : IEquatable<T> => SequenceEqual((ReadOnlySpan<T>)expected, (ReadOnlySpan<T>)actual);

        public static void SequenceEqual<T>(T[] expected, T[] actual) where T : IEquatable<T> => SequenceEqual(expected.AsSpan(), actual.AsSpan());

        public static void AtLeastOneEquals<T>(T expected1, T expected2, T value)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            if (!(comparer.Equals(value, expected1) || comparer.Equals(value, expected2)))
                throw new XunitException($"Expected: {expected1} || {expected2}{Environment.NewLine}Actual: {value}");
        }

        /// <summary>
        /// Compares two strings, logs entire content if they are not equal.
        /// </summary>
        public static void Equal(string expected, string actual)
        {
            if (!expected.Equals(actual))
            {
                throw new AssertActualExpectedException(expected, actual, "Provided strings were not equal!");
            }
        }

        public delegate void AssertThrowsActionReadOnly<T>(ReadOnlySpan<T> span);

        public delegate void AssertThrowsAction<T>(Span<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static E AssertThrows<E, T>(ReadOnlySpan<T> span, AssertThrowsActionReadOnly<T> action) where E : Exception
        {
            Exception exception;

            try
            {
                action(span);
                exception = null;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            switch(exception)
            {
                case null:
                    throw new ThrowsException(typeof(E));
                case E ex when (ex.GetType() == typeof(E)):
                    return ex;
                default:
                    throw new ThrowsException(typeof(E), exception);
            }
        }

        public static E AssertThrows<E, T>(Span<T> span, AssertThrowsAction<T> action) where E : Exception
        {
            Exception exception;

            try
            {
                action(span);
                exception = null;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            switch(exception)
            {
                case null:
                    throw new ThrowsException(typeof(E));
                case E ex when (ex.GetType() == typeof(E)):
                    return ex;
                default:
                    throw new ThrowsException(typeof(E), exception);
            }
        }

        public static E Throws<E, T>(string expectedParamName, ReadOnlySpan<T> span, AssertThrowsActionReadOnly<T> action)
            where E : ArgumentException
        {
            E exception = AssertThrows<E, T>(span, action);
            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        public static E Throws<E, T>(string expectedParamName, Span<T> span, AssertThrowsAction<T> action)
            where E : ArgumentException
        {
            E exception = AssertThrows<E, T>(span, action);
            Assert.Equal(expectedParamName, exception.ParamName);
            return exception;
        }

        private class ItemCount
        {
            public int Original { get; set; }
            public int Remain { get; set; }

            public ItemCount(int original, int remain)
            {
                Original = original;
                Remain = remain;
            }
        }

        /// <summary>Verifies that two <see cref="double"/> values are equal, within the <paramref name="allowedVariance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="allowedVariance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void Equal(double expected, double actual, double variance)
        {
            if (double.IsNaN(expected))
            {
                if (double.IsNaN(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsNaN(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (double.IsNegativeInfinity(expected))
            {
                if (double.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsNegativeInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (double.IsPositiveInfinity(expected))
            {
                if (double.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (double.IsPositiveInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (IsNegativeZero(expected))
            {
                if (IsNegativeZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsNegativeZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            if (IsPositiveZero(expected))
            {
                if (IsPositiveZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsPositiveZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            var delta = Math.Abs(actual - expected);

            if (delta > variance)
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            static unsafe bool IsNegativeZero(double value)
            {
                return (*(ulong*)(&value)) == 0x8000000000000000;
            }

            static unsafe bool IsPositiveZero(double value)
            {
                return (*(ulong*)(&value)) == 0x0000000000000000;
            }

            // We have a custom ToString here to ensure that edge cases (specifically +-0.0,
            // but also NaN and +-infinity) are correctly and consistently represented.
            static string ToStringPadded(double value)
            {
                if (double.IsNaN(value))
                {
                    return "NaN".PadLeft(20);
                }
                else if (double.IsPositiveInfinity(value))
                {
                    return "+\u221E".PadLeft(20);
                }
                else if (double.IsNegativeInfinity(value))
                {
                    return "-\u221E".PadLeft(20);
                }
                else if (IsNegativeZero(value))
                {
                    return "-0.0".PadLeft(20);
                }
                else if (IsPositiveZero(value))
                {
                    return "+0.0".PadLeft(20);
                }
                else
                {
                    return $"{value,20:G17}";
                }
            }
        }

        /// <summary>Verifies that two <see cref="float"/> values are equal, within the <paramref name="variance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void Equal(float expected, float actual, float variance)
        {
            if (float.IsNaN(expected))
            {
                if (float.IsNaN(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsNaN(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (float.IsNegativeInfinity(expected))
            {
                if (float.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsNegativeInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (float.IsPositiveInfinity(expected))
            {
                if (float.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (float.IsPositiveInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (IsNegativeZero(expected))
            {
                if (IsNegativeZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsNegativeZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            if (IsPositiveZero(expected))
            {
                if (IsPositiveZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsPositiveZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            var delta = Math.Abs(actual - expected);

            if (delta > variance)
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            static unsafe bool IsNegativeZero(float value)
            {
                return (*(uint*)(&value)) == 0x80000000;
            }

            static unsafe bool IsPositiveZero(float value)
            {
                return (*(uint*)(&value)) == 0x00000000;
            }

            // We have a custom ToString here to ensure that edge cases (specifically +-0.0,
            // but also NaN and +-infinity) are correctly and consistently represented.
            static string ToStringPadded(float value)
            {
                if (float.IsNaN(value))
                {
                    return "NaN".PadLeft(10);
                }
                else if (float.IsPositiveInfinity(value))
                {
                    return "+\u221E".PadLeft(10);
                }
                else if (float.IsNegativeInfinity(value))
                {
                    return "-\u221E".PadLeft(10);
                }
                else if (IsNegativeZero(value))
                {
                    return "-0.0".PadLeft(10);
                }
                else if (IsPositiveZero(value))
                {
                    return "+0.0".PadLeft(10);
                }
                else
                {
                    return $"{value,10:G9}";
                }
            }
        }

#if NET6_0_OR_GREATER
        /// <summary>Verifies that two <see cref="Half"/> values are equal, within the <paramref name="variance"/>.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void Equal(Half expected, Half actual, Half variance)
        {
            if (Half.IsNaN(expected))
            {
                if (Half.IsNaN(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (Half.IsNaN(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (Half.IsNegativeInfinity(expected))
            {
                if (Half.IsNegativeInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (Half.IsNegativeInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (Half.IsPositiveInfinity(expected))
            {
                if (Half.IsPositiveInfinity(actual))
                {
                    return;
                }

                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }
            else if (Half.IsPositiveInfinity(actual))
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            if (IsNegativeZero(expected))
            {
                if (IsNegativeZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsNegativeZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly -0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            if (IsPositiveZero(expected))
            {
                if (IsPositiveZero(actual))
                {
                    return;
                }

                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }
            else if (IsPositiveZero(actual))
            {
                if (IsPositiveZero(variance) || IsNegativeZero(variance))
                {
                    throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
                }

                // When the variance is not +-0.0, then we are handling a case where
                // the actual result is expected to not be exactly +0.0 on some platforms
                // and we should fallback to checking if it is within the allowed variance instead.
            }

            var delta = (Half)Math.Abs((float)actual - (float)expected);

            if (delta > variance)
            {
                throw new EqualException(ToStringPadded(expected), ToStringPadded(actual));
            }

            static unsafe bool IsNegativeZero(Half value)
            {
                return (*(ushort*)(&value)) == 0x8000;
            }

            static unsafe bool IsPositiveZero(Half value)
            {
                return (*(ushort*)(&value)) == 0x0000;
            }

            // We have a custom ToString here to ensure that edge cases (specifically +-0.0,
            // but also NaN and +-infinity) are correctly and consistently represented.
            static string ToStringPadded(Half value)
            {
                if (Half.IsNaN(value))
                {
                    return "NaN".PadLeft(5);
                }
                else if (Half.IsPositiveInfinity(value))
                {
                    return "+\u221E".PadLeft(5);
                }
                else if (Half.IsNegativeInfinity(value))
                {
                    return "-\u221E".PadLeft(5);
                }
                else if (IsNegativeZero(value))
                {
                    return "-0.0".PadLeft(5);
                }
                else if (IsPositiveZero(value))
                {
                    return "+0.0".PadLeft(5);
                }
                else
                {
                    return $"{value,5:G5}";
                }
            }
        }
#endif
    }
}
