// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

/*
 * This files defines the following types:
 *  - NativeOverlapped
 *  - _IOCompletionCallback
 *  - OverlappedData
 *  - Overlapped
 *  - OverlappedDataCache
 */

/*=============================================================================
**
**
**
** Purpose: Class for converting information to and from the native 
**          overlapped structure used in asynchronous file i/o
**
**
=============================================================================*/


using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace System.Threading
{
    #region struct NativeOverlapped

    // Valuetype that represents the (unmanaged) Win32 OVERLAPPED structure
    // the layout of this structure must be identical to OVERLAPPED.
    // The first five matches OVERLAPPED structure.
    // The remaining are reserved at the end
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    public struct NativeOverlapped
    {
        public IntPtr InternalLow;
        public IntPtr InternalHigh;
        public int OffsetLow;
        public int OffsetHigh;
        public IntPtr EventHandle;
    }

    #endregion struct NativeOverlapped


    #region class _IOCompletionCallback

    internal unsafe class _IOCompletionCallback
    {
        private IOCompletionCallback _ioCompletionCallback;
        private ExecutionContext _executionContext;
        private uint _errorCode; // Error code
        private uint _numBytes; // No. of bytes transferred 
        private NativeOverlapped* _pOVERLAP;

        internal _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
        {
            _ioCompletionCallback = ioCompletionCallback;
            _executionContext = executionContext;
        }
        // Context callback: same sig for SendOrPostCallback and ContextCallback
        internal static ContextCallback _ccb = new ContextCallback(IOCompletionCallback_Context);
        internal static void IOCompletionCallback_Context(Object state)
        {
            _IOCompletionCallback helper = (_IOCompletionCallback)state;
            Debug.Assert(helper != null, "_IOCompletionCallback cannot be null");
            helper._ioCompletionCallback(helper._errorCode, helper._numBytes, helper._pOVERLAP);
        }


        // call back helper
        internal static unsafe void PerformIOCompletionCallback(uint errorCode, // Error code
                                                                            uint numBytes, // No. of bytes transferred 
                                                                            NativeOverlapped* pOVERLAP // ptr to OVERLAP structure
                                                                            )
        {
            Overlapped overlapped;
            _IOCompletionCallback helper;

            do
            {
                overlapped = OverlappedData.GetOverlappedFromNative(pOVERLAP).m_overlapped;
                helper = overlapped.iocbHelper;

                if (helper == null || helper._executionContext == null || helper._executionContext.IsDefault)
                {
                    // We got here because of UnsafePack (or) Pack with EC flow suppressed
                    IOCompletionCallback callback = overlapped.UserCallback;
                    callback(errorCode, numBytes, pOVERLAP);
                }
                else
                {
                    // We got here because of Pack
                    helper._errorCode = errorCode;
                    helper._numBytes = numBytes;
                    helper._pOVERLAP = pOVERLAP;
                    ExecutionContext.RunInternal(helper._executionContext, _ccb, helper);
                }

                //Quickly check the VM again, to see if a packet has arrived.
                OverlappedData.CheckVMForIOPacket(out pOVERLAP, out errorCode, out numBytes);
            } while (pOVERLAP != null);
        }
    }

    #endregion class _IOCompletionCallback


    #region class OverlappedData

    sealed internal class OverlappedData
    {
        // ! If you make any change to the layout here, you need to make matching change 
        // ! to OverlappedObject in vm\nativeoverlapped.h
        internal IAsyncResult m_asyncResult;
        internal IOCompletionCallback m_iocb;
        internal _IOCompletionCallback m_iocbHelper;
        internal Overlapped m_overlapped;
        private Object m_userObject;
        private IntPtr m_pinSelf;
        private IntPtr m_userObjectInternal;
        private int m_AppDomainId;
#pragma warning disable 414  // Field is not used from managed.        
#pragma warning disable 169
        private byte m_isArray;
        private byte m_toBeCleaned;
#pragma warning restore 414        
#pragma warning restore 169
        internal NativeOverlapped m_nativeOverlapped;

        // Adding an empty default ctor for annotation purposes
        internal OverlappedData() { }

        internal void ReInitialize()
        {
            m_asyncResult = null;
            m_iocb = null;
            m_iocbHelper = null;
            m_overlapped = null;
            m_userObject = null;
            Debug.Assert(m_pinSelf == IntPtr.Zero, "OverlappedData has not been freed: m_pinSelf");
            m_pinSelf = IntPtr.Zero;
            m_userObjectInternal = IntPtr.Zero;
            Debug.Assert(m_AppDomainId == 0 || m_AppDomainId == AppDomain.CurrentDomain.Id, "OverlappedData is not in the current domain");
            m_AppDomainId = 0;
            m_nativeOverlapped.EventHandle = IntPtr.Zero;
            m_isArray = 0;
            m_nativeOverlapped.InternalLow = IntPtr.Zero;
            m_nativeOverlapped.InternalHigh = IntPtr.Zero;
        }

        internal unsafe NativeOverlapped* Pack(IOCompletionCallback iocb, Object userData)
        {
            if (m_pinSelf != IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
            }

            if (iocb != null)
            {
                ExecutionContext ec = ExecutionContext.Capture();
                m_iocbHelper = ec != null ? new _IOCompletionCallback(iocb, ec) : null;
                m_iocb = iocb;
            }
            else
            {
                m_iocbHelper = null;
                m_iocb = null;
            }
            m_userObject = userData;
            if (m_userObject != null)
            {
                if (m_userObject.GetType() == typeof(Object[]))
                {
                    m_isArray = 1;
                }
                else
                {
                    m_isArray = 0;
                }
            }
            return AllocateNativeOverlapped();
        }

        internal unsafe NativeOverlapped* UnsafePack(IOCompletionCallback iocb, Object userData)
        {
            if (m_pinSelf != IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
            }
            m_userObject = userData;
            if (m_userObject != null)
            {
                if (m_userObject.GetType() == typeof(Object[]))
                {
                    m_isArray = 1;
                }
                else
                {
                    m_isArray = 0;
                }
            }
            m_iocb = iocb;
            m_iocbHelper = null;
            return AllocateNativeOverlapped();
        }

        internal IntPtr UserHandle
        {
            get { return m_nativeOverlapped.EventHandle; }
            set { m_nativeOverlapped.EventHandle = value; }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern unsafe NativeOverlapped* AllocateNativeOverlapped();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void FreeNativeOverlapped(NativeOverlapped* nativeOverlappedPtr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe OverlappedData GetOverlappedFromNative(NativeOverlapped* nativeOverlappedPtr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void CheckVMForIOPacket(out NativeOverlapped* pOVERLAP, out uint errorCode, out uint numBytes);
    }

    #endregion class OverlappedData


    #region class Overlapped

    public class Overlapped
    {
        private OverlappedData m_overlappedData;
        private static PinnableBufferCache s_overlappedDataCache = new PinnableBufferCache("System.Threading.OverlappedData", () => new OverlappedData());

        public Overlapped()
        {
            m_overlappedData = (OverlappedData)s_overlappedDataCache.Allocate();
            m_overlappedData.m_overlapped = this;
        }

        public Overlapped(int offsetLo, int offsetHi, IntPtr hEvent, IAsyncResult ar)
        {
            m_overlappedData = (OverlappedData)s_overlappedDataCache.Allocate();
            m_overlappedData.m_overlapped = this;
            m_overlappedData.m_nativeOverlapped.OffsetLow = offsetLo;
            m_overlappedData.m_nativeOverlapped.OffsetHigh = offsetHi;
            m_overlappedData.UserHandle = hEvent;
            m_overlappedData.m_asyncResult = ar;
        }

        [Obsolete("This constructor is not 64-bit compatible.  Use the constructor that takes an IntPtr for the event handle.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult ar) : this(offsetLo, offsetHi, new IntPtr(hEvent), ar)
        {
        }

        public IAsyncResult AsyncResult
        {
            get { return m_overlappedData.m_asyncResult; }
            set { m_overlappedData.m_asyncResult = value; }
        }

        public int OffsetLow
        {
            get { return m_overlappedData.m_nativeOverlapped.OffsetLow; }
            set { m_overlappedData.m_nativeOverlapped.OffsetLow = value; }
        }

        public int OffsetHigh
        {
            get { return m_overlappedData.m_nativeOverlapped.OffsetHigh; }
            set { m_overlappedData.m_nativeOverlapped.OffsetHigh = value; }
        }

        [Obsolete("This property is not 64-bit compatible.  Use EventHandleIntPtr instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventHandle
        {
            get { return m_overlappedData.UserHandle.ToInt32(); }
            set { m_overlappedData.UserHandle = new IntPtr(value); }
        }

        public IntPtr EventHandleIntPtr
        {
            get { return m_overlappedData.UserHandle; }
            set { m_overlappedData.UserHandle = value; }
        }

        internal _IOCompletionCallback iocbHelper
        {
            get { return m_overlappedData.m_iocbHelper; }
        }

        internal IOCompletionCallback UserCallback
        {
            get { return m_overlappedData.m_iocb; }
        }

        /*====================================================================
        *  Packs a managed overlapped class into native Overlapped struct.
        *  Roots the iocb and stores it in the ReservedCOR field of native Overlapped 
        *  Pins the native Overlapped struct and returns the pinned index. 
        ====================================================================*/
        [Obsolete("This method is not safe.  Use Pack (iocb, userData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback iocb)
        {
            return Pack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback iocb, Object userData)
        {
            return m_overlappedData.Pack(iocb, userData);
        }

        [Obsolete("This method is not safe.  Use UnsafePack (iocb, userData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback iocb)
        {
            return UnsafePack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback iocb, Object userData)
        {
            return m_overlappedData.UnsafePack(iocb, userData);
        }

        /*====================================================================
        *  Unpacks an unmanaged native Overlapped struct. 
        *  Unpins the native Overlapped struct
        ====================================================================*/
        [CLSCompliant(false)]
        public static unsafe Overlapped Unpack(NativeOverlapped* nativeOverlappedPtr)
        {
            if (nativeOverlappedPtr == null)
                throw new ArgumentNullException(nameof(nativeOverlappedPtr));

            Overlapped overlapped = OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr).m_overlapped;

            return overlapped;
        }

        [CLSCompliant(false)]
        public static unsafe void Free(NativeOverlapped* nativeOverlappedPtr)
        {
            if (nativeOverlappedPtr == null)
                throw new ArgumentNullException(nameof(nativeOverlappedPtr));

            Overlapped overlapped = OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr).m_overlapped;
            OverlappedData.FreeNativeOverlapped(nativeOverlappedPtr);
            OverlappedData overlappedData = overlapped.m_overlappedData;
            overlapped.m_overlappedData = null;
            overlappedData.ReInitialize();
            s_overlappedDataCache.Free(overlappedData);
        }
    }

    #endregion class Overlapped
}  // namespace
