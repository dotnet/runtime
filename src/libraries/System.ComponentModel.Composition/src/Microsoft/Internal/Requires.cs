// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
            Contract.EndContractBlock();
        }

        [DebuggerStepThrough]
        public static void NullOrNotNullElements<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values, string parameterName)
            where TKey : class
            where TValue : class
        {
            NotNullElements(values, parameterName);
            Contract.EndContractBlock();
        }

        [DebuggerStepThrough]
        public static void NullOrNotNullElements<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            NotNullElements(values, parameterName);
            Contract.EndContractBlock();
        }

        [DebuggerStepThrough]
        private static void NotNullElements<T>(IEnumerable<T> values, string parameterName)
            where T : class
        {
            if (values != null && !Contract.ForAll(values, (value) => value != null))
            {
                throw ExceptionBuilder.CreateContainsNullElement(parameterName);
            }
            Contract.EndContractBlock();
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
            if (values != null && !Contract.ForAll(values, (keyValue) => keyValue.Key != null && keyValue.Value != null))
            {
                throw ExceptionBuilder.CreateContainsNullElement(parameterName);
            }
            Contract.EndContractBlock();
        }

        [DebuggerStepThrough]
        public static void IsInMembertypeSet(MemberTypes value, string parameterName, MemberTypes enumFlagSet)
        {
            if ((value & enumFlagSet) != value || // Ensure the member is in the set
                (value & (value - 1)) != 0) // Ensure that there is only one flag in the value (i.e. value is a power of 2).
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.ArgumentOutOfRange_InvalidEnumInSet, parameterName, value, enumFlagSet.ToString()), parameterName);
            }
            Contract.EndContractBlock();
        }        
    }
}
