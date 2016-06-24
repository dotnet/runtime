// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{    
    using System;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // These are the types of handles used by the EE.  
    // IMPORTANT: These must match the definitions in ObjectHandle.h in the EE. 
    // IMPORTANT: If new values are added to the enum the GCHandle::MaxHandleType
    //            constant must be updated.
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
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
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct GCHandle
    {
        // IMPORTANT: This must be kept in sync with the GCHandleType enum.
        private const GCHandleType MaxHandleType = GCHandleType.Pinned;

#if MDA_SUPPORTED
        [System.Security.SecuritySafeCritical]  // auto-generated
        static GCHandle()
        {
            s_probeIsActive = Mda.IsInvalidGCHandleCookieProbeEnabled();
            if (s_probeIsActive)
                s_cookieTable = new GCHandleCookieTable();
        }
#endif

        // Allocate a handle storing the object and the type.
        [System.Security.SecurityCritical]  // auto-generated
        internal GCHandle(Object value, GCHandleType type)
        {
            // Make sure the type parameter is within the valid range for the enum.
            if ((uint)type > (uint)MaxHandleType)
                throw new ArgumentOutOfRangeException("type", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();

            m_handle = InternalAlloc(value, type);

            // Record if the handle is pinned.
            if (type == GCHandleType.Pinned)
                SetIsPinned();
        }  

        // Used in the conversion functions below.
        [System.Security.SecurityCritical]  // auto-generated
        internal GCHandle(IntPtr handle)
        {
            InternalCheckDomain(handle);
            m_handle = handle;
        }

        // Creates a new GC handle for an object.
        //
        // value - The object that the GC handle is created for.
        // type - The type of GC handle to create.
        // 
        // returns a new GC handle that protects the object.
        [System.Security.SecurityCritical]  // auto-generated_required
        public static GCHandle Alloc(Object value)
        {
            return new GCHandle(value, GCHandleType.Normal);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static GCHandle Alloc(Object value, GCHandleType type)
        {
            return new GCHandle(value, type);
        }


        // Frees a GC handle.
        [System.Security.SecurityCritical]  // auto-generated_required
        public void Free()
        {
            // Copy the handle instance member to a local variable. This is required to prevent
            // race conditions releasing the handle.
            IntPtr handle = m_handle;

            // Free the handle if it hasn't already been freed.
            if (handle != IntPtr.Zero && Interlocked.CompareExchange(ref m_handle, IntPtr.Zero, handle) == handle)
            {
#if MDA_SUPPORTED
                // If this handle was passed out to unmanaged code, we need to remove it
                // from the cookie table.
                // NOTE: the entry in the cookie table must be released before the
                // internal handle is freed to prevent a race with reusing GC handles.
                if (s_probeIsActive)
                    s_cookieTable.RemoveHandleIfPresent(handle);
#endif

#if BIT64
                InternalFree((IntPtr)(((long)handle) & ~1L));
#else // BIT64 (32)
                InternalFree((IntPtr)(((int)handle) & ~1));
#endif
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
            }
        }
        
        // Target property - allows getting / updating of the handle's referent.
        public Object Target
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            get
            {
                // Check if the handle was never initialized or was freed.
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));

                return InternalGet(GetHandleValue());
            }
    
            [System.Security.SecurityCritical]  // auto-generated_required
            set
            {
                // Check if the handle was never initialized or was freed.
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));

                InternalSet(GetHandleValue(), value, IsPinned());
            }
        }
        
        // Retrieve the address of an object in a Pinned handle.  This throws
        // an exception if the handle is any type other than Pinned.
        [System.Security.SecurityCritical]  // auto-generated_required
        public IntPtr AddrOfPinnedObject()
        {
            // Check if the handle was not a pinned handle.
            if (!IsPinned())
            {
                // Check if the handle was never initialized for was freed.
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));

                // You can only get the address of pinned handles.
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotPinned"));
            }

            // Get the address.
            return InternalAddrOfPinnedObject(GetHandleValue());
        }

        // Determine whether this handle has been allocated or not.
        public bool IsAllocated
        {
            get
            {
                return m_handle != IntPtr.Zero;
            }
        }

        // Used to create a GCHandle from an int.  This is intended to
        // be used with the reverse conversion.
        [System.Security.SecurityCritical]  // auto-generated_required
        public static explicit operator GCHandle(IntPtr value)
        {
            return FromIntPtr(value);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public static GCHandle FromIntPtr(IntPtr value)
        {
            if (value == IntPtr.Zero)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
            Contract.EndContractBlock();

            IntPtr handle = value;
            
#if MDA_SUPPORTED
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
            }
#endif

            return new GCHandle(handle);
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

        public override bool Equals(Object o)
        {
            GCHandle hnd;
            
            // Check that o is a GCHandle first
            if(o == null || !(o is GCHandle))
                return false;
            else 
                hnd = (GCHandle) o;

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
#if BIT64
            return new IntPtr(((long)m_handle) & ~1L);
#else // !BIT64 (32)
            return new IntPtr(((int)m_handle) & ~1);
#endif
        }

        internal bool IsPinned()
        {
#if BIT64
            return (((long)m_handle) & 1) != 0;
#else // !BIT64 (32)
            return (((int)m_handle) & 1) != 0;
#endif
        }

        internal void SetIsPinned()
        {
#if BIT64
            m_handle = new IntPtr(((long)m_handle) | 1L);
#else // !BIT64 (32)
            m_handle = new IntPtr(((int)m_handle) | 1);
#endif
        }

        // Internal native calls that this implementation uses.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalAlloc(Object value, GCHandleType type);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalFree(IntPtr handle);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object InternalGet(IntPtr handle);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(IntPtr handle, Object value, bool isPinned);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Object InternalCompareExchange(IntPtr handle, Object value, Object oldValue, bool isPinned);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalAddrOfPinnedObject(IntPtr handle);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void InternalCheckDomain(IntPtr handle);
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern GCHandleType InternalGetHandleType(IntPtr handle);

        // The actual integer handle value that the EE uses internally.
        private IntPtr m_handle;

#if MDA_SUPPORTED
        // The GCHandle cookie table.
        static private volatile GCHandleCookieTable s_cookieTable = null;
        static private volatile bool s_probeIsActive = false;
#endif
    }
}
