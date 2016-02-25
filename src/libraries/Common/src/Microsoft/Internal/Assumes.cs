// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Internal
{
    internal static partial class Assumes
    {
        [DebuggerStepThrough]
        internal static void NotNull<T>(T value)
            where T : class
        {
            IsTrue(value != null);
        }

        [DebuggerStepThrough]
        internal static void NotNull<T1, T2>(T1 value1, T2 value2)
            where T1 : class
            where T2 : class
        {
            NotNull(value1);
            NotNull(value2);
        }

        [DebuggerStepThrough]
        internal static void NotNull<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            NotNull(value1);
            NotNull(value2);
            NotNull(value3);
        }

        [DebuggerStepThrough]
        internal static void NotNullOrEmpty(string value)
        {
            NotNull(value);
            IsTrue(value.Length > 0);
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition)
        {
            if (!condition)
            {
                throw UncatchableException(null);
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, /*[Localizable(false)]*/string message)
        {
            if (!condition)
            {
                throw UncatchableException(message);
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, /*[Localizable(false)]*/string message, object arg0)
        {
            if (!condition)
            {
                throw UncatchableException(String.Format(message, arg0));
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, /*[Localizable(false)]*/string message, object arg0, object arg1)
        {
            if (!condition)
            {
                throw UncatchableException(String.Format(message, arg0, arg1));
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, /*[Localizable(false)]*/string message, object arg0, object arg1, object arg2)
        {
            if (!condition)
            {
                throw UncatchableException(String.Format(message, arg0, arg1, arg2));
            }
        }

        [DebuggerStepThrough]
        internal static void IsTrue(bool condition, /*[Localizable(false)]*/string message, params object[] args)
        {
            if (!condition)
            {
                throw UncatchableException(String.Format(message, args));
            }
        }

        [DebuggerStepThrough]
        internal static T NotReachable<T>()
        {
            throw UncatchableException("Code path should never be reached!");
        }

        [DebuggerStepThrough]
        private static Exception UncatchableException(/*[Localizable(false)]*/string message)
        {
            return new InternalErrorException(message);
        }
    }
}
