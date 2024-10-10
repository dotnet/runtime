// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel
{
    internal static class ThrowHelpers
    {
        internal static class ArgumentNullException
        {
            internal static void ThrowIfNull(object value, string name)
            {
                if (value == null) throw new System.ArgumentNullException(name);
            }
        }

        internal static class ArgumentOutOfRangeException
        {
            internal static void ThrowIfNegative(long value, string name)
            {
                if (value < 0) throw new System.ArgumentOutOfRangeException(name);
            }

            internal static void ThrowIfGreaterThan(long value, long limit, string name)
            {
                if (value > limit) throw new System.ArgumentOutOfRangeException(name);
            }
        }

        internal static class ObjectDisposedException
        {
            internal static void ThrowIf(bool condition, string typeName)
            {
                if (condition) throw new System.ObjectDisposedException(typeName);
            }
        }
    }
}
