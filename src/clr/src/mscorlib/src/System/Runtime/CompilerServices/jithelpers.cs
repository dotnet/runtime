// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// JitHelpers
//    Low-level Jit Helpers
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Runtime;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.CompilerServices {

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

    // Helper class to assist with unsafe pinning of arbitrary objects. The typical usage pattern is:
    // fixed (byte * pData = &JitHelpers.GetPinningHelper(value).m_data)
    // {
    //    ... pData is what Object::GetData() returns in VM ...
    // }
    internal class PinningHelper
    {
        public byte m_data;
    }

    [FriendAccessAllowed]
    internal static class JitHelpers
    {
        // The special dll name to be used for DllImport of QCalls
        internal const string QCall = "QCall";

        // Wraps object variable into a handle. Used to return managed strings from QCalls.
        // s has to be a local variable on the stack.
        [SecurityCritical]
        static internal StringHandleOnStack GetStringHandleOnStack(ref string s)
        {
            return new StringHandleOnStack(UnsafeCastToStackPointer(ref s));
        }

        // Wraps object variable into a handle. Used to pass managed object references in and out of QCalls.
        // o has to be a local variable on the stack.
        [SecurityCritical]
        static internal ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o) where T : class
        {
            return new ObjectHandleOnStack(UnsafeCastToStackPointer(ref o));
        }

        // Wraps StackCrawlMark into a handle. Used to pass StackCrawlMark to QCalls.
        // stackMark has to be a local variable on the stack.
        [SecurityCritical]
        static internal StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            return new StackCrawlMarkHandle(UnsafeCastToStackPointer(ref stackMark));
        }

#if _DEBUG
        [SecurityCritical]
        [FriendAccessAllowed]
        static internal T UnsafeCast<T>(Object o) where T : class
        {
            T ret = UnsafeCastInternal<T>(o);
            Contract.Assert(ret == (o as T), "Invalid use of JitHelpers.UnsafeCast!");
            return ret;
        }

        // The IL body of this method is not critical, but its body will be replaced with unsafe code, so
        // this method is effectively critical
        [SecurityCritical]
        static private T UnsafeCastInternal<T>(Object o) where T : class
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns o!!!
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        static internal int UnsafeEnumCast<T>(T val) where T : struct		// Actually T must be 4 byte (or less) enum
        {
            Contract.Assert(typeof(T).IsEnum 
                              && (Enum.GetUnderlyingType(typeof(T)) == typeof(int) 
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(uint) 
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(short)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(ushort)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(byte)
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte)),
                "Error, T must be an 4 byte (or less) enum JitHelpers.UnsafeEnumCast!");            
            return UnsafeEnumCastInternal<T>(val);
        }

        static private int UnsafeEnumCastInternal<T>(T val) where T : struct		// Actually T must be 4 (or less) byte enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        static internal long UnsafeEnumCastLong<T>(T val) where T : struct		// Actually T must be 8 byte enum
        {
            Contract.Assert(typeof(T).IsEnum 
                              && (Enum.GetUnderlyingType(typeof(T)) == typeof(long) 
                                  || Enum.GetUnderlyingType(typeof(T)) == typeof(ulong)), 
                "Error, T must be an 8 byte enum JitHelpers.UnsafeEnumCastLong!");
            return UnsafeEnumCastLongInternal<T>(val);
        }

        static private long UnsafeEnumCastLongInternal<T>(T val) where T : struct	// Actually T must be 8 byte enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        // Internal method for getting a raw pointer for handles in JitHelpers.
        // The reference has to point into a local stack variable in order so it can not be moved by the GC.
        [SecurityCritical]
        static internal IntPtr UnsafeCastToStackPointer<T>(ref T val)
        {
            IntPtr p = UnsafeCastToStackPointerInternal<T>(ref val);
            Contract.Assert(IsAddressInStack(p), "Pointer not in the stack!");
            return p;
        }

        [SecurityCritical]
        static private IntPtr UnsafeCastToStackPointerInternal<T>(ref T val)
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns val!!!
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }
#else // _DEBUG
        // The IL body of this method is not critical, but its body will be replaced with unsafe code, so
        // this method is effectively critical
        [SecurityCritical]
        [FriendAccessAllowed]
        static internal T UnsafeCast<T>(Object o) where T : class
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns o!!!
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        static internal int UnsafeEnumCast<T>(T val) where T : struct		// Actually T must be 4 byte (or less) enum
        {
            // should be return (int) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        static internal long UnsafeEnumCastLong<T>(T val) where T : struct	// Actually T must be 8 byte enum
        {
            // should be return (long) val; but C# does not allow, runtime does this magically
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }

        [SecurityCritical]
        static internal IntPtr UnsafeCastToStackPointer<T>(ref T val)
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns o!!!
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }
#endif // _DEBUG

        // Set the given element in the array without any type or range checks
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static internal void UnsafeSetArrayElement(Object[] target, int index, Object element);

        // Used for unsafe pinning of arbitrary objects.
        [System.Security.SecurityCritical]  // auto-generated
        static internal PinningHelper GetPinningHelper(Object o)
        {
            // This cast is really unsafe - call the private version that does not assert in debug
#if _DEBUG
            return UnsafeCastInternal<PinningHelper>(o);
#else
            return UnsafeCast<PinningHelper>(o);
#endif
        }

#if _DEBUG
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern static bool IsAddressInStack(IntPtr ptr);
#endif
    }
}
