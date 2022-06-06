// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public abstract partial class Enum
    {
        internal sealed unsafe class EnumInfo
        {
            /// <summary>
            /// Set by <see cref="EnsureReflectionInfoIsInitialized(RuntimeType, bool)"/>, tracks when
            /// <see cref="HasFlagsAttribute"/>, <see cref="Values"/> and <see cref="Names"/> have been set.
            /// </summary>
            private bool _isReflectionInfoInitialized;

            public bool HasFlagsAttribute;
            public ulong[]? Values;
            public string[]? Names;

            /// <summary>
            /// The cached managed function pointer to the right JIT allocation helper.
            /// Same as in RuntimeType.ActivationCache, just copied here to avoid cache conflicts.
            /// </summary>
            private delegate*<void*, object> _pfnAllocator;

            /// <summary>
            /// Initializes the reflection info (<see cref="HasFlagsAttribute"/>, <see cref="Values"/> and <see cref="Names"/>).
            /// </summary>
            /// <param name="enumType">The enum type to use.</param>
            /// <param name="getNames">Whether or not to also retrieve the enum names.</param>
            public void EnsureReflectionInfoIsInitialized(RuntimeType enumType, bool getNames)
            {
                if (!_isReflectionInfoInitialized || (getNames && Names is null))
                {
                    ulong[]? values = null;
                    string[]? names = null;
                    RuntimeTypeHandle enumTypeHandle = enumType.TypeHandle;

                    GetEnumValuesAndNames(
                        new QCallTypeHandle(ref enumTypeHandle),
                        ObjectHandleOnStack.Create(ref values),
                        ObjectHandleOnStack.Create(ref names),
                        getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);

                    bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                    HasFlagsAttribute = hasFlagsAttribute;
                    Values = values!;
                    Names = names!;

                    _isReflectionInfoInitialized = true;
                }
            }

            /// <summary>
            /// Creates an uninitialized object of the current type.
            /// </summary>
            /// <param name="enumType">The enum type to use.</param>
            /// <returns>An uninitalized instance of the object of the specified type.</returns>
            public object CreateUninitializedInstance(RuntimeType enumType)
            {
                Diagnostics.Debug.Assert(enumType.IsEnum);

                delegate*<void*, object> pfnAllocator = _pfnAllocator;

                if (pfnAllocator is null)
                {
                    RuntimeTypeHandle.GetActivationInfo(
                        enumType,
                        out pfnAllocator,
                        out _,
                        out _,
                        out _);

                    _pfnAllocator = pfnAllocator;
                }

                // The allocator argument is the method table. For enum types, that's just the type handle
                object result = pfnAllocator((MethodTable*)enumType.m_handle);

                GC.KeepAlive(enumType);

                return result;
            }
        }
    }
}
