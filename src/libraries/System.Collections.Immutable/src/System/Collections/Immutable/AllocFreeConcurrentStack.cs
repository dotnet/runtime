// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal static class AllocFreeConcurrentStack<T>
    {
        private const int MaxSize = 35;
        private static readonly Type s_typeOfT = typeof(T);

        public static void TryAdd(T item)
        {
            // Just in case we're in a scenario where an object is continually requested on one thread
            // and returned on another, avoid unbounded growth of the stack.
            Stack<RefAsValueType<T>> localStack = ThreadLocalStack;
            if (localStack.Count < MaxSize)
            {
                localStack.Push(new RefAsValueType<T>(item));
            }
        }

        public static bool TryTake(out T item)
        {
            Stack<RefAsValueType<T>> localStack = ThreadLocalStack;
            if (localStack != null && localStack.Count > 0)
            {
                item = localStack.Pop().Value;
                return true;
            }

            item = default(T);
            return false;
        }

        private static Stack<RefAsValueType<T>> ThreadLocalStack
        {
            get
            {
                // Ensure the [ThreadStatic] is initialized to a dictionary
                Dictionary<Type, object> typesToStacks = AllocFreeConcurrentStack.t_stacks;
                if (typesToStacks == null)
                {
                    AllocFreeConcurrentStack.t_stacks = typesToStacks = new Dictionary<Type, object>();
                }

                // Get the stack that corresponds to the T
                object stackObj;
                if (!typesToStacks.TryGetValue(s_typeOfT, out stackObj))
                {
                    stackObj = new Stack<RefAsValueType<T>>(MaxSize);
                    typesToStacks.Add(s_typeOfT, stackObj);
                }

                // Return it as the correct type.
                return (Stack<RefAsValueType<T>>)stackObj;
            }
        }
    }

    internal static class AllocFreeConcurrentStack
    {
        // Workaround for https://github.com/dotnet/coreclr/issues/2191.
        // When that's fixed, a [ThreadStatic] Stack should be added back to AllocFreeConcurrentStack<T>.

        [ThreadStatic]
        internal static Dictionary<Type, object> t_stacks;
    }
}
