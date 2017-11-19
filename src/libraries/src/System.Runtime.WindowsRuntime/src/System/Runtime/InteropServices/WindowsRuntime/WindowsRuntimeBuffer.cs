// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    /// <summary>
    /// Contains an implementation of the WinRT IBuffer interface that conforms to all requirements on classes that implement that interface,
    /// such as implementing additional interfaces.
    /// </summary>
    public sealed class WindowsRuntimeBuffer : IBuffer, IBufferByteAccess, IMarshal, IAgileObject
    {
        #region Constants

        private const String WinTypesDLL = "WinTypes.dll";

        private enum MSHCTX : int { Local = 0, NoSharedMem = 1, DifferentMachine = 2, InProc = 3, CrossCtx = 4 }
        private enum MSHLFLAGS : int { Normal = 0, TableStrong = 1, TableWeak = 2, NoPing = 4 }


        #endregion Constants


        #region Static factory methods

        [CLSCompliant(false)]
        public static IBuffer Create(Int32 capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            Contract.Ensures(Contract.Result<IBuffer>() != null);
            Contract.Ensures(Contract.Result<IBuffer>().Length == unchecked((UInt32)0));
            Contract.Ensures(Contract.Result<IBuffer>().Capacity == unchecked((UInt32)capacity));
            Contract.EndContractBlock();

            return new WindowsRuntimeBuffer(capacity);
        }


        [CLSCompliant(false)]
        public static IBuffer Create(Byte[] data, Int32 offset, Int32 length, Int32 capacity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (data.Length - offset < length) throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
            if (data.Length - offset < capacity) throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
            if (capacity < length) throw new ArgumentException(SR.Argument_InsufficientBufferCapacity);

            Contract.Ensures(Contract.Result<IBuffer>() != null);
            Contract.Ensures(Contract.Result<IBuffer>().Length == unchecked((UInt32)length));
            Contract.Ensures(Contract.Result<IBuffer>().Capacity == unchecked((UInt32)capacity));

            Contract.EndContractBlock();

            Byte[] underlyingData = new Byte[capacity];
            Buffer.BlockCopy(data, offset, underlyingData, 0, length);
            return new WindowsRuntimeBuffer(underlyingData, 0, length, capacity);
        }

        #endregion Static factory methods


        #region Static fields and helpers

        // This object handles IMarshal calls for us:
        [ThreadStatic]
        private static IMarshal t_winRtMarshalProxy = null;

        private static void EnsureHasMarshalProxy()
        {
            if (t_winRtMarshalProxy != null)
                return;

            try
            {
                IMarshal proxy;
                Int32 hr = Interop.mincore.RoGetBufferMarshaler(out proxy);
                t_winRtMarshalProxy = proxy;

                if (hr != __HResults.S_OK)
                {
                    Exception ex = new Exception(String.Format("{0} ({1}!RoGetBufferMarshaler)", SR.WinRtCOM_Error, WinTypesDLL));
                    ex.SetErrorCode(hr);
                    throw ex;
                }

                if (proxy == null)
                    throw new NullReferenceException(String.Format("{0} ({1}!RoGetBufferMarshaler)", SR.WinRtCOM_Error, WinTypesDLL));
            }
            catch (DllNotFoundException ex)
            {
                throw new NotImplementedException(SR.Format(SR.NotImplemented_NativeRoutineNotFound,
                                                               String.Format("{0}!RoGetBufferMarshaler", WinTypesDLL)),
                                                  ex);
            }
        }

        #endregion Static fields and helpers


        #region Fields

        private Byte[] _data = null;
        private Int32 _dataStartOffs = 0;
        private Int32 _usefulDataLength = 0;
        private Int32 _maxDataCapacity = 0;
        private GCHandle _pinHandle;

        // Pointer to data[dataStartOffs] when data is pinned:
        private IntPtr _dataPtr = IntPtr.Zero;

        #endregion Fields


        #region Constructors

        internal WindowsRuntimeBuffer(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Contract.EndContractBlock();

            _data = new Byte[capacity];
            _dataStartOffs = 0;
            _usefulDataLength = 0;
            _maxDataCapacity = capacity;
            _dataPtr = IntPtr.Zero;
        }


        internal WindowsRuntimeBuffer(Byte[] data, Int32 offset, Int32 length, Int32 capacity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (data.Length - offset < length) throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
            if (data.Length - offset < capacity) throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
            if (capacity < length) throw new ArgumentException(SR.Argument_InsufficientBufferCapacity);
            Contract.EndContractBlock();

            _data = data;
            _dataStartOffs = offset;
            _usefulDataLength = length;
            _maxDataCapacity = capacity;
            _dataPtr = IntPtr.Zero;
        }

        #endregion Constructors


        #region Helpers

        internal void GetUnderlyingData(out Byte[] underlyingDataArray, out Int32 underlyingDataArrayStartOffset)
        {
            underlyingDataArray = _data;
            underlyingDataArrayStartOffset = _dataStartOffs;
        }


        private unsafe Byte* PinUnderlyingData()
        {
            GCHandle gcHandle = default(GCHandle);
            bool ptrWasStored = false;
            IntPtr buffPtr;

            try { }
            finally
            {
                try
                {
                    // Pin the data array:
                    gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                    buffPtr = gcHandle.AddrOfPinnedObject() + _dataStartOffs;

                    // Store the pin IFF it has not been assigned:
                    ptrWasStored = (Interlocked.CompareExchange(ref _dataPtr, buffPtr, IntPtr.Zero) == IntPtr.Zero);
                }
                finally
                {
                    if (!ptrWasStored)
                    {
                        // There is a race with another thread also trying to create a pin and they were first
                        // in assigning to data pin. That's ok, just give it up.
                        // Unpin again (the pin from the other thread remains):
                        gcHandle.Free();
                    }
                    else
                    {
                        if (_pinHandle.IsAllocated)
                            _pinHandle.Free();

                        // Make sure we keep track of the handle
                        _pinHandle = gcHandle;
                    }
                }
            }

            // Ok, now all is good:
            return (Byte*)buffPtr;
        }

        ~WindowsRuntimeBuffer()
        {
            if (_pinHandle.IsAllocated)
                _pinHandle.Free();
        }

        #endregion Helpers


        #region Implementation of Windows.Foundation.IBuffer

        UInt32 IBuffer.Capacity
        {
            get { return unchecked((UInt32)_maxDataCapacity); }
        }


        UInt32 IBuffer.Length
        {
            get
            {
                return unchecked((UInt32)_usefulDataLength);
            }

            set
            {
                if (value > ((IBuffer)this).Capacity)
                {
                    ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(value), SR.Argument_BufferLengthExceedsCapacity);
                    ex.SetErrorCode(__HResults.E_BOUNDS);
                    throw ex;
                }

                // Capacity is ensured to not exceed Int32.MaxValue, so Length is within this limit and this cast is safe:
                Debug.Assert(((IBuffer)this).Capacity <= Int32.MaxValue);
                _usefulDataLength = unchecked((Int32)value);
            }
        }

        #endregion Implementation of Windows.Foundation.IBuffer


        #region Implementation of IBufferByteAccess

        unsafe IntPtr IBufferByteAccess.GetBuffer()
        {
            // Get pin handle:
            IntPtr buffPtr = Volatile.Read(ref _dataPtr);

            // If we are already pinned, return the pointer and have a nice day:
            if (buffPtr != IntPtr.Zero)
                return buffPtr;

            // Ok, we we are not yet pinned. Let's do it.
            return new IntPtr(PinUnderlyingData());
        }

        #endregion Implementation of IBufferByteAccess


        #region Implementation of IMarshal

        void IMarshal.DisconnectObject(UInt32 dwReserved)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.DisconnectObject(dwReserved);
        }


        void IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlflags, out UInt32 pSize)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.GetMarshalSizeMax(ref riid, pv, dwDestContext, pvDestContext, mshlflags, out pSize);
        }


        void IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlFlags, out Guid pCid)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.GetUnmarshalClass(ref riid, pv, dwDestContext, pvDestContext, mshlFlags, out pCid);
        }


        void IMarshal.MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, UInt32 dwDestContext, IntPtr pvDestContext, UInt32 mshlflags)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.MarshalInterface(pStm, ref riid, pv, dwDestContext, pvDestContext, mshlflags);
        }


        void IMarshal.ReleaseMarshalData(IntPtr pStm)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.ReleaseMarshalData(pStm);
        }


        void IMarshal.UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
        {
            EnsureHasMarshalProxy();
            t_winRtMarshalProxy.UnmarshalInterface(pStm, ref riid, out ppv);
        }
        #endregion Implementation of IMarshal

    }  // class WindowsRuntimeBuffer
}  // namespace

// WindowsRuntimeBuffer.cs
