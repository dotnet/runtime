// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: Exception messages call ToString instead of Name to avoid MissingMetadataException when just outputting basic info

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xunit
{
    public static class AssertExtensions
    {
        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="ArgumentException"/> with the given parameter name.
        /// </summary>
        /// <param name="action">
        ///     The delegate of type <see cref="Action"/> to execute.
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
        public static ArgumentException ThrowsArgumentException(string parameterName, Action action)
        {
            return ThrowsArgumentException<ArgumentException>(parameterName, action);
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="ArgumentException"/> of type <typeparamref name="T"/> with the given parameter name.
        /// </summary>
        /// <param name="parameterName">
        ///     A <see cref="String"/> containing the parameter of name to check, <see langword="null"/> to skip parameter validation.
        /// </param>
        /// <param name="action">
        ///     The delegate of type <see cref="Action"/> to execute.
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
        public static T ThrowsArgumentException<T>(string parameterName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

#if DEBUG
            // ParamName's not available on ret builds
            if (parameterName != null)
                Assert.Equal(parameterName, exception.ParamName);
#endif

            return exception;
        }

        /// <summary>
        ///     Asserts that the given delegate throws an <see cref="MissingMemberException"/> of type <typeparamref name="T"/> with the given parameter name.
        /// </summary>
        /// <param name="memberName">
        ///     A <see cref="String"/> containing the parameter of name to check, <see langword="null"/> to skip parameter validation.
        /// </param>
        /// <param name="action">
        ///     The delegate of type <see cref="Action"/> to execute.
        /// </param>
        /// <returns>
        ///     The thrown <see cref="Exception"/>.
        /// </returns>
        /// <exception cref="AssertFailedException">
        ///     <see cref="Exception"/> of type <typeparam name="T"/> was not thrown.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="MissingMemberException.Message"/> does not contain <paramref name="memberName"/> .
        /// </exception>
        public static T ThrowsMissingMemberException<T>(string memberName, Action action)
            where T : MissingMemberException
        {
            T exception = Assert.Throws<T>(action);

            if (memberName != null)
                Assert.True(exception.Message.Contains(memberName));

            return exception;
        }

        /// <summary>
        ///     Asserts that the given async delegate throws an <see cref="Exception"/> of type <typeparam name="T" /> and <see cref="Exception.InnerException"/>
        ///     returns an <see cref="Exception"/> of type <typeparam name="TInner" />.
        /// </summary>
        /// <param name="action">
        ///     The delegate of type <see cref="Action"/> to execute.
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
        public static TInner ThrowsWithInnerException<T, TInner>(Action action)
            where T : Exception
            where TInner : Exception
        {
            T outerException = Assert.Throws<T>(action);

            if (outerException.InnerException == null)
                Assert.Fail(string.Format("Expected '{0}.InnerException' to be '{1}', however it is null.", typeof(T), typeof(TInner)));

            if (outerException.InnerException is not TInner)
                Assert.Fail(string.Format("Expected '{0}.InnerException', to be '{1}', however, '{2}' is.", typeof(T), typeof(TInner), outerException.InnerException.GetType()));

            return (TInner)outerException.InnerException;
        }


        /// <summary>
        /// Tests whether the two lists are the same length and contain the same objects (using Object.Equals()) in the same order and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected list.</param>
        /// <param name="actual">Actual list.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void CollectionEqual<T>(T[] expected, T[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
                Assert.Equal<T>(expected[i], actual[i]);
        }

        /// <summary>
        /// Tests whether the two enumerables are the same length and contain the same objects (using Object.Equals()) in the same order and
        /// throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected enumerables.</param>
        /// <param name="actual">Actual enumerables.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void CollectionEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            CollectionEqual(CopyToArray(expected), CopyToArray(actual));
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
    }
}
