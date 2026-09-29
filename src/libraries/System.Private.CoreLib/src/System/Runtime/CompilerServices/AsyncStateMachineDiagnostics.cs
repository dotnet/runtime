// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    internal static class AsyncStateMachineDiagnostics<TStateMachine>
        where TStateMachine : IAsyncStateMachine
    {
#if NATIVEAOT
        // In NativeAOT we don't have reflection to resolve the method handle and state field offset.
        // Due to the way the state machine is constructed, we can't get a direct pointer to its MoveNext method
        // and using the interface dispatch to locate the method at slot 0 is unreliable due to Native AOT optimizations.
        // The state field is also not guaranteed to be at a specific offset due to auto layout and Native AOT optimizations.
        // To support this on Native AOT we would need to precompute this information in ILC and emit a
        // hash table keyed by state machine MethodTable. At runtime we would still need to cache
        // this data in static fields to avoid lookup cost when walking each continuation frame.
        // On JIT these static fields are lazy evaluated and cached on initial access, but on Native AOT
        // they will be pre-allocated, so code should be linked out when diagnostics is not supported.
        // Given the added complexity on Native AOT, the fact that this is only used for diagnostics,
        // and that Native AOT currently have limited asyncv1 diagnostics support in tooling, we can
        // postpone the support until proven needed.
        public static ulong MethodId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetState(ref TStateMachine? _) => -1;
#else
        private static readonly ulong s_methodId = ResolveMethodId();
        private static readonly int s_resolveStateFieldOffset = ResolveStateFieldOffset();

        public static ulong MethodId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_methodId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetState(ref TStateMachine? stateMachine)
        {
            if (typeof(TStateMachine).IsValueType)
            {
                // Struct: state field is inline at offset within the struct
                return Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref Unsafe.As<TStateMachine?, byte>(ref stateMachine), (nint)s_resolveStateFieldOffset));
            }
            else
            {
                // Class (debug builds): StateMachine is a reference, dereference to get object data
                if (stateMachine is not null)
                {
                    return Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref RuntimeHelpers.GetRawData(stateMachine), (nint)s_resolveStateFieldOffset));
                }
            }

            return -1;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090", Justification = "State machine types are always preserved.")]
        private static ulong ResolveMethodId()
        {
            MethodInfo? methodInfo = typeof(TStateMachine).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo is not null)
            {
#if MONO
                return (ulong)(nuint)RuntimeMethodHandle.GetNativeCodeInternal(methodInfo.MethodHandle.Value);
#else
                if (methodInfo is IRuntimeMethodInfo runtimeMethodInfo)
                {
                    return (ulong)(nuint)RuntimeMethodHandle.GetNativeCodeInternal(runtimeMethodInfo);
                }
#endif
            }

            return 0;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090", Justification = "State machine types are always preserved.")]
        private static int ResolveStateFieldOffset()
        {
            FieldInfo? stateField = typeof(TStateMachine).GetField("<>1__state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (stateField is not null)
            {
#if MONO
                return stateField.GetFieldOffset();
#else
                Debug.Assert(stateField is RtFieldInfo, $"Expected RtFieldInfo but got {stateField.GetType().Name}");
                return RuntimeFieldHandle.GetInstanceFieldOffset((RtFieldInfo)stateField);
#endif
            }

            return 0;
        }
#endif
    }
}
