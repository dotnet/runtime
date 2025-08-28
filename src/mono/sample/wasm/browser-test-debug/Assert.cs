using System;
using System.Collections.Generic;

namespace Sample
{
    public static class Assert
    {
        private static string FormatIfArray(object? obj)
        {
            if (obj is Array arr)
            {
                var items = new System.Text.StringBuilder();
                items.Append('[');
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i > 0) items.Append(", ");
                    var value = arr.GetValue(i);
                    // Recursively format nested arrays
                    items.Append(FormatIfArray(value));
                }
                items.Append(']');
                return items.ToString();
            }
            // For collections (e.g., List<object>)
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                var items = new System.Text.StringBuilder();
                items.Append('[');
                bool first = true;
                foreach (var value in enumerable)
                {
                    if (!first) items.Append(", ");
                    items.Append(FormatIfArray(value));
                    first = false;
                }
                items.Append(']');
                return items.ToString();
            }
            return obj?.ToString() ?? "null";
        }

        public static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex is TException expected)
                    return expected;
                throw new Exception($"AssertHelper.Throws failed. Expected exception of type {typeof(TException)}, but got {ex.GetType()}.\nMessage: {ex.Message}", ex);
            }
            throw new Exception($"AssertHelper.Throws failed. No exception was thrown. Expected exception of type {typeof(TException)}.");
        }
        public static void Contains<T>(T expected, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new Exception("AssertHelper.Contains failed. Collection is null.");
            foreach (var item in collection)
            {
                if (object.Equals(item, expected))
                    return;
            }
            throw new Exception($"AssertHelper.Contains failed. Expected item: {FormatIfArray(expected)} was not found in collection: {FormatIfArray(collection)}.");
        }

        public static void Contains(string expectedSubstring, string? actualString)
        {
            if (actualString == null)
                throw new Exception("AssertHelper.Contains failed. Actual string is null.");
            if (!actualString.Contains(expectedSubstring, StringComparison.Ordinal))
                throw new Exception($"AssertHelper.Contains failed. Expected substring: \"{expectedSubstring}\" was not found in string: \"{actualString}\".");
        }

        public static void DoesNotContain<T>(T expected, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new Exception("AssertHelper.DoesNotContain failed. Collection is null.");
            foreach (var item in collection)
            {
                if (object.Equals(item, expected))
                    throw new Exception($"AssertHelper.DoesNotContain failed. Item: {FormatIfArray(expected)} was found in collection: {FormatIfArray(collection)}.");
            }
        }

        public static void DoesNotContain(string expectedSubstring, string? actualString)
        {
            if (actualString == null)
                return;
            if (actualString.Contains(expectedSubstring, StringComparison.Ordinal))
                throw new Exception($"AssertHelper.DoesNotContain failed. Substring: \"{expectedSubstring}\" was found in string: \"{actualString}\".");
        }
        public static void NotEqual<T>(T expected, T actual)
        {
            if (object.Equals(expected, actual))
            {
                string expectedStr = FormatIfArray(expected);
                string actualStr = FormatIfArray(actual);
                throw new Exception($"AssertHelper.NotEqual failed. Expected: {expectedStr}. Actual: {actualStr}.");
            }
        }
        public static void Equal<T>(T expected, T actual)
        {
            // Handle nulls
            if (ReferenceEquals(expected, actual))
                return;
            if (expected is null || actual is null)
                throw new Exception($"AssertHelper.Equal failed. Expected: {FormatIfArray(expected)}, Actual: {FormatIfArray(actual)}.");

            // Recursively compare arrays
            if (expected is Array expectedArray && actual is Array actualArray)
            {
                if (expectedArray.Length != actualArray.Length)
                    throw new Exception($"AssertHelper.Equal failed. Array lengths differ. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}.");

                for (int i = 0; i < expectedArray.Length; i++)
                {
                    var e = expectedArray.GetValue(i);
                    var a = actualArray.GetValue(i);
                    try
                    {
                        // Recursively call Equal for nested arrays/objects
                        Equal(e, a);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"AssertHelper.Equal failed at index {i}. Expected: {FormatIfArray(expectedArray)}, Actual: {FormatIfArray(actualArray)}. Inner: {ex.Message}");
                    }
                }
                return;
            }

            // Fallback to default equality
            if (!object.Equals(expected, actual))
            {
                throw new Exception($"AssertHelper.Equal failed. Expected: {FormatIfArray(expected)}, Actual: {FormatIfArray(actual)}.");
            }
        }
        public static void Same(object? expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual))
            {
                string expectedStr = FormatIfArray(expected);
                string actualStr = FormatIfArray(actual);
                throw new Exception(
                    $"AssertHelper.Same failed. Expected and actual are not the same reference.\nExpected: {expectedStr}\nActual: {actualStr}"
                );
            }
        }
        public static void True(bool condition)
        {
            if (!condition)
                throw new Exception("AssertHelper.True failed. Condition was false.");
        }
        public static void True(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"AssertHelper.True failed. Condition was false. {message}");
        }
        public static void False(bool condition)
        {
            if (condition)
                throw new Exception("AssertHelper.False failed. Condition was true.");
        }
        public static void StartsWith(string expectedStart, string? actual)
        {
            if (actual == null)
                throw new Exception($"AssertHelper.StartsWith failed. Actual string was null. Expected start: \"{expectedStart}\".");

            if (!actual.StartsWith(expectedStart, StringComparison.Ordinal))
                throw new Exception($"AssertHelper.StartsWith failed. Expected string starting with: \"{expectedStart}\". Actual: \"{actual}\".");
        }
        public static void Null(object? expected)
        {
            if (expected != null)
                throw new Exception($"AssertHelper.Null failed. Expected null, but got: {FormatIfArray(expected)}.");
        }
    }
}
