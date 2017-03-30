// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Dynamic;

namespace System.Linq.Expressions.Compiler
{
    internal static partial class DelegateHelpers
    {
        /// <summary>
        /// Finds a delegate type using the types in the array.
        /// We use the cache to avoid copying the array, and to cache the
        /// created delegate type
        /// </summary>
        internal static Type MakeDelegateType(Type[] types)
        {
            lock (_DelegateCache)
            {
                TypeInfo curTypeInfo = _DelegateCache;

                // arguments & return type
                for (int i = 0; i < types.Length; i++)
                {
                    curTypeInfo = NextTypeInfo(types[i], curTypeInfo);
                }

                // see if we have the delegate already
                if (curTypeInfo.DelegateType == null)
                {
                    // clone because MakeCustomDelegate can hold onto the array.
                    curTypeInfo.DelegateType = MakeNewDelegate((Type[])types.Clone());
                }

                return curTypeInfo.DelegateType;
            }
        }

        internal static TypeInfo NextTypeInfo(Type initialArg)
        {
            lock (_DelegateCache)
            {
                return NextTypeInfo(initialArg, _DelegateCache);
            }
        }

        internal static TypeInfo GetNextTypeInfo(Type initialArg, TypeInfo curTypeInfo)
        {
            lock (_DelegateCache)
            {
                return NextTypeInfo(initialArg, curTypeInfo);
            }
        }
    }
}
