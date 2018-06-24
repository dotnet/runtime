// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
#if BIT64
    using nint = System.Int64;
#else
    using nint = System.Int32;
#endif

    // These are the types of handles used by the EE.  
    // IMPORTANT: These must match the definitions in ObjectHandle.h in the EE. 
    // IMPORTANT: If new values are added to the enum the GCHandle::MaxHandleType
    //            constant must be updated.
    public enum GCHandleType
    {
        Weak = 0,
        WeakTrackResurrection = 1,
        Normal = 2,
        Pinned = 3
    }

    // This class allows you to create an opaque, GC handle to any 
    // COM+ object. A GC handle is used when an object reference must be
    // reachable from unmanaged memory.  There are 3 kinds of roots:
    // Normal - keeps the object from being collected.
    // Weak - allows object to be collected and handle contents will be zeroed.
    //          Weak references are zeroed before the finalizer runs, so if the
    //          object is resurrected in the finalizer the weak reference is
    //          still zeroed.
    // WeakTrackResurrection - Same as weak, but stays until after object is
    //          really gone.
    // Pinned - same as normal, but allows the address of the actual object
    //          to be taken.
    //

    [StructLayout(LayoutKind.Sequential)]
    public struct GCHandle
    {
        // IMPORTANT: This must be kept in sync with the GCHandleType enum.
        private const GCHandleType MaxHandleType = GCHandleType.Pinned;

#if MDA_SUPPORTED
        static GCHandle()
        {
            s_probeIsActive = Mda.IsInvalidGCHandleCookieProbeEnabled();
            if (s_probeIsActive)
                s_cookieTable = new GCHandleCookieTable();
        }
#endif

        // Allocate a handle storing the object and the type.
        internal GCHandle(object value, GCHandleType type)
        {
            // Make sure the type parameter is within the valid range for the enum.
            if ((uint)type > (uint)MaxHandleType)
                ThrowArgumentOutOfRangeException_ArgumentOutOfRange_Enum();

            IntPtr handle = InternalAlloc(value, type);

            if (type == GCHandleType.Pinned)
            {
                // Record if the handle is pinned.
                handle = (IntPtr)((nint)handle | 1);
            }

            m_handle = handle;
        }

        // Used in the conversion functions below.
        internal GCHandle(IntPtr handle)
        {
            m_handle = handle;
        }

        // Creates a new GC handle for an object.
        //
        // value - The object that the GC handle is created for.
        // type - The type of GC handle to create.
        // 
        // returns a new GC handle that protects the object.
        public static GCHandle Alloc(object value)
        {
            return new GCHandle(value, GCHandleType.Normal);
        }

        public static GCHandle Alloc(object value, GCHandleType type)
        {
            return new GCHandle(value, type);
        }

        // Frees a GC handle.
        public void Free()
        {
            // Free the handle if it hasn't already been freed.
            IntPtr handle = Interlocked.Exchange(ref m_handle, IntPtr.Zero);
            ValidateHandle(handle);
#if MDA_SUPPORTED
            // If this handle was passed out to unmanaged code, we need to remove it
            // from the cookie table.
            // NOTE: the entry in the cookie table must be released before the
            // internal handle is freed to prevent a race with reusing GC handles.
            if (s_probeIsActive)
                s_cookieTable.RemoveHandleIfPresent(handle);
#endif
            InternalFree(GetHandleValue(handle));
        }

        // Target property - allows getting / updating of the handle's referent.
        public object Target
        {
            get
            {
                ValidateHandle();
                return InternalGet(GetHandleValue());
            }

            set
            {
                ValidateHandle();
                InternalSet(GetHandleValue(), value, IsPinned());
            }
        }

        // Retrieve the address of an object in a Pinned handle.  This throws
        // an exception if the handle is any type other than Pinned.
        public IntPtr AddrOfPinnedObject()
        {
            // Check if the handle was not a pinned handle.
            if (!IsPinned())
            {
                ValidateHandle();

                // You can only get the address of pinned handles.
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotPinned);
            }

            // Get the address.
            return InternalAddrOfPinnedObject(GetHandleValue());
        }

        // Determine whether this handle has been allocated or not.
        public bool IsAllocated => m_handle != IntPtr.Zero;

        // Used to create a GCHandle from an int.  This is intended to
        // be used with the reverse conversion.
        public static explicit operator GCHandle(IntPtr value)
        {
            ValidateHandle(value);
            return new GCHandle(value);
        }

        public static GCHandle FromIntPtr(IntPtr value)
        {
            ValidateHandle(value);

#if MDA_SUPPORTED
            IntPtr handle = value;
            if (s_probeIsActive)
            {
                // Make sure this cookie matches up with a GCHandle we've passed out a cookie for.
                handle = s_cookieTable.GetHandle(value);
                if (IntPtr.Zero == handle)
                {
                    // Fire an MDA if we were unable to retrieve the GCHandle.
                    Mda.FireInvalidGCHandleCookieProbe(value);
                    return new GCHandle(IntPtr.Zero);
                }
                return new GCHandle(handle);
            }
#endif
            return new GCHandle(value);
        }

        // Used to get the internal integer representation of the handle out.
        public static explicit operator IntPtr(GCHandle value)
        {
            return ToIntPtr(value);
        }

        public static IntPtr ToIntPtr(GCHandle value)
        {
#if MDA_SUPPORTED
            if (s_probeIsActive)
            {
                // Remember that we passed this GCHandle out by storing the cookie we returned so we
                //  can later validate.
                return s_cookieTable.FindOrAddHandle(value.m_handle);
            }
#endif
            return value.m_handle;
        }

        public override int GetHashCode()
        {
            return m_handle.GetHashCode();
        }

        public override bool Equals(object o)
        {
            GCHandle hnd;

            // Check that o is a GCHandle first
            if (o == null || !(o is GCHandle))
                return false;
            else
                hnd = (GCHandle)o;

            return m_handle == hnd.m_handle;
        }

        public static bool operator ==(GCHandle a, GCHandle b)
        {
            return a.m_handle == b.m_handle;
        }

        public static bool operator !=(GCHandle a, GCHandle b)
        {
            return a.m_handle != b.m_handle;
        }

        internal IntPtr GetHandleValue()
        {
            return GetHandleValue(m_handle);
        }

        private static IntPtr GetHandleValue(IntPtr handle)
        {
            // Remove Pin flag
            return new IntPtr((nint)handle & ~(nint)1);
        }

        internal bool IsPinned()
        {
            // Check Pin flag
            return ((nint)m_handle & 1) != 0;
        }

        // Internal native calls that this implementation uses.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalAlloc(object value, GCHandleType type);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalFree(IntPtr handle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InternalGet(IntPtr handle);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(IntPtr handle, object value, bool isPinned);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object InternalCompareExchange(IntPtr handle, object value, object oldValue, bool isPinned);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalAddrOfPinnedObject(IntPtr handle);

        // The actual integer handle value that the EE uses internally.
        private IntPtr m_handle;

#if MDA_SUPPORTED
        // The GCHandle cookie table.
        private static volatile GCHandleCookieTable s_cookieTable = null;
        private static volatile bool s_probeIsActive = false;
#endif

        private void ValidateHandle()
        {
            // Check if the handle was never initialized or was freed.
            if (m_handle == IntPtr.Zero)
                ThrowInvalidOperationException_HandleIsNotInitialized();
        }

        private static void ValidateHandle(IntPtr handle)
        {
            // Check if the handle was never initialized or was freed.
            if (handle == IntPtr.Zero)
                ThrowInvalidOperationException_HandleIsNotInitialized();
        }

        private static void ThrowArgumentOutOfRangeException_ArgumentOutOfRange_Enum()
        {
            throw ThrowHelper.GetArgumentOutOfRangeException(ExceptionArgument.type, ExceptionResource.ArgumentOutOfRange_Enum);
        }

        private static void ThrowInvalidOperationException_HandleIsNotInitialized()
        {
            throw ThrowHelper.GetInvalidOperationException(ExceptionResource.InvalidOperation_HandleIsNotInitialized);
        }
    }
}
