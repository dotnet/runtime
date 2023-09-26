// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        // The special dll name to be used for DllImport of QCalls
        internal const string QCall = "QCall";

        public delegate void TryCode(object? userData);

        public delegate void CleanupCode(object? userData, bool exceptionThrown);

        /// <summary>
        /// Slices the specified array using the specified range.
        /// </summary>
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            (int offset, int length) = range.GetOffsetAndLength(array.Length);

            if (length == 0)
            {
                return Array.Empty<T>();
            }

            T[] dest = new T[length];

            // Due to array variance, it's possible that the incoming array is
            // actually of type U[], where U:T; or that an int[] <-> uint[] or
            // similar cast has occurred. In any case, since it's always legal
            // to reinterpret U as T in this scenario (but not necessarily the
            // other way around), we can use Buffer.Memmove here.

            Buffer.Memmove(
                ref MemoryMarshal.GetArrayDataReference(dest),
                ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), offset),
                (uint)length);

            return dest;
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(backoutCode);

            bool exceptionThrown = true;

            try
            {
                code(userData);
                exceptionThrown = false;
            }
            finally
            {
                backoutCode(userData, exceptionThrown);
            }
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareContractedDelegate(Delegate d)
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ProbeForSufficientStack()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegions()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegionsNoOP()
        {
        }

        internal static bool IsPrimitiveType(this CorElementType et)
            // COR_ELEMENT_TYPE_I1,I2,I4,I8,U1,U2,U4,U8,R4,R8,I,U,CHAR,BOOLEAN
            => ((1 << (int)et) & 0b_0011_0000_0000_0011_1111_1111_1100) != 0;

        /// <summary>Provide a fast way to access constant data stored in a module as a ReadOnlySpan{T}</summary>
        /// <param name="fldHandle">A field handle that specifies the location of the data to be referred to by the ReadOnlySpan{T}. The Rva of the field must be aligned on a natural boundary of type T</param>
        /// <returns>A ReadOnlySpan{T} of the data stored in the field</returns>
        /// <exception cref="ArgumentException"><paramref name="fldHandle"/> does not refer to a field which is an Rva, is misaligned, or T is of an invalid type.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code. T must be one of byte, sbyte, bool, char, short, ushort, int, uint, long, ulong, float, or double.</remarks>
        [Intrinsic]
        public static unsafe ReadOnlySpan<T> CreateSpan<T>(RuntimeFieldHandle fldHandle) => new ReadOnlySpan<T>(GetSpanDataFrom(fldHandle, typeof(T).TypeHandle, out int length), length);


        // The following intrinsics return true if input is a compile-time constant
        // Feel free to add more overloads on demand
#pragma warning disable IDE0060
        [Intrinsic]
        internal static bool IsKnownConstant(Type? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(string? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(char t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(int t) => false;
#pragma warning restore IDE0060

        // TODO, this method should be marked so that it is only callable from a runtime async method
        public static TResult UnsafeAwaitAwaiterFromRuntimeAsync<TResult, TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion2<TResult>
        {
            if (!awaiter.IsCompleted)
            {
                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate();
                if (resumption != null)
                {
                    // We are trying to suspend
                    bool threwException = true;
                    try
                    {
                        // Call the UnsafeOnCompleted api under a try block, as registering the suspension may cause
                        // an exception to occur.
                        awaiter.UnsafeOnCompleted(resumption);
                        threwException = false;
                    }
                    finally
                    {
                        // If UnsafeOnCompleted itself threw, we should bubble the error up, but we need
                        // to destroy any allocated tasklets that were created as part of the GetOrCreateResumptionDelegate api
                        // as that state will never be useable.
                        if (threwException)
                            RuntimeHelpers.AbortSuspend();
                    }
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    RuntimeHelpers.SuspendIfSuspensionNotAborted();
                }
            }

            // Get the result from the awaiter, or throw the exception stored in the Task
            return awaiter.GetResult();
        }

        // TODO, this method should be marked so that it is only callable from a runtime async method
        public static TResult AwaitAwaiterFromRuntimeAsync<TResult, TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion2<TResult>
        {
            if (!awaiter.IsCompleted)
            {
                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate();
                if (resumption != null)
                {
                    // We are trying to suspend
                    bool threwException = true;
                    try
                    {
                        // Call the OnCompleted api under a try block, as registering the suspension may cause
                        // an exception to occur.
                        awaiter.OnCompleted(resumption);
                        threwException = false;
                    }
                    finally
                    {
                        // If OnCompleted itself threw, we should bubble the error up, but we need
                        // to destroy any allocated tasklets that were created as part of the GetOrCreateResumptionDelegate api
                        // as that state will never be useable.
                        if (threwException)
                            RuntimeHelpers.AbortSuspend();
                    }
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    RuntimeHelpers.SuspendIfSuspensionNotAborted();
                }
            }

            // Get the result from the awaiter, or throw the exception stored in the Task
            return awaiter.GetResult();

        }

        // TODO, this method should be marked so that it is only callable from a runtime async method
        public static void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion2
        {
            if (!awaiter.IsCompleted)
            {
                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate();
                if (resumption != null)
                {
                    // We are trying to suspend
                    bool threwException = true;
                    try
                    {
                        // Call the UnsafeOnCompleted api under a try block, as registering the suspension may cause
                        // an exception to occur.
                        awaiter.UnsafeOnCompleted(resumption);
                        threwException = false;
                    }
                    finally
                    {
                        // If UnsafeOnCompleted itself threw, we should bubble the error up, but we need
                        // to destroy any allocated tasklets that were created as part of the GetOrCreateResumptionDelegate api
                        // as that state will never be useable.
                        if (threwException)
                            RuntimeHelpers.AbortSuspend();
                    }
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    RuntimeHelpers.SuspendIfSuspensionNotAborted();
                }
            }

            // Get the result from the awaiter, or throw the exception stored in the Task
            awaiter.GetResult();
        }

        // TODO, this method should be marked so that it is only callable from a runtime async method
        public static void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion2
        {
            if (!awaiter.IsCompleted)
            {
                // Create resumption delegate, wrapping task, and create tasklets to represent each stack frame on the stack.
                // RuntimeTaskSuspender.GetOrCreateResumptionDelegate() works like a POSIX fork call in that calls to it will return a
                // delegate if they are the initial call to GetOrCreateResumptionDelegate, but once the thread is resumed,
                // it will resume with a return value of null.
                Action? resumption = RuntimeHelpers.GetOrCreateResumptionDelegate();
                if (resumption != null)
                {
                    // We are trying to suspend
                    bool threwException = true;
                    try
                    {
                        // Call the OnCompleted api under a try block, as registering the suspension may cause
                        // an exception to occur.
                        awaiter.OnCompleted(resumption);
                        threwException = false;
                    }
                    finally
                    {
                        // If OnCompleted itself threw, we should bubble the error up, but we need
                        // to destroy any allocated tasklets that were created as part of the GetOrCreateResumptionDelegate api
                        // as that state will never be useable.
                        if (threwException)
                            RuntimeHelpers.AbortSuspend();
                    }
                    // If we reach here, the only way that we actually run follow on code is for the continuation to actually run,
                    // and return from GetOrCreateResumptionDelegate with a null return value.
                    RuntimeHelpers.SuspendIfSuspensionNotAborted();
                }
            }

            // Get the result from the awaiter, or throw the exception stored in the Task
            awaiter.GetResult();
        }

        private static void SuspendIfSuspensionNotAborted() {}
        private static Action? GetOrCreateResumptionDelegate() { return null; }
        private static void AbortSuspend() {}
    }
}
