// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace System.Reflection.Runtime.General
{
    //
    // This class dispenses randomized strings (that serve as both the fake name and fake assembly container) for
    // reflection-blocked types.
    //
    // The names are randomized to prevent apps from hard-wiring dependencies on them or attempting to serialize them
    // across app execution.
    //
    internal static class BlockedRuntimeTypeNameGenerator
    {
        public static string GetNameForBlockedRuntimeType(RuntimeTypeHandle typeHandle)
        {
            string name = s_blockedNameTable.GetOrAdd(new RuntimeTypeHandleKey(typeHandle));
            return name;
        }

        private sealed class BlockedRuntimeTypeNameTable : ConcurrentUnifier<RuntimeTypeHandleKey, string>
        {
            protected sealed override string Factory(RuntimeTypeHandleKey key)
            {
                uint count = s_counter++;
                return $"$BlockedFromReflection_{count}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            private static uint s_counter;
        }

        private static readonly BlockedRuntimeTypeNameTable s_blockedNameTable = new BlockedRuntimeTypeNameTable();
    }
}
