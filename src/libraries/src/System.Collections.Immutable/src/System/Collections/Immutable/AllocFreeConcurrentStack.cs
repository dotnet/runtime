// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Collections.Immutable
{
    [DebuggerDisplay("Count = {stack.Count}")]
    internal class AllocFreeConcurrentStack<T>
    {
        /// <summary>
        /// We use locks to protect this rather than ThreadLocal{T} because in perf tests
        /// uncontested locks are much faster than looking up thread-local storage.
        /// </summary>
        private readonly Stack<RefAsValueType<T>> stack = new Stack<RefAsValueType<T>>();

        public void TryAdd(T item)
        {
            lock (this.stack)
            {
                this.stack.Push(new RefAsValueType<T>(item));
            }
        }

        public bool TryTake(out T item)
        {
            lock (this.stack)
            {
                int count = this.stack.Count;
                if (count > 0)
                {
                    item = this.stack.Pop().Value;
                    return true;
                }
            }

            item = default(T);
            return false;
        }
    }
}
