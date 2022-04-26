// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    public static class GlobalizationExtensions
    {
        public static StringComparer GetStringComparer(this CompareInfo compareInfo, CompareOptions options)
        {
            ArgumentNullException.ThrowIfNull(compareInfo);

            if (options == CompareOptions.Ordinal)
            {
                return StringComparer.Ordinal;
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return StringComparer.OrdinalIgnoreCase;
            }

            return new CultureAwareComparer(compareInfo, options);
        }
    }
}
