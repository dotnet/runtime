// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.FileSystemGlobbing.Util
{
    internal static class StringComparisonHelper
    {
        public static StringComparer GetStringComparer(StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return StringComparer.CurrentCulture;
                case StringComparison.CurrentCultureIgnoreCase:
                    return StringComparer.CurrentCultureIgnoreCase;
                case StringComparison.Ordinal:
                    return StringComparer.Ordinal;
                case StringComparison.OrdinalIgnoreCase:
                    return StringComparer.OrdinalIgnoreCase;
                case StringComparison.InvariantCulture:
                    return StringComparer.InvariantCulture;
                case StringComparison.InvariantCultureIgnoreCase:
                    return StringComparer.InvariantCultureIgnoreCase;
                default:
                    throw new InvalidOperationException($"Unexpected StringComparison type: {comparisonType}");
            }
        }
    }
}
