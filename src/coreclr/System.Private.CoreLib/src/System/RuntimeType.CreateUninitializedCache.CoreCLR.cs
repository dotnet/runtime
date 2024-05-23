// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal sealed partial class RuntimeType
    {
        /// <summary>
        /// A cache which allows optimizing <see cref="RuntimeHelpers.GetUninitializedObject(Type)"/>.
        /// </summary>
        private sealed unsafe partial class CreateUninitializedCache
        {
            // The managed calli to the newobj allocator, plus its first argument (MethodTable*).
            private readonly delegate*<void*, object> _pfnAllocator;
            private readonly void* _allocatorFirstArg;

#if DEBUG
            private readonly RuntimeType _originalRuntimeType;
#endif

            internal CreateUninitializedCache(RuntimeType rt)
            {
                Debug.Assert(rt != null);

#if DEBUG
                _originalRuntimeType = rt;
#endif

                GetCreateUninitializedInfo(rt, out _pfnAllocator, out _allocatorFirstArg);
            }

            internal object CreateUninitializedObject(RuntimeType rt)
            {
#if DEBUG
                if (_originalRuntimeType != rt)
                {
                    Debug.Fail("Caller passed the wrong RuntimeType to this routine."
                        + Environment.NewLineConst + "Expected: " + (_originalRuntimeType ?? (object)"<null>")
                        + Environment.NewLineConst + "Actual: " + (rt ?? (object)"<null>"));
                }
#endif
                object retVal = _pfnAllocator(_allocatorFirstArg);
                GC.KeepAlive(rt);
                return retVal;
            }

            /// <summary>
            /// Given a RuntimeType, returns information about how to create uninitialized instances
            /// of it via calli semantics.
            /// </summary>
            private static void GetCreateUninitializedInfo(
                RuntimeType rt,
                out delegate*<void*, object> pfnAllocator,
                out void* vAllocatorFirstArg)
            {
                Debug.Assert(rt != null);

                delegate*<void*, object> pfnAllocatorTemp = default;
                void* vAllocatorFirstArgTemp = default;

                GetCreateUninitializedInfo(
                    new QCallTypeHandle(ref rt),
                    &pfnAllocatorTemp, &vAllocatorFirstArgTemp);

                pfnAllocator = pfnAllocatorTemp;
                vAllocatorFirstArg = vAllocatorFirstArgTemp;
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ReflectionSerialization_GetCreateUninitializedObjectInfo")]
            private static partial void GetCreateUninitializedInfo(
                QCallTypeHandle type,
                delegate*<void*, object>* ppfnAllocator,
                void** pvAllocatorFirstArg);
        }
    }
}
