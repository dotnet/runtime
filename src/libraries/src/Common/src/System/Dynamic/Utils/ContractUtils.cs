// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Dynamic.Utils
{
    internal static partial class ContractUtils
    {
        public static Exception Unreachable
        {
            get
            {
                Debug.Assert(false, "Unreachable");
                return new InvalidOperationException("Code supposed to be unreachable");
            }
        }

        public static void Requires(bool precondition, string paramName)
        {
            Debug.Assert(!string.IsNullOrEmpty(paramName));

            if (!precondition)
            {
                throw new ArgumentException(Strings.InvalidArgumentValue, paramName);
            }
        }

        public static void RequiresNotNull(object value, string paramName)
        {
            Debug.Assert(!string.IsNullOrEmpty(paramName));

            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void RequiresNotNull(object value, string paramName, int index)
        {
            Debug.Assert(!string.IsNullOrEmpty(paramName));

            if (value == null)
            {
                throw new ArgumentNullException(GetParamName(paramName, index));
            }
        }

        public static void RequiresNotEmpty<T>(ICollection<T> collection, string paramName)
        {
            RequiresNotNull(collection, paramName);
            if (collection.Count == 0)
            {
                throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
            }
        }

        public static void RequiresNotEmptyList<T>(IReadOnlyList<T> collection, string paramName)
        {
            RequiresNotNull(collection, paramName);
            if (collection.Count == 0)
            {
                throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
            }
        }

        /// <summary>
        /// Requires the array and all its items to be non-null.
        /// </summary>
        public static void RequiresNotNullItems<T>(IList<T> array, string arrayName)
        {
            Debug.Assert(arrayName != null);
            RequiresNotNull(array, arrayName);

            for (int i = 0, n = array.Count; i < n; i++)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException(GetParamName(arrayName, i));
                }
            }
        }

        private static string GetParamName(string paramName, int index)
        {
            if (index >= 0)
            {
                return $"{paramName}[{index}]";
            }

            return paramName;
        }
    }
}
