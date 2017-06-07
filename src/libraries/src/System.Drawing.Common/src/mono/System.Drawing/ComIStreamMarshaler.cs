// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.ComIStreamMarshaler.cs
//
// Author:
//   Kornél Pál <http://www.kornelpal.hu/>
//
// Copyright (C) 2005-2006 Kornél Pál
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Undefine to debug the protected blocks
#define MAP_EX_TO_HR

// Define to debug wrappers recursively
// #define RECURSIVE_WRAPPING

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace System.Drawing
{
    // Mono does not implement COM interface marshaling
    // This custom marshaler should be replaced with UnmanagedType.Interface
    // Provides identical behaviour under Mono and .NET Framework
    internal sealed class ComIStreamMarshaler : ICustomMarshaler
    {
        private const int S_OK = 0x00000000;
        private const int E_NOINTERFACE = unchecked((int)0x80004002);

        private delegate int QueryInterfaceDelegate(IntPtr @this, [In()] ref Guid riid, IntPtr ppvObject);
        private delegate int AddRefDelegate(IntPtr @this);
        private delegate int ReleaseDelegate(IntPtr @this);
        private delegate int ReadDelegate(IntPtr @this, [Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pv, int cb, IntPtr pcbRead);
        private delegate int WriteDelegate(IntPtr @this, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pv, int cb, IntPtr pcbWritten);
        private delegate int SeekDelegate(IntPtr @this, long dlibMove, int dwOrigin, IntPtr plibNewPosition);
        private delegate int SetSizeDelegate(IntPtr @this, long libNewSize);
        private delegate int CopyToDelegate(IntPtr @this, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);
        private delegate int CommitDelegate(IntPtr @this, int grfCommitFlags);
        private delegate int RevertDelegate(IntPtr @this);
        private delegate int LockRegionDelegate(IntPtr @this, long libOffset, long cb, int dwLockType);
        private delegate int UnlockRegionDelegate(IntPtr @this, long libOffset, long cb, int dwLockType);
        private delegate int StatDelegate(IntPtr @this, out STATSTG pstatstg, int grfStatFlag);
        private delegate int CloneDelegate(IntPtr @this, out IntPtr ppstm);

        [StructLayout(LayoutKind.Sequential)]
        private sealed class IStreamInterface
        {
            internal IntPtr lpVtbl;
            internal IntPtr gcHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private sealed class IStreamVtbl
        {
            internal QueryInterfaceDelegate QueryInterface;
            internal AddRefDelegate AddRef;
            internal ReleaseDelegate Release;
            internal ReadDelegate Read;
            internal WriteDelegate Write;
            internal SeekDelegate Seek;
            internal SetSizeDelegate SetSize;
            internal CopyToDelegate CopyTo;
            internal CommitDelegate Commit;
            internal RevertDelegate Revert;
            internal LockRegionDelegate LockRegion;
            internal UnlockRegionDelegate UnlockRegion;
            internal StatDelegate Stat;
            internal CloneDelegate Clone;
        }

        // Managed COM Callable Wrapper implementation
        // Reference counting is thread safe
        private sealed class ManagedToNativeWrapper
        {
            // Mono does not implement Marshal.Release
            [StructLayout(LayoutKind.Sequential)]
            private sealed class ReleaseSlot
            {
                internal ReleaseDelegate Release;
            }

            private static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
            private static readonly Guid IID_IStream = new Guid("0000000C-0000-0000-C000-000000000046");
            private static readonly MethodInfo exceptionGetHResult = typeof(Exception).GetTypeInfo().GetProperty("HResult", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding, null, typeof(int), new Type[] { }, null).GetGetMethod(true);
            // Keeps delegates alive while they are marshaled
            private static readonly IStreamVtbl managedVtable;
            private static IntPtr comVtable;
            private static int vtableRefCount;

            private IStream managedInterface;
            private IntPtr comInterface;
            // Keeps the object alive when it has no managed references
            private GCHandle gcHandle;
            private int refCount = 1;

            static ManagedToNativeWrapper()
            {
                EventHandler onShutdown;
                AppDomain currentDomain;
                IStreamVtbl newVtable;

                onShutdown = new EventHandler(OnShutdown);
                currentDomain = AppDomain.CurrentDomain;
                currentDomain.DomainUnload += onShutdown;
                currentDomain.ProcessExit += onShutdown;

                newVtable = new IStreamVtbl();
                newVtable.QueryInterface = new QueryInterfaceDelegate(QueryInterface);
                newVtable.AddRef = new AddRefDelegate(AddRef);
                newVtable.Release = new ReleaseDelegate(Release);
                newVtable.Read = new ReadDelegate(Read);
                newVtable.Write = new WriteDelegate(Write);
                newVtable.Seek = new SeekDelegate(Seek);
                newVtable.SetSize = new SetSizeDelegate(SetSize);
                newVtable.CopyTo = new CopyToDelegate(CopyTo);
                newVtable.Commit = new CommitDelegate(Commit);
                newVtable.Revert = new RevertDelegate(Revert);
                newVtable.LockRegion = new LockRegionDelegate(LockRegion);
                newVtable.UnlockRegion = new UnlockRegionDelegate(UnlockRegion);
                newVtable.Stat = new StatDelegate(Stat);
                newVtable.Clone = new CloneDelegate(Clone);
                managedVtable = newVtable;

                CreateVtable();
            }

            private ManagedToNativeWrapper(IStream managedInterface)
            {
                IStreamInterface newInterface;

                lock (managedVtable)
                {
                    // Vtable may have been disposed when shutting down
                    if (vtableRefCount == 0 && comVtable == IntPtr.Zero)
                        CreateVtable();
                    vtableRefCount++;
                }

                try
                {
                    this.managedInterface = managedInterface;
                    gcHandle = GCHandle.Alloc(this);

                    newInterface = new IStreamInterface();
                    newInterface.lpVtbl = comVtable;
                    newInterface.gcHandle = (IntPtr)gcHandle;
                    comInterface = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IStreamInterface)));
                    Marshal.StructureToPtr(newInterface, comInterface, false);
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
            }

            private void Dispose()
            {
                if (gcHandle.IsAllocated)
                    gcHandle.Free();

                if (comInterface != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(comInterface);
                    comInterface = IntPtr.Zero;
                }

                managedInterface = null;

                lock (managedVtable)
                {
                    // Dispose vtable when shutting down
                    if (--vtableRefCount == 0 && Environment.HasShutdownStarted)
                        DisposeVtable();
                }
            }

            private static void OnShutdown(object sender, EventArgs e)
            {
                lock (managedVtable)
                {
                    // There may be object instances when shutting down
                    if (vtableRefCount == 0 && comVtable != IntPtr.Zero)
                        DisposeVtable();
                }
            }

            private static void CreateVtable()
            {
                comVtable = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IStreamVtbl)));
                Marshal.StructureToPtr(managedVtable, comVtable, false);
            }

            private static void DisposeVtable()
            {
                Marshal.DestroyStructure(comVtable, typeof(IStreamVtbl));
                Marshal.FreeHGlobal(comVtable);
                comVtable = IntPtr.Zero;
            }

            internal static IStream GetUnderlyingInterface(IntPtr comInterface, bool outParam)
            {
                if (Marshal.ReadIntPtr(comInterface) == comVtable)
                {
                    IStream managedInterface = GetObject(comInterface).managedInterface;

                    if (outParam)
                        Release(comInterface);

                    return managedInterface;
                }
                else
                    return null;
            }

            internal static IntPtr GetInterface(IStream managedInterface)
            {
                IntPtr comInterface;

                if (managedInterface == null)
                    return IntPtr.Zero;
#if !RECURSIVE_WRAPPING
                else if ((comInterface = NativeToManagedWrapper.GetUnderlyingInterface(managedInterface)) == IntPtr.Zero)
#endif
                    comInterface = new ManagedToNativeWrapper(managedInterface).comInterface;

                return comInterface;
            }

            internal static void ReleaseInterface(IntPtr comInterface)
            {
                if (comInterface != IntPtr.Zero)
                {
                    IntPtr vtable = Marshal.ReadIntPtr(comInterface);

                    if (vtable == comVtable)
                        Release(comInterface);
                    else
                    {
                        ReleaseSlot releaseSlot = (ReleaseSlot)Marshal.PtrToStructure((IntPtr)((long)vtable + (long)(IntPtr.Size * 2)), typeof(ReleaseSlot));
                        releaseSlot.Release(comInterface);
                    }
                }
            }

            // Mono does not implement Marshal.GetHRForException
            private static int GetHRForException(Exception e)
            {
                return (int)exceptionGetHResult.Invoke(e, null);
            }

            private static ManagedToNativeWrapper GetObject(IntPtr @this)
            {
                return (ManagedToNativeWrapper)((GCHandle)Marshal.ReadIntPtr(@this, IntPtr.Size)).Target;
            }

            private static int QueryInterface(IntPtr @this, ref Guid riid, IntPtr ppvObject)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    if (IID_IUnknown.Equals(riid) || IID_IStream.Equals(riid))
                    {
                        Marshal.WriteIntPtr(ppvObject, @this);
                        AddRef(@this);
                        return S_OK;
                    }
                    else
                    {
                        Marshal.WriteIntPtr(ppvObject, IntPtr.Zero);
                        return E_NOINTERFACE;
                    }
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int AddRef(IntPtr @this)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    ManagedToNativeWrapper thisObject = GetObject(@this);

                    lock (thisObject)
                    {
                        return ++thisObject.refCount;
                    }
