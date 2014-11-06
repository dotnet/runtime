// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Collections.Immutable
{
    [DebuggerDisplay("Count = {stack != null ? stack.Count : 0}")]
    internal static class AllocFreeConcurrentStack<T>
    {
        private const int MaxSize = 35;

        [ThreadStatic]
        private static Stack<RefAsValueType<T>> stack;

        public static void TryAdd(T item)
        {
            if (stack == null)
            {
                stack = new Stack<RefAsValueType<T>>(MaxSize);
            }

            // Just in case we're in a scenario where an object is continually requested on one thread
            // and returned on another, avoid unbounded growth of the stack.
            if (stack.Count < MaxSize)
            {
                stack.Push(new RefAsValueType<T>(item));
            }
        }

        public static bool TryTake(out T item)
        {
            if (stack != null)
            {
                int count = stack.Count;
                if (count > 0)
                {
                    item = stack.Pop().Value;
                    return true;
                }
            }

            item = default(T);
            return false;
        }
    }
}
