// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);

        // GetObjectValue is intended to allow value classes to be manipulated as 'Object'
        // but have aliasing behavior of a value class.  The intent is that you would use
        // this function just before an assignment to a variable of type 'Object'.  If the
        // value being assigned is a mutable value class, then a shallow copy is returned
        // (because value classes have copy semantics), but otherwise the object itself
        // is returned.
        //
        // Note: VB calls this method when they're about to assign to an Object
        // or pass it as a parameter.  The goal is to make sure that boxed
        // value types work identical to unboxed value types - ie, they get
        // cloned when you pass them around, and are always passed by value.
        // Of course, reference types are not cloned.
        //
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern object GetObjectValue(object obj);

        // RunClassConstructor causes the class constructor for the given type to be triggered
        // in the current domain.  After this call returns, the class constructor is guaranteed to
        // have at least been started by some thread.  In the absence of class constructor
        // deadlock conditions, the call is further guaranteed to have completed.
        //
        // This call will generate an exception if the specified class constructor threw an
        // exception when it ran.

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _RunClassConstructor(RuntimeType type);

        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            _RunClassConstructor(type.GetRuntimeType());
        }

        // RunModuleConstructor causes the module constructor for the given type to be triggered
        // in the current domain.  After this call returns, the module constructor is guaranteed to
        // have at least been started by some thread.  In the absence of module constructor
        // deadlock conditions, the call is further guaranteed to have completed.
        //
        // This call will generate an exception if the specified module constructor threw an
        // exception when it ran.

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _RunModuleConstructor(System.Reflection.RuntimeModule module);

        public static void RunModuleConstructor(ModuleHandle module)
        {
            _RunModuleConstructor(module.GetRuntimeModule());
        }


        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void _CompileMethod(RuntimeMethodHandleInternal method);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void _PrepareMethod(IRuntimeMethodInfo method, IntPtr* pInstantiation, int cInstantiation);

        public static void PrepareMethod(RuntimeMethodHandle method)
        {
            unsafe
            {
                _PrepareMethod(method.GetMethodInfo(), null, 0);
            }
        }

        public static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[]? instantiation)
        {
            unsafe
            {
                int length;
                IntPtr[]? instantiationHandles = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out length);
                fixed (IntPtr* pInstantiation = instantiationHandles)
                {
                    _PrepareMethod(method.GetMethodInfo(), pInstantiation, length);
                    GC.KeepAlive(instantiation);
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void PrepareDelegate(Delegate d);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetHashCode(object o);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public new static extern bool Equals(object? o1, object? o2);

        public static int OffsetToStringData
        {
            // This offset is baked in by string indexer intrinsic, so there is no harm
            // in getting it baked in here as well.
            [System.Runtime.Versioning.NonVersionable]
            get
            {
                // Number of bytes from the address pointed to by a reference to
                // a String to the first 16-bit character in the String.  Skip
                // over the MethodTable pointer, & String
                // length.  Of course, the String reference points to the memory
                // after the sync block, so don't count that.
                // This property allows C#'s fixed statement to work on Strings.
                // On 64 bit platforms, this should be 12 (8+4) and on 32 bit 8 (4+4).
#if BIT64
                return 12;
#else // 32
                return 8;
#endif // BIT64
            }
        }

        // This method ensures that there is sufficient stack to execute the average Framework function.
        // If there is not enough stack, then it throws System.InsufficientExecutionStackException.
        // Note: this method is not part of the CER support, and is not to be confused with ProbeForSufficientStack
        // below.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void EnsureSufficientExecutionStack();

        // This method ensures that there is sufficient stack to execute the average Framework function.
        // If there is not enough stack, then it return false.
        // Note: this method is not part of the CER support, and is not to be confused with ProbeForSufficientStack
        // below.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern bool TryEnsureSufficientExecutionStack();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData);

        internal static void ExecuteBackoutCodeHelper(object backoutCode, object? userData, bool exceptionThrown)
        {
            ((CleanupCode)backoutCode)(userData, exceptionThrown);
        }

        /// <returns>true if given type is reference type or value type that contains references</returns>
        public static bool IsReferenceOrContainsReferences<T>()
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForRuntimeHelpers for how this happens.
            throw new InvalidOperationException();
        }

        /// <returns>true if given type is bitwise equatable (memcmp can be used for equality checking)</returns>
        /// <remarks>
        /// Only use the result of this for Equals() comparison, not for CompareTo() comparison.
        /// </remarks>
        internal static bool IsBitwiseEquatable<T>()
        {
            // The body of this function will be replaced by the EE with unsafe code!!!
            // See getILIntrinsicImplementationForRuntimeHelpers for how this happens.
            throw new InvalidOperationException();
        }

        // Returns true iff the object has a component size;
        // i.e., is variable length like System.String or Array.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool ObjectHasComponentSize(object obj)
        {
            // CLR objects are laid out in memory as follows.
            // [ pMethodTable || .. object data .. ]
            //   ^-- the object reference points here
            //
            // The first DWORD of the method table class will have its high bit set if the
            // method table has component size info stored somewhere. See member
            // MethodTable:IsStringOrArray in src\vm\methodtable.h for full details.
            //
            // So in effect this method is the equivalent of
            // return ((MethodTable*)(*obj))->IsStringOrArray();

            Debug.Assert(obj != null);
            return *(int*)GetObjectMethodTablePointer(obj) < 0;
        }

        // Given an object reference, returns its MethodTable* as an IntPtr.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr GetObjectMethodTablePointer(object obj)
        {
            Debug.Assert(obj != null);

            // We know that the first data field in any managed object is immediately after the
            // method table pointer, so just back up one pointer and immediately deref.
            // This is not ideal in terms of minimizing instruction count but is the best we can do at the moment.

            return Unsafe.Add(ref Unsafe.As<byte, IntPtr>(ref obj.GetRawData()), -1);

            // The JIT currently implements this as:
            // lea tmp, [rax + 8h] ; assume rax contains the object reference, tmp is type IntPtr&
            // mov tmp, qword ptr [tmp - 8h] ; tmp now contains the MethodTable* pointer
            //
            // Ideally this would just be a single dereference:
            // mov tmp, qword ptr [rax] ; rax = obj ref, tmp = MethodTable* pointer
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object GetUninitializedObjectInternal(Type type);

    }
}
