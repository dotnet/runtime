// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    internal static class AllocFreeConcurrentStack<T>
    {
        private const int MaxSize = 35;

#if NETCOREAPP
        [ThreadStatic]
        private static Stack<RefAsValueType<T>>? t_stack;
#else
        private static readonly Type s_typeOfT = typeof(T);
#endif

        public static void TryAdd(T item)
        {
            Stack<RefAsValueType<T>> localStack =
#if NETCOREAPP
                t_stack ??= new Stack<RefAsValueType<T>>(MaxSize);
#else
                ThreadLocalStack;
#endif

            // Just in case we're in a scenario where an object is continually requested on one thread
            // and returned on another, avoid unbounded growth of the stack.
            if (localStack.Count < MaxSize)
            {
                localStack.Push(new RefAsValueType<T>(item));
            }
        }

        public static bool TryTake([MaybeNullWhen(false)] out T item)
        {
#if NETCOREAPP
            Stack<RefAsValueType<T>>? localStack = t_stack; // cache in a local to avoid unnecessary TLS hits on repeated accesses
#else
            Stack<RefAsValueType<T>> localStack = ThreadLocalStack;
#endif
            if (localStack != null && localStack.Count > 0)
            {
                item = localStack.Pop().Value;
                return true;
            }

            item = default;
            return false;
        }

#if !NETCOREAPP
        private static Stack<RefAsValueType<T>> ThreadLocalStack
        {
            get
            {
                // Ensure the [ThreadStatic] is initialized to a dictionary
                Dictionary<Type, object>? typesToStacks = AllocFreeConcurrentStack.t_stacks ??= new Dictionary<Type, object>();

                // Get the stack that corresponds to the T
                if (!typesToStacks.TryGetValue(s_typeOfT, out object? stackObj))
                {
                    stackObj = new Stack<RefAsValueType<T>>(MaxSize);
                    typesToStacks.Add(s_typeOfT, stackObj);
                }

                // Return it as the correct type.
                return (Stack<RefAsValueType<T>>)stackObj;
            }
        }
#endif
    }

#if !NETCOREAPP
    internal static class AllocFreeConcurrentStack
    {
        // Workaround for https://github.com/dotnet/runtime/issues/4731.
        [ThreadStatic]
        internal static Dictionary<Type, object>? t_stacks;
    }
#endif
}
