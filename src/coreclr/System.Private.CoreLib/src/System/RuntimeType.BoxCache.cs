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
        /// A cache which allows optimizing <see cref="RuntimeHelpers.Box(ref byte, RuntimeTypeHandle)"/>.
        /// </summary>
        internal sealed unsafe partial class BoxCache : IGenericCacheEntry<BoxCache>
        {
            public static BoxCache Create(RuntimeType type) => new(type);
            public void InitializeCompositeCache(CompositeCacheEntry compositeEntry) => compositeEntry._boxCache = this;
            public static ref BoxCache? GetStorageRef(CompositeCacheEntry compositeEntry) => ref compositeEntry._boxCache;

            // The managed calli to the newobj allocator, plus its first argument
            private readonly delegate*<void*, object> _pfnAllocator;
            private readonly void* _allocatorFirstArg;
            private readonly int _nullableValueOffset;
            private readonly uint _valueTypeSize;
            private readonly MethodTable* _pMT;

#if DEBUG
            private readonly RuntimeType _originalRuntimeType;
#endif

            private BoxCache(RuntimeType rt)
            {
                Debug.Assert(rt != null);

#if DEBUG
                _originalRuntimeType = rt;
#endif

                TypeHandle handle = rt.TypeHandle.GetNativeTypeHandle();

                if (handle.IsTypeDesc)
                    throw new ArgumentException(SR.Arg_TypeNotSupported);

                _pMT = handle.AsMethodTable();

                // For value types, this is checked in GetBoxInfo,
                // but for non-value types, we still need to check this case for consistent behavior.
                if (_pMT->ContainsGenericVariables)
                    throw new ArgumentException(SR.Arg_TypeNotSupported);

                if (_pMT->IsValueType)
                {
                    GetBoxInfo(rt, out _pfnAllocator, out _allocatorFirstArg, out _nullableValueOffset, out _valueTypeSize);
                }
            }

            internal object? Box(RuntimeType rt, ref byte data)
            {
#if DEBUG
                if (_originalRuntimeType != rt)
                {
                    Debug.Fail("Caller passed the wrong RuntimeType to this routine."
                        + Environment.NewLineConst + "Expected: " + (_originalRuntimeType ?? (object)"<null>")
                        + Environment.NewLineConst + "Actual: " + (rt ?? (object)"<null>"));
                }
#endif
                if (_pfnAllocator == null)
                {
                    // If the allocator is null, then we shouldn't allocate and make a copy,
                    // we should return the data as the object it currently is.
                    return Unsafe.As<byte, object>(ref data);
                }

                ref byte source = ref data;

                byte maybeNullableHasValue = Unsafe.ReadUnaligned<byte>(ref source);

                if (_nullableValueOffset != 0)
                {
                    if (maybeNullableHasValue == 0)
                    {
                        return null;
                    }
                    source = ref Unsafe.Add(ref source, _nullableValueOffset);
                }

                object result = _pfnAllocator(_allocatorFirstArg);
                GC.KeepAlive(rt);

                if (_pMT->ContainsGCPointers)
                {
                    Buffer.BulkMoveWithWriteBarrier(ref result.GetRawData(), ref source, _valueTypeSize);
                }
                else
                {
                    SpanHelpers.Memmove(ref result.GetRawData(), ref source, _valueTypeSize);
                }

                return result;
            }

            /// <summary>
            /// Given a RuntimeType, returns information about how to box instances
            /// of it via calli semantics.
            /// </summary>
            private static void GetBoxInfo(
                RuntimeType rt,
                out delegate*<void*, object> pfnAllocator,
                out void* vAllocatorFirstArg,
                out int nullableValueOffset,
                out uint valueTypeSize)
            {
                Debug.Assert(rt != null);

                delegate*<void*, object> pfnAllocatorTemp = default;
                void* vAllocatorFirstArgTemp = default;
                int nullableValueOffsetTemp = default;
                uint valueTypeSizeTemp = default;

                GetBoxInfo(
                    new QCallTypeHandle(ref rt),
                    &pfnAllocatorTemp, &vAllocatorFirstArgTemp,
                    &nullableValueOffsetTemp, &valueTypeSizeTemp);

                pfnAllocator = pfnAllocatorTemp;
                vAllocatorFirstArg = vAllocatorFirstArgTemp;
                nullableValueOffset = nullableValueOffsetTemp;
                valueTypeSize = valueTypeSizeTemp;
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ReflectionInvocation_GetBoxInfo")]
            private static partial void GetBoxInfo(
                QCallTypeHandle type,
                delegate*<void*, object>* ppfnAllocator,
                void** pvAllocatorFirstArg,
                int* pNullableValueOffset,
                uint* pValueTypeSize);
        }

        internal object? Box(ref byte data)
        {
            return GetOrCreateCacheEntry<BoxCache>().Box(this, ref data);
        }
    }
}
