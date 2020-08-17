// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        public static bool TryTake([MaybeNullWhen(false)] out T item)
        {
            Stack<RefAsValueType<T>> localStack = ThreadLocalStack;
            if (localStack != null && localStack.Count > 0)
            {
                item = localStack.Pop().Value;
                return true;
            }

            item = default;
            return false;
        }

        private static Stack<RefAsValueType<T>> ThreadLocalStack
        {
            get
            {
                // Ensure the [ThreadStatic] is initialized to a dictionary
                Dictionary<Type, object>? typesToStacks = AllocFreeConcurrentStack.t_stacks;
                if (typesToStacks == null)
                {
                    AllocFreeConcurrentStack.t_stacks = typesToStacks = new Dictionary<Type, object>();
                }

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
    }

    internal static class AllocFreeConcurrentStack
    {
        // WARNING: We allow diagnostic tools to directly inspect this member (t_stacks).
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.

        // Workaround for https://github.com/dotnet/runtime/issues/4731.
        // When that's fixed, a [ThreadStatic] Stack should be added back to AllocFreeConcurrentStack<T>.

        [ThreadStatic]
        internal static Dictionary<Type, object>? t_stacks;
    }
}
