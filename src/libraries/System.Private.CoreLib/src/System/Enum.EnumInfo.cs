// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public abstract partial class Enum
    {
        internal sealed unsafe class EnumInfo
        {
            public readonly bool HasFlagsAttribute;
            public readonly ulong[] Values;
            public readonly string[] Names;

            // The cached managed function pointer to the right JIT allocation helper.
            // Same as in RuntimeType.ActivationCache, just copied here to avoid cache conflicts.
            private delegate*<void*, object> _pfnAllocator;
            private void* _allocatorFirstArg;

            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public EnumInfo(bool hasFlagsAttribute, ulong[] values, string[] names)
            {
                HasFlagsAttribute = hasFlagsAttribute;
                Values = values;
                Names = names;
            }

            /// <summary>
            /// Creates an uninitialized object of the current type.
            /// </summary>
            /// <returns>An uninitalized instance of the object of the specified type.</returns>
            internal object CreateUninitializedInstance(RuntimeType type)
            {
                Diagnostics.Debug.Assert(type.IsEnum);

                if (_pfnAllocator is null)
                {
                    RuntimeTypeHandle.GetActivationInfo(
                        type,
                        out _pfnAllocator, out _allocatorFirstArg,
                        out _, out _);
                }

                object result = _pfnAllocator(_allocatorFirstArg);

                GC.KeepAlive(type);

                return result;
            }
        }
    }
}
