// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana
{
    public static class ThrowHelpers
    {
        public static class ArgumentNullException
        {
            public static void ThrowIfNull(object? value, string name)
            {
                if (value == null) throw new System.ArgumentNullException(name);
            }
        }

        public static class ArgumentOutOfRangeException
        {
            public static void ThrowIfNegative(long value, string name)
            {
                if (value < 0) throw new System.ArgumentOutOfRangeException(name);
            }

            public static void ThrowIfGreaterThan(long value, long limit, string name)
            {
                if (value > limit) throw new System.ArgumentOutOfRangeException(name);
            }
        }

        public static class ObjectDisposedException
        {
            public static void ThrowIf(bool condition, string typeName)
            {
                if (condition) throw new System.ObjectDisposedException(typeName);
            }
        }
    }
}
