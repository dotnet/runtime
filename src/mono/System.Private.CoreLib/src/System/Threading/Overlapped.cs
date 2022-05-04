// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace System.Threading
{
    #region class OverlappedData

    internal sealed unsafe class OverlappedData
    {
        internal IAsyncResult? _asyncResult;
        internal object? _callback; // IOCompletionCallback or _IOCompletionCallback
        internal Overlapped? _overlapped;
        private object? _userObject;
        private NativeOverlapped* _pNativeOverlapped;
        private IntPtr _eventHandle;
        private int _offsetLow;
        private int _offsetHigh;
        private GCHandle[]? _pinnedData;

        internal ref IAsyncResult? AsyncResult => ref _asyncResult;

        internal ref int OffsetLow => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->OffsetLow : ref _offsetLow;
        internal ref int OffsetHigh => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->OffsetHigh : ref _offsetHigh;
        internal ref IntPtr EventHandle => ref (_pNativeOverlapped != null) ? ref _pNativeOverlapped->EventHandle : ref _eventHandle;

        internal unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
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

        internal unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
        {
            if (_pNativeOverlapped != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
            }
            _userObject = userData;
            _callback = iocb;
            return AllocateNativeOverlapped();
        }

        private unsafe NativeOverlapped* AllocateNativeOverlapped()
        {
            Debug.Assert(_pinnedData == null);

            bool success = false;
            try
            {
                if (_userObject != null)
                {
                    if (_userObject.GetType() == typeof(object[]))
                    {
                        object[] objArray = (object[])_userObject;

                        _pinnedData = new GCHandle[objArray.Length];
                        for (int i = 0; i < objArray.Length; i++)
                        {
                            _pinnedData[i] = GCHandle.Alloc(objArray[i], GCHandleType.Pinned);
                        }
                    }
                    else
                    {
                        _pinnedData = new GCHandle[1];
                        _pinnedData[0] = GCHandle.Alloc(_userObject, GCHandleType.Pinned);
                    }
                }

                //CORERT: NativeOverlapped* pNativeOverlapped = (NativeOverlapped*)Interop.MemAlloc((UIntPtr)(sizeof(NativeOverlapped) + sizeof(GCHandle)));
                NativeOverlapped* pNativeOverlapped = (NativeOverlapped*)Marshal.AllocHGlobal((IntPtr)(sizeof(NativeOverlapped) + sizeof(GCHandle)));

                *(GCHandle*)(pNativeOverlapped + 1) = default;
                _pNativeOverlapped = pNativeOverlapped;

                _pNativeOverlapped->InternalLow = default;
                _pNativeOverlapped->InternalHigh = default;
                _pNativeOverlapped->OffsetLow = _offsetLow;
                _pNativeOverlapped->OffsetHigh = _offsetHigh;
                _pNativeOverlapped->EventHandle = _eventHandle;

                *(GCHandle*)(_pNativeOverlapped + 1) = GCHandle.Alloc(this);

                success = true;
#if FEATURE_PERFTRACING
                if (NativeRuntimeEventSource.Log.IsEnabled())
                    NativeRuntimeEventSource.Log.ThreadPoolIOPack(pNativeOverlapped);
#endif
                return _pNativeOverlapped;
            }
            finally
            {
                if (!success)
                    FreeNativeOverlapped();
            }
        }

        internal static unsafe void FreeNativeOverlapped(NativeOverlapped* nativeOverlappedPtr)
        {
            OverlappedData overlappedData = GetOverlappedFromNative(nativeOverlappedPtr);
            overlappedData.FreeNativeOverlapped();
        }

        private void FreeNativeOverlapped()
        {
            if (_pinnedData != null)
            {
                for (int i = 0; i < _pinnedData.Length; i++)
                {
                    if (_pinnedData[i].IsAllocated)
                    {
                        _pinnedData[i].Free();
                    }
                }
                _pinnedData = null;
            }

            if (_pNativeOverlapped != null)
            {
                GCHandle handle = *(GCHandle*)(_pNativeOverlapped + 1);
                if (handle.IsAllocated)
                    handle.Free();

                Marshal.FreeHGlobal((IntPtr)_pNativeOverlapped);
                //CORERT: Interop.MemFree((IntPtr)_pNativeOverlapped);
                _pNativeOverlapped = null;
            }
        }

        internal static unsafe OverlappedData GetOverlappedFromNative(NativeOverlapped* pNativeOverlapped)
        {
            GCHandle handle = *(GCHandle*)(pNativeOverlapped + 1);
            return (OverlappedData)handle.Target!;
        }
    }

    #endregion class OverlappedData

    #region class Overlapped

    public class Overlapped
    {
        private OverlappedData _overlappedData;

        public Overlapped()
        {
            _overlappedData = new OverlappedData();
            _overlappedData._overlapped = this;
        }

        public Overlapped(int offsetLo, int offsetHi, IntPtr hEvent, IAsyncResult? ar) : this()
        {
            _overlappedData.OffsetLow = offsetLo;
            _overlappedData.OffsetHigh = offsetHi;
            _overlappedData.EventHandle = hEvent;
            _overlappedData.AsyncResult = ar;
        }

        [Obsolete("This constructor is not 64-bit compatible and has been deprecated. Use the constructor that accepts an IntPtr for the event handle instead.")]
        public Overlapped(int offsetLo, int offsetHi, int hEvent, IAsyncResult? ar) : this(offsetLo, offsetHi, new IntPtr(hEvent), ar)
        {
        }

        public IAsyncResult? AsyncResult
        {
            get { return _overlappedData.AsyncResult; }
            set { _overlappedData.AsyncResult = value; }
        }

        public int OffsetLow
        {
            get { return _overlappedData.OffsetLow; }
            set { _overlappedData.OffsetLow = value; }
        }

        public int OffsetHigh
        {
            get { return _overlappedData.OffsetHigh; }
            set { _overlappedData.OffsetHigh = value; }
        }

        [Obsolete("Overlapped.EventHandle is not 64-bit compatible and has been deprecated. Use EventHandleIntPtr instead.")]
        public int EventHandle
        {
            get { return EventHandleIntPtr.ToInt32(); }
            set { EventHandleIntPtr = new IntPtr(value); }
        }

        public IntPtr EventHandleIntPtr
        {
            get { return _overlappedData.EventHandle; }
            set { _overlappedData.EventHandle = value; }
        }

        /*====================================================================
        *  Packs a managed overlapped class into native Overlapped struct.
        *  Roots the iocb and stores it in the ReservedCOR field of native Overlapped
        *  Pins the native Overlapped struct and returns the pinned index.
        ====================================================================*/
        [Obsolete("This overload is not safe and has been deprecated. Use Pack(IOCompletionCallback?, object?) instead.")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb)
        {
            return Pack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* Pack(IOCompletionCallback? iocb, object? userData)
        {
            return _overlappedData.Pack(iocb, userData);
        }

        [Obsolete("This overload is not safe and has been deprecated. Use UnsafePack(IOCompletionCallback?, object?) instead.")]
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb)
        {
            return UnsafePack(iocb, null);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafePack(IOCompletionCallback? iocb, object? userData)
        {
            return _overlappedData.UnsafePack(iocb, userData);
        }

        /*====================================================================
        *  Unpacks an unmanaged native Overlapped struct.
        *  Unpins the native Overlapped struct
        ====================================================================*/
        [CLSCompliant(false)]
        public static unsafe Overlapped Unpack(NativeOverlapped* nativeOverlappedPtr)
        {
            ArgumentNullException.ThrowIfNull(nativeOverlappedPtr);

            return OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped!;
        }

        [CLSCompliant(false)]
        public static unsafe void Free(NativeOverlapped* nativeOverlappedPtr)
        {
            ArgumentNullException.ThrowIfNull(nativeOverlappedPtr);

            OverlappedData.GetOverlappedFromNative(nativeOverlappedPtr)._overlapped!._overlappedData = null!;
            OverlappedData.FreeNativeOverlapped(nativeOverlappedPtr);
        }
    }

    #endregion class Overlapped
}