#if MAP_EX_TO_HR
                }
                catch
                {
                    return 0;
                }
#endif
            }

            private static int Release(IntPtr @this)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    ManagedToNativeWrapper thisObject = GetObject(@this);

                    lock (thisObject)
                    {
                        if ((thisObject.refCount != 0) && (--thisObject.refCount == 0))
                            thisObject.Dispose();

                        return thisObject.refCount;
                    }
#if MAP_EX_TO_HR
                }
                catch
                {
                    return 0;
                }
#endif
            }

            private static int Read(IntPtr @this, byte[] pv, int cb, IntPtr pcbRead)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Read(pv, cb, pcbRead);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int Write(IntPtr @this, byte[] pv, int cb, IntPtr pcbWritten)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Write(pv, cb, pcbWritten);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int Seek(IntPtr @this, long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Seek(dlibMove, dwOrigin, plibNewPosition);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int SetSize(IntPtr @this, long libNewSize)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.SetSize(libNewSize);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int CopyTo(IntPtr @this, IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.CopyTo(pstm, cb, pcbRead, pcbWritten);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int Commit(IntPtr @this, int grfCommitFlags)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Commit(grfCommitFlags);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int Revert(IntPtr @this)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Revert();
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int LockRegion(IntPtr @this, long libOffset, long cb, int dwLockType)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.LockRegion(libOffset, cb, dwLockType);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int UnlockRegion(IntPtr @this, long libOffset, long cb, int dwLockType)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.UnlockRegion(libOffset, cb, dwLockType);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }

            private static int Stat(IntPtr @this, out STATSTG pstatstg, int grfStatFlag)
            {
#if MAP_EX_TO_HR
                try
                {
#endif
                    GetObject(@this).managedInterface.Stat(out pstatstg, grfStatFlag);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    pstatstg = new STATSTG();
                    return GetHRForException(e);
                }
#endif
            }

            private static int Clone(IntPtr @this, out IntPtr ppstm)
            {
                ppstm = IntPtr.Zero;
#if MAP_EX_TO_HR
                try
                {
#endif
                    IStream newInterface;

                    GetObject(@this).managedInterface.Clone(out newInterface);
                    ppstm = ManagedToNativeWrapper.GetInterface(newInterface);
                    return S_OK;
#if MAP_EX_TO_HR
                }
                catch (Exception e)
                {
                    return GetHRForException(e);
                }
#endif
            }
        }

        // Managed Runtime Callable Wrapper implementation
        private sealed class NativeToManagedWrapper : IStream
        {
            private IntPtr comInterface;
            private IStreamVtbl managedVtable;

            private NativeToManagedWrapper(IntPtr comInterface, bool outParam)
            {
                this.comInterface = comInterface;
                managedVtable = (IStreamVtbl)Marshal.PtrToStructure(Marshal.ReadIntPtr(comInterface), typeof(IStreamVtbl));
                if (!outParam)
                    managedVtable.AddRef(comInterface);
            }

            ~NativeToManagedWrapper()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                managedVtable.Release(comInterface);
                if (disposing)
                {
                    comInterface = IntPtr.Zero;
                    managedVtable = null;
                    GC.SuppressFinalize(this);
                }
            }

            internal static IntPtr GetUnderlyingInterface(IStream managedInterface)
            {
                if (managedInterface is NativeToManagedWrapper)
                {
                    NativeToManagedWrapper wrapper = (NativeToManagedWrapper)managedInterface;

                    wrapper.managedVtable.AddRef(wrapper.comInterface);
                    return wrapper.comInterface;
                }
                else
                    return IntPtr.Zero;
            }

            internal static IStream GetInterface(IntPtr comInterface, bool outParam)
            {
                IStream managedInterface;

                if (comInterface == IntPtr.Zero)
                    return null;
#if !RECURSIVE_WRAPPING
                else if ((managedInterface = ManagedToNativeWrapper.GetUnderlyingInterface(comInterface, outParam)) == null)
#endif
                    managedInterface = (IStream)new NativeToManagedWrapper(comInterface, outParam);

                return managedInterface;
            }

            internal static void ReleaseInterface(IStream managedInterface)
            {
                if (managedInterface is NativeToManagedWrapper)
                    ((NativeToManagedWrapper)managedInterface).Dispose(true);
            }

            // Mono does not implement Marshal.ThrowExceptionForHR
            private static void ThrowExceptionForHR(int result)
            {
                if (result < 0)
                    throw new COMException(null, result);
            }

            public void Read(byte[] pv, int cb, IntPtr pcbRead)
            {
                ThrowExceptionForHR(managedVtable.Read(comInterface, pv, cb, pcbRead));
            }

            public void Write(byte[] pv, int cb, IntPtr pcbWritten)
            {
                ThrowExceptionForHR(managedVtable.Write(comInterface, pv, cb, pcbWritten));
            }

            public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
                ThrowExceptionForHR(managedVtable.Seek(comInterface, dlibMove, dwOrigin, plibNewPosition));
            }

            public void SetSize(long libNewSize)
            {
                ThrowExceptionForHR(managedVtable.SetSize(comInterface, libNewSize));
            }

            public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
                ThrowExceptionForHR(managedVtable.CopyTo(comInterface, pstm, cb, pcbRead, pcbWritten));
            }

            public void Commit(int grfCommitFlags)
            {
                ThrowExceptionForHR(managedVtable.Commit(comInterface, grfCommitFlags));
            }

            public void Revert()
            {
                ThrowExceptionForHR(managedVtable.Revert(comInterface));
            }

            public void LockRegion(long libOffset, long cb, int dwLockType)
            {
                ThrowExceptionForHR(managedVtable.LockRegion(comInterface, libOffset, cb, dwLockType));
            }

            public void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
                ThrowExceptionForHR(managedVtable.UnlockRegion(comInterface, libOffset, cb, dwLockType));
            }

            public void Stat(out STATSTG pstatstg, int grfStatFlag)
            {
                ThrowExceptionForHR(managedVtable.Stat(comInterface, out pstatstg, grfStatFlag));
            }

            public void Clone(out IStream ppstm)
            {
                IntPtr newInterface;

                ThrowExceptionForHR(managedVtable.Clone(comInterface, out newInterface));
                ppstm = NativeToManagedWrapper.GetInterface(newInterface, true);
            }
        }

        private static readonly ComIStreamMarshaler defaultInstance = new ComIStreamMarshaler();

        private ComIStreamMarshaler()
        {
        }

        private static ICustomMarshaler GetInstance(string cookie)
        {
            return defaultInstance;
        }

        public IntPtr MarshalManagedToNative(object managedObj)
        {
#if RECURSIVE_WRAPPING
			managedObj = NativeToManagedWrapper.GetInterface(ManagedToNativeWrapper.GetInterface((IStream)managedObj), true);
#endif
            return ManagedToNativeWrapper.GetInterface((IStream)managedObj);
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            ManagedToNativeWrapper.ReleaseInterface(pNativeData);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
#if RECURSIVE_WRAPPING
			pNativeData = ManagedToNativeWrapper.GetInterface(NativeToManagedWrapper.GetInterface(pNativeData, true));
#endif
            return NativeToManagedWrapper.GetInterface(pNativeData, false);
        }

        public void CleanUpManagedData(object managedObj)
        {
            NativeToManagedWrapper.ReleaseInterface((IStream)managedObj);
        }

        public int GetNativeDataSize()
        {
            return -1;
        }
    }
}
