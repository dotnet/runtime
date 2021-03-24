// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    [TypeDependency("System.Collections.Generic.ObjectComparer`1")]
    public abstract partial class Comparer<T> : IComparer, IComparer<T>
    {
        // To minimize generic instantiation overhead of creating the comparer per type, we keep the generic portion of the code as small
        // as possible and define most of the creation logic in a non-generic class.
        public static Comparer<T> Default { [Intrinsic] get; } = (Comparer<T>)ComparerHelpers.CreateDefaultComparer(typeof(T));
    }

    internal sealed partial class EnumComparer<T> : Comparer<T> where T : struct, Enum
    {
        public override int Compare(T x, T y)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.EnumCompareTo(x, y);
        }
    }
}
