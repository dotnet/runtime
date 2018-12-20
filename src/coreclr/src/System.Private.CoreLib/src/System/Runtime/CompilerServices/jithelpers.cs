// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// JitHelpers
//    Low-level Jit Helpers
////////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // Wrapper for address of a string variable on stack
    internal struct StringHandleOnStack
    {
        private IntPtr m_ptr;

        internal StringHandleOnStack(IntPtr pString)
        {
            m_ptr = pString;
        }
    }

    // Wrapper for address of a object variable on stack
    internal struct ObjectHandleOnStack
    {
        private IntPtr m_ptr;

        internal ObjectHandleOnStack(IntPtr pObject)
        {
            m_ptr = pObject;
        }
    }

    // Wrapper for StackCrawlMark
    internal struct StackCrawlMarkHandle
    {
        private IntPtr m_ptr;

        internal StackCrawlMarkHandle(IntPtr stackMark)
        {
            m_ptr = stackMark;
        }
    }

    // Helper class to assist with unsafe pinning of arbitrary objects.
    // It's used by VM code.
    internal class RawData
    {
        public byte Data;
    }

    internal class RawSzArrayData
    {
        public IntPtr Count; // Array._numComponents padded to IntPtr
        public byte Data;
    }

    internal static unsafe class JitHelpers
    {
        // The special dll name to be used for DllImport of QCalls
        internal const string QCall = "QCall";

        // Wraps object variable into a handle. Used to return managed strings from QCalls.
        // s has to be a local variable on the stack.
        internal static StringHandleOnStack GetStringHandleOnStack(ref string s)
        {
            return new StringHandleOnStack((IntPtr)Unsafe.AsPointer(ref s));
        }

        // Wraps object variable into a handle. Used to pass managed object references in and out of QCalls.
        // o has to be a local variable on the stack.
        internal static ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o) where T : class
        {
            return new ObjectHandleOnStack((IntPtr)Unsafe.AsPointer(ref o));
        }

        // Wraps StackCrawlMark into a handle. Used to pass StackCrawlMark to QCalls.
        // stackMark has to be a local variable on the stack.
        internal static StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            return new StackCrawlMarkHandle((IntPtr)Unsafe.AsPointer(ref stackMark));
        }

#if DEBUG
        internal static int UnsafeEnumCast<T>(T val) where T : struct		// Actually T must be 4 byte (or less) enum
        {
            Debug.Assert(typeof(T).IsEnum
                              && (Enum.GetUnderlyingType(typeof(T)) == typeof(int)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(uint)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(short)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(ushort)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(byte)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte)),
                "Error, T must be an 4 byte (or less) enum JitHelpers.UnsafeEnumCast!");
            return UnsafeEnumCastInternal<T>(val);
        }

        private static int UnsafeEnumCastInternal<T>(T val) where T : struct		// Actually T must be 4 (or less) byte enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        internal static long UnsafeEnumCastLong<T>(T val) where T : struct		// Actually T must be 8 byte enum
        {
            Debug.Assert(typeof(T).IsEnum
                              && (Enum.GetUnderlyingType(typeof(T)) == typeof(long)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(ulong)),
                "Error, T must be an 8 byte enum JitHelpers.UnsafeEnumCastLong!");
            return UnsafeEnumCastLongInternal<T>(val);
        }

        private static long UnsafeEnumCastLongInternal<T>(T val) where T : struct	// Actually T must be 8 byte enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }
#else // DEBUG

        internal static int UnsafeEnumCast<T>(T val) where T : struct		// Actually T must be 4 byte (or less) enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        internal static long UnsafeEnumCastLong<T>(T val) where T : struct	// Actually T must be 8 byte enum
        {
            // should be return (long) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }
#endif // DEBUG

        // Set the given element in the array without any type or range checks
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void UnsafeSetArrayElement(object[] target, int index, object element);

        internal static ref byte GetRawData(this object obj) =>
            ref Unsafe.As<RawData>(obj).Data;

        internal static ref byte GetRawSzArrayData(this Array array) =>
            ref Unsafe.As<RawSzArrayData>(array).Data;
    }
}
