// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents an equality comparer that serves as a proxy for another comparer.
    /// That is, something that should be unwrapped before being presented to the user.
    /// </summary>
    internal interface IEqualityComparerProxy<T> : IEqualityComparer<T>
    {
        IEqualityComparer<T> GetUnderlyingEqualityComparer();
    }

    internal static class EqualityComparerProxy
    {
        /// <summary>
        /// Unwraps the incoming equality comparer, if proxied.
        /// Otherwise returns the equality comparer itself or its default equivalent.
        /// </summary>
        internal static IEqualityComparer<T> GetUnderlyingEqualityComparer<T>(IEqualityComparer<T>? outerComparer)
        {
            if (outerComparer is null)
            {
                return EqualityComparer<T>.Default;
            }
            else if (outerComparer is IEqualityComparerProxy<T> proxy)
            {
                return proxy.GetUnderlyingEqualityComparer();
            }
            else
            {
                return outerComparer;
            }
        }
    }
}
