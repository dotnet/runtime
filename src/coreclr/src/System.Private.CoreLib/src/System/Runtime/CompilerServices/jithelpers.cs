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
        internal static StringHandleOnStack GetStringHandleOnStack(ref string? s)
        {
            return new StringHandleOnStack((IntPtr)Unsafe.AsPointer(ref s));
        }

        // Wraps object variable into a handle. Used to pass managed object references in and out of QCalls.
        // o has to be a local variable on the stack.
        internal static ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o) where T : class?
        {
            return new ObjectHandleOnStack((IntPtr)Unsafe.AsPointer(ref o));
        }

        // Wraps StackCrawlMark into a handle. Used to pass StackCrawlMark to QCalls.
        // stackMark has to be a local variable on the stack.
        internal static StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            return new StackCrawlMarkHandle((IntPtr)Unsafe.AsPointer(ref stackMark));
        }

        internal static bool EnumEquals<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.Equals(y);
        }

        internal static int EnumCompareTo<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.CompareTo(y);
        }

        internal static ref byte GetRawData(this object obj) =>
            ref Unsafe.As<RawData>(obj).Data;

        internal static ref byte GetRawSzArrayData(this Array array) =>
            ref Unsafe.As<RawSzArrayData>(array).Data;
    }
}
