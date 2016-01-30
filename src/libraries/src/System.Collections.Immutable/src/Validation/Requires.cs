// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Common runtime checks that throw <see cref="ArgumentException"/> upon failure.
    /// </summary>
    internal static class Requires
    {
        /// <summary>
        /// Throws an exception if the specified parameter's value is null.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="value">The value of the argument.</param>
        /// <param name="parameterName">The name of the parameter to include in any thrown exception.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c></exception>
        [DebuggerStepThrough]
        public static void NotNull<T>([ValidatedNotNull]T value, string parameterName)
            where T : class // ensures value-types aren't passed to a null checking method
        {
            if (value == null)
            {
                FailArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throws an exception if the specified parameter's value is null.  It passes through the specified value back as a return value.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="value">The value of the argument.</param>
        /// <param name="parameterName">The name of the parameter to include in any thrown exception.</param>
        /// <returns>The value of the parameter.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c></exception>
        [DebuggerStepThrough]
        public static T NotNullPassthrough<T>([ValidatedNotNull]T value, string parameterName)
            where T : class // ensures value-types aren't passed to a null checking method
        {
            NotNull(value, parameterName);
            return value;
        }

        /// <summary>
        /// Throws an exception if the specified parameter's value is null.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="value">The value of the argument.</param>
        /// <param name="parameterName">The name of the parameter to include in any thrown exception.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c></exception>
        /// <remarks>
        /// This method exists for callers who themselves only know the type as a generic parameter which
        /// may or may not be a class, but certainly cannot be null.
        /// </remarks>
        [DebuggerStepThrough]
        public static void NotNullAllowStructs<T>([ValidatedNotNull]T value, string parameterName)
        {
            if (null == value)
            {
                FailArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="parameterName">The name of the parameter that was null.</param>
        [DebuggerStepThrough]
        private static void FailArgumentNullException(string parameterName)
        {
            // Separating out this throwing operation helps with inlining of the caller
            throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Range(bool condition, string parameterName, string message = null)
        {
            if (!condition)
            {
                FailRange(parameterName, message);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static void FailRange(string parameterName, string message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
            else
            {
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if a condition does not evaluate to true.
        /// </summary>
        [DebuggerStepThrough]
        public static void Argument(bool condition, string parameterName, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if a condition does not evaluate to true.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        [DebuggerStepThrough]
        public static void Argument(bool condition)
        {
            if (!condition)
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> for a disposed object.
        /// </summary>
        /// <typeparam name="TDisposed">Specifies the type of the disposed object.</typeparam>
        /// <param name="disposed">The disposed object.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.NoInlining)] // inlining this on .NET < 4.5.2 on x64 causes InvalidProgramException. 
        public static void FailObjectDisposed<TDisposed>(TDisposed disposed)
        {
            // separating out this throwing helps with inlining of the caller, especially
            // due to the retrieval of the type's name
            throw new ObjectDisposedException(disposed.GetType().FullName);
        }
    }
}
