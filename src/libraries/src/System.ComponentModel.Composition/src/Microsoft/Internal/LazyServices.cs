// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Internal
{
    internal static class LazyServices
    {
        public static T GetNotNullValue<T>(this Lazy<T> lazy, string argument)
            where T : class
        {
            Assumes.NotNull(lazy);
            T value = lazy.Value;
            if (value == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, SR.LazyServices_LazyResolvesToNull, typeof(T), argument));
            }

            return value;
        }
    }
}
