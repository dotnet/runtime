// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static void RequiresNotEmpty<T>(ICollection<T> collection, string paramName)
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

            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] == null)
                {
                    throw new ArgumentNullException(string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}[{1}]", arrayName, i));
                }
            }
        }
    }
}
