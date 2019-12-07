using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal class ReferenceEqualsEqualityComparer<T> : IEqualityComparer<T>
    {
        public static ReferenceEqualsEqualityComparer<T> Comparer => new ReferenceEqualsEqualityComparer<T>();

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return GetHashCode();
        }
    }
}
