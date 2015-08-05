// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Sdk;

// CoreCLR on Unix today lacks support for PDBs.  As a result, failed xunit asserts don't include
// file and line information, making it more difficult and time intensive to debug various
// failures.
//
// This file provides stubs that delegate to xunit but that use [Caller*] attributes
// to capture file/line number information from call sites and include that information in
// assertion failure messages.  This file can then be included into a test project to help with
// debugging, and any unqualified references to Assert should bind to this internal, namespace-less 
// Assert class rather than to Xunit.Assert from a referenced assembly.
//
// This stub class includes wrappers for almost all of the Xunit.Assert's members.  Those missing
// or slightly modified were left out due to one of the following reasons:
// - The member's signature referred to types from contracts unlikely to be referenced (e.g. Regex)
// - The member's signature used optional parameters that conflicted with the [Caller*] attributes
// - The member's signature used params arrays that conflicted with the [Caller*] attributes
//
// Assertion failures will result in an Exception getting thrown; that Exception wraps the original
// XunitException and has a message containing the file/line number info.

internal static class Assert
{
    public static void All<T>(
        IEnumerable<T> collection, Action<T> action,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.All(collection, action); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Collection<T>(IEnumerable<T> collection, Action<T>[] elementInspectors, // removed params from elementInspectors
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Collection(collection, elementInspectors); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Contains(string expectedSubstring, string actualString,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Contains(expectedSubstring, actualString); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Contains(string expectedSubstring, string actualString, StringComparison comparisonType,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Contains(expectedSubstring, actualString, comparisonType); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Contains<T>(IEnumerable<T> collection, Predicate<T> filter,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Contains(collection, filter); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Contains(expected, collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Contains(expected, collection, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void DoesNotContain(string expectedSubstring, string actualString,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.DoesNotContain(expectedSubstring, actualString); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void DoesNotContain(string expectedSubstring, string actualString, StringComparison comparisonType,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.DoesNotContain(expectedSubstring, actualString, comparisonType); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void DoesNotContain<T>(IEnumerable<T> collection, Predicate<T> filter,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.DoesNotContain(collection, filter); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.DoesNotContain(expected, collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.DoesNotContain(expected, collection, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Empty(IEnumerable collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Empty(collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void EndsWith(string expectedEndString, string actualString,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.EndsWith(expectedEndString, actualString); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void EndsWith(string expectedEndString, string actualString, StringComparison comparisonType,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.EndsWith(expectedEndString, actualString, comparisonType); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal(string expected, string actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal(decimal expected, decimal actual, int precision,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual, precision); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal(double expected, double actual, int precision,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual, precision); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal(string expected, string actual, bool ignoreCase, bool ignoreLineEndingDifferences, bool ignoreWhiteSpaceDifferences, // made bools non-optional
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual, ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal<T>(T expected, T actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal<T>(T expected, T actual, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Equal(expected, actual, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This is an override of Object.Equals(). Call Assert.Equal() instead.", true)]
    public static bool Equals(object a, object b,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.Equals(a, b); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void False(bool condition, string userMessage = null,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try
        {
            if (userMessage == null)
            {
                Xunit.Assert.False(condition);
            }
            else
            {
                Xunit.Assert.False(condition, userMessage);
            }
        }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void InRange<T>(T actual, T low, T high,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : IComparable
    {
        try { Xunit.Assert.InRange(actual, low, high); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void InRange<T>(T actual, T low, T high, IComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.InRange(actual, low, high, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void IsAssignableFrom(Type expectedType, object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.IsAssignableFrom(expectedType, @object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T IsAssignableFrom<T>(object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.IsAssignableFrom<T>(@object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void IsNotType(Type expectedType, object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.IsNotType(expectedType, @object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void IsNotType<T>(object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.IsNotType<T>(@object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void IsType(Type expectedType, object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.IsType(expectedType, @object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T IsType<T>(object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.IsType<T>(@object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEmpty(IEnumerable collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEmpty(collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual(decimal expected, decimal actual, int precision,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual, precision); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual(double expected, double actual, int precision,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual, precision); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual<T>(T expected, T actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual<T>(T expected, T actual, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotEqual(expected, actual, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotInRange<T>(T actual, T low, T high,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : IComparable
    {
        try { Xunit.Assert.NotInRange(actual, low, high); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotInRange<T>(T actual, T low, T high, IComparer<T> comparer,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotInRange(actual, low, high, comparer); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotNull(object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotNull(@object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotSame(object expected, object actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotSame(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void NotStrictEqual<T>(T expected, T actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.NotStrictEqual(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Null(object @object,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Null(@object); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void ProperSubset<T>(ISet<T> expectedSuperset, ISet<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.ProperSubset(expectedSuperset, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void ProperSuperset<T>(ISet<T> expectedSubset, ISet<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.ProperSuperset(expectedSubset, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This is an override of Object.ReferenceEquals(). Call Assert.Same() instead.", true)]
    public static bool ReferenceEquals(object a, object b,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.ReferenceEquals(a, b); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Same(object expected, object actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Same(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static object Single(IEnumerable collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.Single(collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Single(IEnumerable collection, object expected,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Single(collection, expected); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Single<T>(IEnumerable<T> collection,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.Single(collection); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Single<T>(IEnumerable<T> collection, Predicate<T> predicate,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.Single(collection, predicate); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void StartsWith(string expectedStartString, string actualString,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.StartsWith(expectedStartString, actualString); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void StartsWith(string expectedStartString, string actualString, StringComparison comparisonType,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.StartsWith(expectedStartString, actualString, comparisonType); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void StrictEqual<T>(T expected, T actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.StrictEqual(expected, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Subset<T>(ISet<T> expectedSuperset, ISet<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Subset(expectedSuperset, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void Superset<T>(ISet<T> expectedSubset, ISet<T> actual,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { Xunit.Assert.Superset(expectedSubset, actual); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static Exception Throws(Type exceptionType, Func<object> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Throws(exceptionType, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static Exception Throws(Type exceptionType, Action testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return Xunit.Assert.Throws(exceptionType, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Throws<T>(Func<object> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return Xunit.Assert.Throws<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
    public static T Throws<T>(Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return Xunit.Assert.Throws<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Throws<T>(Action testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return Xunit.Assert.Throws<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("You must call Assert.ThrowsAsync<T> (and await the result) when testing async code.", true)]
    public static T Throws<T>(string paramName, Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : ArgumentException
    {
        try { return Xunit.Assert.Throws<T>(paramName, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Throws<T>(string paramName, Func<object> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : ArgumentException
    {
        try { return Xunit.Assert.Throws<T>(paramName, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T Throws<T>(string paramName, Action testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : ArgumentException
    {
        try { return Xunit.Assert.Throws<T>(paramName, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T ThrowsAny<T>(Func<object> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return Xunit.Assert.ThrowsAny<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static T ThrowsAny<T>(Action testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return Xunit.Assert.ThrowsAny<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static async Task<T> ThrowsAnyAsync<T>(Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return await Xunit.Assert.ThrowsAnyAsync<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static async Task<Exception> ThrowsAsync(Type exceptionType, Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try { return await Xunit.Assert.ThrowsAsync(exceptionType, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static async Task<T> ThrowsAsync<T>(Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : Exception
    {
        try { return await Xunit.Assert.ThrowsAsync<T>(testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static async Task<T> ThrowsAsync<T>(string paramName, Func<Task> testCode,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0) where T : ArgumentException
    {
        try { return await Xunit.Assert.ThrowsAsync<T>(paramName, testCode); }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    public static void True(bool condition, string userMessage = null,
        [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
    {
        try
        {
            if (userMessage == null)
            {
                Xunit.Assert.True(condition);
            }
            else
            {
                Xunit.Assert.True(condition, userMessage);
            }
        }
        catch (Exception e) { throw WrapException(e, path, line); }
    }

    private static XunitException WrapException(Exception e, string callerFilePath, int callerLineNumber)
    {
        throw new WrapperXunitException(string.Format("File path: {0}. Line: {1}", callerFilePath, callerLineNumber), e);
    }

    // XunitException exposes its (string, Exception) ctor as protected, not public,
    // so to use it we derive a custom exception type
    internal sealed class WrapperXunitException : XunitException
    {
        internal WrapperXunitException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
