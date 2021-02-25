// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

/*
 * This files defines the following types:
 *  - _IOCompletionCallback
 *  - OverlappedData
 *  - Overlapped
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

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    #region class _IOCompletionCallback

    internal unsafe class _IOCompletionCallback
    {
        private readonly IOCompletionCallback _ioCompletionCallback;
        private readonly ExecutionContext _executionContext;
        private uint _errorCode; // Error code
        private uint _numBytes; // No. of bytes transferred
        private NativeOverlapped* _pNativeOverlapped;

        internal _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
        {
            _ioCompletionCallback = ioCompletionCallback;
            _executionContext = executionContext;
        }
        // Context callback: same sig for SendOrPostCallback and ContextCallback
        internal static ContextCallback _ccb = new ContextCallback(IOCompletionCallback_Context);
        internal static void IOCompletionCallback_Context(object? state)
        {
            _IOCompletionCallback? helper = (_IOCompletionCallback?)state;
            Debug.Assert(helper != null, "_IOCompletionCallback cannot be null");
            helper._ioCompletionCallback(helper._errorCode, helper._numBytes, helper._pNativeOverlapped);
        }

        // call back helper
        internal static void PerformIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pNativeOverlapped)
        {
            do
            {
                OverlappedData overlapped = OverlappedData.GetOverlappedFromNative(pNativeOverlapped);

                if (overlapped._callback is IOCompletionCallback iocb)
                {
                    // We got here because of UnsafePack (or) Pack with EC flow suppressed
                    iocb(errorCode, numBytes, pNativeOverlapped);
                }
                else
                {
                    // We got here because of Pack
                    var helper = (_IOCompletionCallback?)overlapped._callback;
                    Debug.Assert(helper != null, "Should only be receiving a completion callback if a delegate was provided.");
                    helper._errorCode = errorCode;
                    helper._numBytes = numBytes;
                    helper._pNativeOverlapped = pNativeOverlapped;
                    ExecutionContext.RunInternal(helper._executionContext, _ccb, helper);
                }

                // Quickly check the VM again, to see if a packet has arrived.
                OverlappedData.CheckVMForIOPacket(out pNativeOverlapped, out errorCode, out numBytes);
            } while (pNativeOverlapped != null);
        }
    }

    #endregion class _IOCompletionCallback

    #region class OverlappedData

    internal sealed unsafe class OverlappedData
    {
        // ! If you make any change to the layout here, you need to make matching change
        // ! to OverlappedDataObject in vm\nativeoverlapped.h
        internal IAsyncResult? _asyncResult;
        internal object? _callback; // IOCompletionCallback or _IOCompletionCallback
        internal readonly Overlapped _overlapped;
        private object? _userObject;
        private readonly NativeOverlapped* _pNativeOverlapped;
        private IntPtr _eventHandle;
        private int _offsetLow;
        private int _offsetHigh;

        internal OverlappedData(Overlapped overlapped) => _overlapped = overlapped;

        internal ref int OffsetLow => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->OffsetLow : ref _offsetLow;
        internal ref int OffsetHigh => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->OffsetHigh : ref _offsetHigh;
        internal ref IntPtr EventHandle => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->EventHandle : ref _eventHandle;

        internal NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
        {
            if (_pNativeOverlapped != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
            }

            if (iocb != null)
            {
                ExecutionContext? ec = ExecutionContext.Capture();
                _callback = (ec != null && !ec.IsDefault) ? new _IOCompletionCallback(iocb, ec) : (object)iocb;
            }
            else
            {
                _callback = null;
            }
            _userObject = userData;
            return AllocateNativeOverlapped();
        }

        internal NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
        {
            if (_pNativeOverlapped != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
            }
            _userObject = userData;
            _callback = iocb;
            return AllocateNativeOverlapped();
        }

        internal bool IsUserObject(byte[]? buffer) => ReferenceEquals(_userObject, buffer);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern NativeOverlapped* AllocateNativeOverlapped();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void FreeNativeOverlapped(NativeOverlapped* nativeOverlappedPtr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern OverlappedData GetOverlappedFromNative(NativeOverlapped* nativeOverlappedPtr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void CheckVMForIOPacket(out NativeOverlapped* pNativeOverlapped, out uint errorCode, out uint numBytes);
    }

    #endregion class OverlappedData

    #region class Overlapped

    public class Overlapped
    {
        private OverlappedData? _overlappedData;

        public Overlapped()
        {
            // The split between Overlapped and OverlappedData should not be needed. It is required by the implementation of
            // async GC handles currently. It expects OverlappedData to be a sealed type.
            _overlappedData = new OverlappedData(this);
        }

        public Overlapped(int offsetLo, int offsetHi, IntPtr hEvent, IAsyncResult? ar) : this()
        {
            Debug.Assert(_overlappedData != null, "Initialized in delegated ctor");
            _overlappedData.OffsetLow = offsetLo;
            _overlappedData.OffsetHigh = offsetHi;
            _overlappedData.EventHandle = hEvent;
            _overlappedData._asyncResult = ar;
        }

        [Obsolete("This constructor is not 64-bit compatible.  Use the constructor that takes an IntPtr for the event handle.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult? ar) : this(offsetLo, offsetHi, new IntPtr(hEvent), ar)
        {
        }

        public IAsyncResult? AsyncResult
        {
            get => _overlappedData!._asyncResult;
            set => _overlappedData!._asyncResult = value;
        }

        public int OffsetLow
        {
            get => _overlappedData!.OffsetLow;
            set => _overlappedData!.OffsetLow = value;
        }

        public int OffsetHigh
        {
            get => _overlappedData!.OffsetHigh;
            set => _overlappedData!.OffsetHigh = value;
        }

        [Obsolete("This property is not 64-bit compatible.  Use EventHandleIntPtr instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventHandle
        {
            get => EventHandleIntPtr.ToInt32();
            set => EventHandleIntPtr = new IntPtr(value);
        }

        public IntPtr EventHandleIntPtr
        {
            get => _overlappedData!.EventHandle;
            set => _overlappedData!.EventHandle = value;
        }

        /*====================================================================
        *  Packs a managed overlapped class into native Overlapped struct.
        *  Roots the iocb and stores it in the ReservedCOR field of native Overlapped
        *  Pins the native Overlapped struct and returns the pinned index.
        ====================================================================*/
        [Obsolete("This method is not safe.  Use Pack (iocb, userData) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb)
        {
            return Pack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
        {
            return _overlappedData!.Pack(iocb, userData);
        }

        [Obsolete("This method is not safe.  Use UnsafePack (iocb, userData) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb)
        {
            return UnsafePack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
        {
            return _overlappedData!.UnsafePack(iocb, userData);
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

            return OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped;
        }

        [CLSCompliant(false)]
        public static unsafe void Free(NativeOverlapped* nativeOverlappedPtr)
        {
            if (nativeOverlappedPtr == null)
                throw new ArgumentNullException(nameof(nativeOverlappedPtr));

            OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped._overlappedData = null;
            OverlappedData.FreeNativeOverlapped(nativeOverlappedPtr);
        }

        internal bool IsUserObject(byte[]? buffer) => _overlappedData!.IsUserObject(buffer);
    }

    #endregion class Overlapped
}
