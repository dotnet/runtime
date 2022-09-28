// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to provide some basic Assert functionality for assemblies that directly reference System.Private.CoreLib
// and not the ref pack.

// Note: Exception messages call ToString instead of Name to avoid MissingMetadataException when just outputting basic info

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xunit
{
    /// <summary>
    /// A copy of the Xunit.FactAttribute type for assemblies that reference System.Private.CoreLib directly.
    /// </summary>
    public sealed class FactAttribute : Attribute
    {}

    /// <summary>
    ///    A collection of helper classes to test various conditions within
    /// unit tests. If the condition being tested is not met, an exception
    /// is thrown.
    /// </summary>
    public static class Assert
    {
        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="Exception"/> of type <typeparam name="T" />.
        /// </summary>
        /// <param name="action">
        ///     The delegate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        /// </exception>
        public static T Throws<T>(Action action) where T : Exception
        {
            Exception exception = RunWithCatch(action);

            if (exception == null)
                Assert.True(false, $"Expected '{typeof(T)}' to be thrown.");

            if (exception is not T)
                Assert.True(false, $"Expected '{typeof(T)}' to be thrown, however '{exception.GetType()}' was thrown.");

            return (T)exception;
        }

        /// <summary>
        /// Tests whether the specified condition is true and throws an exception
        /// if the condition is false.
        /// </summary>
        /// <param name="condition">The condition the test expects to be true.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is false. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is false.
        /// </exception>
        public static void True(bool condition, string message = "")
        {
            if (!condition)
            {
                Assert.HandleFail("Assert.True", message);
            }
        }

        /// <summary>
        /// Tests whether the specified condition is false and throws an exception
        /// if the condition is true.
        /// </summary>
        /// <param name="condition">The condition the test expects to be false.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="condition"/>
        /// is true. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="condition"/> is true.
        /// </exception>
        public static void False(bool condition, string message = "")
        {
            if (condition)
            {
                Assert.HandleFail("Assert.False", message);
            }
        }

        /// <summary>
        /// Tests whether the specified object is null and throws an exception
        /// if it is not.
        /// </summary>
        /// <param name="value">The object the test expects to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is not null. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is not null.
        /// </exception>
        public static void Null(object value)
        {
            if (value != null)
            {
                Assert.HandleFail("Assert.Null", "");
            }
        }

        /// <summary>
        /// Tests whether the specified object is non-null and throws an exception
        /// if it is null.
        /// </summary>
        /// <param name="value">The object the test expects not to be null.</param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="value"/>
        /// is null. The message is shown in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="value"/> is null.
        /// </exception>
        public static void NotNull(object value)
        {
            if (value == null)
            {
                Assert.HandleFail("Assert.NotNull", "");
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void Equal<T>(T expected, T actual)
        {
            if (!Object.Equals(expected, actual))
            {
                Assert.HandleFail("Assert.Equal", "");
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void Same(object expected, object actual)
        {
            const string EXPECTED_MSG = @"Expected: [{1}]. Actual: [{2}]. {0}";

            if (!Object.ReferenceEquals(expected, actual))
            {
                Assert.HandleFail("Assert.Same", "");
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void Equal<T>(T expected, T actual, string format, params Object[] args)
        {
            Equal<T>(expected, actual);
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and
        /// throws an exception if it is not.
        /// </summary>
        /// </summary>
        /// <param name="notExpected">Expected object that we do not want it to be.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void NotEqual<T>(T notExpected, T actual)
        {
            if (Object.Equals(notExpected, actual))
            {
                Assert.HandleFail("Assert.NotEqual", "");
            }
        }

        /// <summary>
        /// Helper function that creates and throws an exception.
        /// </summary>
        /// <param name="assertionName">name of the assertion throwing an exception.</param>
        /// <param name="message">message describing conditions for assertion failure.</param>
        /// <param name="parameters">The parameters.</param>=
        internal static void HandleFail(string assertionName, string message)
        {
            throw new XunitException(assertionName + ": " + message);
        }


        [Obsolete("Did you mean to call Assert.Equal()")]
        public static new bool Equals(Object o1, Object o2)
        {
            Assert.True(false, "Don't call this.");
            throw new Exception();
        }

        private static Exception RunWithCatch(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }

    /// <summary>
    /// Exception raised by the Assert on Fail
    /// </summary>
    public class XunitException : Exception
    {
        public XunitException(string message)
            : base(message)
        {
        }

        public XunitException()
            : base()
        {
        }
    }
}
