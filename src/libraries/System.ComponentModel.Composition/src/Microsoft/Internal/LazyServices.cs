// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Internal
{
    internal static class LazyServices
    {
        public static T GetNotNullValue<T>(this Lazy<T> lazy, string argument)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(lazy);

            T value = lazy.Value;
            if (value == null)
            {
                throw new InvalidOperationException(SR.Format(SR.LazyServices_LazyResolvesToNull, typeof(T), argument));
            }

            return value;
        }
    }
}
