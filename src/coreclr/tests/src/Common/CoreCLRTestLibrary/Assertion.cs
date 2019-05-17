// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Note: Exception messages call ToString instead of Name to avoid MissingMetadataException when just outputting basic info

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestLibrary
{
    /// <summary>
    ///    A collection of helper classes to test various conditions within
    /// unit tests. If the condition being tested is not met, an exception
    /// is thrown.
    /// </summary>
    public static class Assert
    {
        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="ArgumentNullException"/> with the given parameter name.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="parameterName">
        ///     A <see cref="String"/> containing the parameter of name to check, <see langword="null"/> to skip parameter validation.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="ArgumentNullException"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <see cref="ArgumentNullException"/> was not thrown.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="ArgumentException.ParamName"/> is not equal to <paramref name="parameterName"/> .
        /// </exception>
        public static ArgumentNullException ThrowsArgumentNullException(string parameterName, Action action, string message = null)
        {
            return ThrowsArgumentException<ArgumentNullException>(parameterName, action, message);
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="ArgumentException"/> with the given parameter name.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="parameterName">
        ///     A <see cref="String"/> containing the parameter of name to check, <see langword="null"/> to skip parameter validation.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="ArgumentException"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <see cref="ArgumentException"/> was not thrown.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="ArgumentException.ParamName"/> is not equal to <paramref name="parameterName"/> .
        /// </exception>
        public static ArgumentException ThrowsArgumentException(string parameterName, Action action, string message = null)
        {
            return ThrowsArgumentException<ArgumentException>(parameterName, action, message);
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="ArgumentException"/> of type <typeparamref name="T"/> with the given parameter name.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="parameterName">
        ///     A <see cref="String"/> containing the parameter of name to check, <see langword="null"/> to skip parameter validation.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="ArgumentException.ParamName"/> is not equal to <paramref name="parameterName"/> .
        /// </exception>
        public static T ThrowsArgumentException<T>(string parameterName, Action action, string message = null)
            where T : ArgumentException
        {
            T exception = Throws<T>(action, message);

#if DEBUG
            // ParamName's not available on ret builds
            if (parameterName != null)
                Assert.AreEqual(parameterName, exception.ParamName, "Expected '{0}.ParamName' to be '{1}'. {2}", typeof(T), parameterName, message);
#endif

            return exception;
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="AggregateException"/> with a base exception <see cref="Exception"/> of type <typeparam name="T" />.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <returns>
        ///     The base <see cref="Exception"/> of the <see cref="AggregateException"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="AggregateException"/> of was not thrown.
        ///         -or-
        ///     </para>
        ///     <see cref="AggregateException.GetBaseException()"/> is not of type <typeparam name="TBase"/>.
        /// </exception>
        public static TBase ThrowsAggregateException<TBase>(Action action, string message = "") where TBase : Exception
        {
            AggregateException exception = Throws<AggregateException>(action, message);

            Exception baseException = exception.GetBaseException();
            if (baseException == null)
                Assert.Fail("Expected 'AggregateException.GetBaseException()' to be '{0}', however it is null. {1}", typeof(TBase), message);

            if (baseException.GetType() != typeof(TBase))
                Assert.Fail("Expected 'AggregateException.GetBaseException()', to be '{0}', however, '{1}' is. {2}", typeof(TBase), baseException.GetType(), message);

            return (TBase)baseException;
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="Exception"/> of type <typeparam name="T" />.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="format">
        ///     A <see cref="String"/> containing format information for when the assertion fails.
        /// </param>
        /// <param name="args">
        ///     An <see cref="Array"/> of arguments to be formatted.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        /// </exception>
        public static T Throws<T>(Action action, string format, params Object[] args) where T : Exception
        {
            return Throws<T>(action, String.Format(format, args));
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="Exception"/> of type <typeparam name="T" />.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="options">
        ///     Specifies whether <see cref="Assert.Throws{T}"/> should require an exact type match when comparing the expected exception type with the thrown exception. The default is <see cref="AssertThrowsOptions.None"/>.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        /// </exception>
        public static T Throws<T>(Action action, string message = "", AssertThrowsOptions options = AssertThrowsOptions.None) where T : Exception
        {
            Exception exception = RunWithCatch(action);

            if (exception == null)
                Assert.Fail("Expected '{0}' to be thrown. {1}", typeof(T).ToString(), message);

            if (!IsOfExceptionType<T>(exception, options))
                Assert.Fail("Expected '{0}' to be thrown, however '{1}' was thrown. {2}", typeof(T), exception.GetType(), message);

            return (T)exception;
        }

        /// <summary>
        ///     Asserts that the given async delegate throws an <see cref="Exception"/> of type <typeparam name="T".
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Func{}"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="options">
        ///     Specifies whether <see cref="Assert.Throws{T}"/> should require an exact type match when comparing the expected exception type with the thrown exception. The default is <see cref="AssertThrowsOptions.None"/>.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        /// </exception>
        public static async Task<T> ThrowsAsync<T>(Func<Task> action, string message = "", AssertThrowsOptions options = AssertThrowsOptions.None) where T : Exception
        {
            Exception exception = await RunWithCatchAsync(action);

            if (exception == null)
                Assert.Fail("Expected '{0}' to be thrown. {1}", typeof(T).ToString(), message);

            if (!IsOfExceptionType<T>(exception, options))
                Assert.Fail("Expected '{0}' to be thrown, however '{1}' was thrown. {2}", typeof(T), exception.GetType(), message);

            return (T)exception;
        }

        /// <summary>
        ///     Asserts that the given async delegate throws an <see cref="Exception"/> of type <typeparam name="T" /> and <see cref="Exception.InnerException"/> 
        ///     returns an <see cref="Exception"/> of type <typeparam name="TInner" />.
        /// </summary>
        /// <param name="action">
        ///     The delagate of type <see cref="Action"/> to execute.
        /// </param>
        /// <param name="message">
        ///     A <see cref="String"/> containing additional information for when the assertion fails.
        /// </param>
        /// <param name="options">
        ///     Specifies whether <see cref="Assert.Throws{T}"/> should require an exact type match when comparing the expected exception type with the thrown exception. The default is <see cref="AssertThrowsOptions.None"/>.
        /// </param>
        /// <returns>
        ///     The thrown inner <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="Exception.InnerException"/> is not of type <typeparam name="TInner"/>.
        /// </exception>
        public static TInner Throws<T, TInner>(Action action, string message = "", AssertThrowsOptions options = AssertThrowsOptions.None)
            where T : Exception
            where TInner : Exception
        {
            T outerException = Throws<T>(action, message, options);

            if (outerException.InnerException == null)
                Assert.Fail("Expected '{0}.InnerException' to be '{1}', however it is null. {2}", typeof(T), typeof(TInner), message);

            if (!IsOfExceptionType<TInner>(outerException.InnerException, options))
                Assert.Fail("Expected '{0}.InnerException', to be '{1}', however, '{2}' is. {3}", typeof(T), typeof(TInner), outerException.InnerException.GetType(), message);

            return (TInner)outerException.InnerException;
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
        public static void IsTrue(bool condition, string format, params Object[] args)
        {
            if (!condition)
            {
                Assert.HandleFail("Assert.IsTrue", String.Format(format, args));
            }
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
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                Assert.HandleFail("Assert.IsTrue", message);
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
        public static void IsFalse(bool condition, string message = "")
        {
            if (condition)
            {
                Assert.HandleFail("Assert.IsFalse", message);
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
        public static void IsFalse(bool condition, string format, params Object[] args)
        {
            IsFalse(condition, String.Format(format, args));
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
        public static void IsNull(object value, string message = "")
        {
            if (value != null)
            {
                Assert.HandleFail("Assert.IsNull", message);
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
        public static void IsNull(object value, string format, params Object[] args)
        {
            IsNull(value, String.Format(format, args));
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
        public static void IsNotNull(object value, string message = "")
        {
            if (value == null)
            {
                Assert.HandleFail("Assert.IsNotNull", message);
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and 
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            const string EXPECTED_MSG = @"Expected: [{1}]. Actual: [{2}]. {0}";

            if (!Object.Equals(expected, actual))
            {
                string finalMessage = String.Format(EXPECTED_MSG, message, (object)expected ?? "NULL", (object)actual ?? "NULL");
                Assert.HandleFail("Assert.AreEqual", finalMessage);
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and 
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreEqual<T>(T expected, T actual, string format, params Object[] args)
        {
            AreEqual<T>(expected, actual, String.Format(format, args));
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and 
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object that we do not want it to be.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreNotEqual<T>(T notExpected, T actual, string message = "")
        {
            if (Object.Equals(notExpected, actual))
            {
                String finalMessage =
                    String.Format(@"Expected any value except:[{1}]. Actual:[{2}]. {0}",
                    message, notExpected, actual);

                Assert.HandleFail("Assert.AreNotEqual", finalMessage);
            }
        }

        /// <summary>
        /// Tests whether the expected object is equal to the actual object  and 
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="notExpected">Expected object that we do not want it to be.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreNotEqual<T>(T notExpected, T actual, string format, params Object[] args)
        {
            AreNotEqual<T>(notExpected, actual, String.Format(format, args));
        }

        /// <summary>
        /// Tests whether the two lists are the same length and contain the same objects (using Object.Equals()) in the same order and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected list.</param>
        /// <param name="actual">Actual list.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreAllEqual<T>(T[] expected, T[] actual, string message = "")
        {
            Assert.AreEqual(expected.Length, actual.Length, message);

            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual<T>(expected[i], actual[i], message);
        }

        /// <summary>
        /// Tests whether the two lists are the same length and contain the same objects (using Object.Equals()) in the same order and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected list.</param>
        /// <param name="actual">Actual list.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreAllEqual<T>(T[] expected, T[] actual, string format, params Object[] args)
        {
            AreAllEqual<T>(expected, actual, String.Format(format, args));
        }

        /// <summary>
        /// Tests whether the two lists are the same length and contain the same objects (using Object.Equals()) (but not necessarily in the same order) and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected list.</param>
        /// <param name="actual">Actual list.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreAllEqualUnordered<T>(T[] expected, T[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            int count = expected.Length;
            bool[] removedFromActual = new bool[count];
            for (int i = 0; i < count; i++)
            {
                T item1 = expected[i];
                bool foundMatch = false;
                for (int j = 0; j < count; j++)
                {
                    if (!removedFromActual[j])
                    {
                        T item2 = actual[j];
                        if ((item1 == null && item2 == null) || (item1 != null && item1.Equals(item2)))
                        {
                            foundMatch = true;
                            removedFromActual[j] = true;
                            break;
                        }
                    }
                }
                if (!foundMatch)
                    Assert.HandleFail("Assert.AreAllEqualUnordered", "First array has element not found in second array: " + item1);
            }
            return;
        }

        /// <summary>
        /// Tests whether the two enumerables are the same length and contain the same objects (using Object.Equals()) in the same order and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected enumerables.</param>
        /// <param name="actual">Actual enumerables.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreAllEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = "")
        {
            AreAllEqual(CopyToArray(expected), CopyToArray(actual), message);
        }

        /// <summary>
        /// Tests whether the two enumerables are the same length and contain the same objects (using Object.Equals()) (but not necessarily 
        /// in the same order) and throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected enumerable.</param>
        /// <param name="actual">Actual enumerable.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void AreAllEqualUnordered<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = "")
        {
            AreAllEqualUnordered(CopyToArray(expected), CopyToArray(actual), message);
        }

        /// <summary>
        /// Iterates through an IEnumerable to generate an array of elements. The rational for using this instead of 
        /// System.Linq.ToArray is that this will not require a dependency on System.Linq.dll
        /// </summary>
        private static T[] CopyToArray<T>(IEnumerable<T> source)
        {
            T[] items = new T[4];
            int count = 0;

            if (source == null)
                return null;

            foreach (var item in source)
            {
                if (items.Length == count)
                {
                    var newItems = new T[checked(count * 2)];
                    Array.Copy(items, 0, newItems, 0, count);
                    items = newItems;
                }

                items[count] = item;
                count++;
            }

            if (items.Length == count)
                return items;

            var finalItems = new T[count];
            Array.Copy(items, 0, finalItems, 0, count);
            return finalItems;
        }


        /// <summary>
        /// Tests whether the specified objects both refer to the same object and
        /// throws an exception if the two inputs do not refer to the same object.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the value the test expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> does not refer to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreSame(object expected, object actual)
        {
            Assert.AreSame(expected, actual, string.Empty);
        }

        /// <summary>
        /// Tests whether the specified objects both refer to the same object and
        /// throws an exception if the two inputs do not refer to the same object.
        /// </summary>
        /// <param name="expected">
        /// The first object to compare. This is the value the test expects.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is not the same as <paramref name="expected"/>. The message is shown
        /// in test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="expected"/> does not refer to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreSame(object expected, object actual, string message)
        {
            if (!Object.ReferenceEquals(expected, actual))
            {
                string finalMessage = message;

                ValueType valExpected = expected as ValueType;
                if (valExpected != null)
                {
                    ValueType valActual = actual as ValueType;
                    if (valActual != null)
                    {
                        finalMessage = message == null ? String.Empty : message;
                    }
                }

                Assert.HandleFail("Assert.AreSame", finalMessage);
            }
        }

        /// <summary>
        /// Tests whether the specified objects refer to different objects and
        /// throws an exception if the two inputs refer to the same object.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> refers to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreNotSame(object notExpected, object actual)
        {
            Assert.AreNotSame(notExpected, actual, string.Empty);
        }

        /// <summary>
        /// Tests whether the specified objects refer to different objects and
        /// throws an exception if the two inputs refer to the same object.
        /// </summary>
        /// <param name="notExpected">
        /// The first object to compare. This is the value the test expects not
        /// to match <paramref name="actual"/>.
        /// </param>
        /// <param name="actual">
        /// The second object to compare. This is the value produced by the code under test.
        /// </param>
        /// <param name="message">
        /// The message to include in the exception when <paramref name="actual"/>
        /// is the same as <paramref name="notExpected"/>. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Thrown if <paramref name="notExpected"/> refers to the same object
        /// as <paramref name="actual"/>.
        /// </exception>
        static public void AreNotSame(object notExpected, object actual, string message)
        {
            if (Object.ReferenceEquals(notExpected, actual))
            {
                Assert.HandleFail("Assert.AreNotSame", message);
            }
        }

        static public void OfType<T>(object obj)
        {
            if (!(obj is T))
            {
                Assert.HandleFail(
                    "Assert.IsOfType",
                    $"Expected an object of type [{typeof(T).AssemblyQualifiedName}], got type of type [{obj.GetType().AssemblyQualifiedName}].");
            }
        }

        /// <summary>
        /// Throws an AssertFailedException.
        /// </summary>
        /// <exception cref="AssertFailedException">
        /// Always thrown.
        /// </exception>
        public static void Fail()
        {
            Assert.HandleFail("Assert.Fail", "");
        }

        /// <summary>
        /// Throws an AssertFailedException.
        /// </summary>
        /// <param name="message">
        /// The message to include in the exception. The message is shown in
        /// test results.
        /// </param>
        /// <exception cref="AssertFailedException">
        /// Always thrown.
        /// </exception>
        public static void Fail(string message, params object[] args)
        {
            string exceptionMessage = args.Length == 0 ? message : string.Format(message, args);
            Assert.HandleFail("Assert.Fail", exceptionMessage);
        }

        /// <summary>
        /// Helper function that creates and throws an exception.
        /// </summary>
        /// <param name="assertionName">name of the assertion throwing an exception.</param>
        /// <param name="message">message describing conditions for assertion failure.</param>
        /// <param name="parameters">The parameters.</param>
        /// TODO: Modify HandleFail to take in parameters
        internal static void HandleFail(string assertionName, string message)
        {
            // change this to use AssertFailedException             
            throw new AssertTestException(assertionName + ": " + message);
        }


        [Obsolete("Did you mean to call Assert.AreEqual()")]
        public static new bool Equals(Object o1, Object o2)
        {
            Assert.Fail("Don\u2019t call this.");
            throw new Exception();
        }

        private static bool IsOfExceptionType<T>(Exception thrown, AssertThrowsOptions options)
        {
            if ((options & AssertThrowsOptions.AllowDerived) == AssertThrowsOptions.AllowDerived)
                return thrown is T;

            return thrown.GetType() == typeof(T);
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

        private static async Task<Exception> RunWithCatchAsync(Func<Task> action)
        {
            try
            {
                await action();
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
    public class AssertTestException : Exception
    {
        public AssertTestException(string message)
            : base(message)
        {
        }

        public AssertTestException()
            : base()
        {
        }
    }

    public static class ExceptionAssert
    {
        public static void Throws<T>(String message, Action a) where T : Exception
        {
            Assert.Throws<T>(a, message);
        }
    }

    /// <summary>
    ///     Specifies whether <see cref="Assert.Throws{T}"/> should require an exact type match when comparing the expected exception type with the thrown exception.
    /// </summary>
    [Flags]
    public enum AssertThrowsOptions
    {
        /// <summary>
        ///     Specifies that <see cref="Assert.Throws{T}"/> should require an exact type 
        ///     match when comparing the specified exception type with the throw exception.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Specifies that <see cref="Assert.Throws{T}"/> should not require an exact type 
        ///     match when comparing the specified exception type with the thrown exception.
        /// </summary>
        AllowDerived = 1,
    }
}
