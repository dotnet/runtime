// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static partial class Requires
    {
        [DebuggerStepThrough]
        public static void NotNullOrNullElements<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            NotNull(values, parameterName);
            NotNullElements(values, parameterName);
        }

        [DebuggerStepThrough]
        public static void NullOrNotNullElements<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values, string parameterName)
            where TKey : class
            where TValue : class
        {
            NotNullElements(values, parameterName);
        }

        [DebuggerStepThrough]
        public static void NullOrNotNullElements<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            NotNullElements(values, parameterName);
        }

        [DebuggerStepThrough]
        private static void NotNullElements<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            if (values != null)
            {
                foreach (T value in values)
                {
                    if (value is null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement(parameterName);
                    }
                }
            }
        }

        [DebuggerStepThrough]
        public static void NullOrNotNullElements<T>(T[] values, string parameterName)
            where T : class
        {
            NotNullElements(values, parameterName);
        }

        [DebuggerStepThrough]
        private static void NotNullElements<T>(T[] values, string parameterName)
            where T : class
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    if (value == null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement(parameterName);
                    }
                }
            }
        }

        [DebuggerStepThrough]
        private static void NotNullElements<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values, string parameterName)
            where TKey : class
            where TValue : class
        {
            if (values != null)
            {
                foreach (KeyValuePair<TKey, TValue> keyValue in values)
                {
                    if (keyValue.Key is null || keyValue.Value is null)
                    {
                        throw ExceptionBuilder.CreateContainsNullElement(parameterName);
                    }
                }
            }
        }

        [DebuggerStepThrough]
        public static void IsInMembertypeSet(MemberTypes value, string parameterName, MemberTypes enumFlagSet)
        {
            if ((value & enumFlagSet) != value || // Ensure the member is in the set
                (value & (value - 1)) != 0) // Ensure that there is only one flag in the value (i.e. value is a power of 2).
            {
                throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_InvalidEnumInSet, parameterName, value, enumFlagSet.ToString()), parameterName);
            }
        }

        public static void NotNull<T>(T value, string parameterName)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void NotNullOrEmpty(string value, string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, parameterName), parameterName);
            }
        }

    }
}
