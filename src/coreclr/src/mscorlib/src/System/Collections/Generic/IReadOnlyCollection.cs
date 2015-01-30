// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface:  IReadOnlyCollection<T>
** 
** 
**
** Purpose: Base interface for read-only generic lists.
** 
===========================================================*/
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{

    // Provides a read-only, covariant view of a generic list.

    // Note that T[] : IReadOnlyList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special workaround internally though - see VM\compile.cpp.
    // The same attribute is on IList<T>, IEnumerable<T>, ICollection<T>, and IReadOnlyList<T>.
    [TypeDependencyAttribute("System.SZArrayHelper")]
#if CONTRACTS_FULL
    [ContractClass(typeof(IReadOnlyCollectionContract<>))]
#endif
    // If we ever implement more interfaces on IReadOnlyCollection, we should also update RuntimeTypeCache.PopulateInterfaces() in rttype.cs
    public interface IReadOnlyCollection<out T> : IEnumerable<T>
    {
        int Count { get; }
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IReadOnlyCollection<>))]
    internal abstract class IReadOnlyCollectionContract<T> : IReadOnlyCollection<T>
    {
        int IReadOnlyCollection<T>.Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return default(int);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return default(IEnumerator<T>);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }
    }
#endif
}
