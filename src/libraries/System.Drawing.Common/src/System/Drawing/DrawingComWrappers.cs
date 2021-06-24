// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// The ComWrappers implementation for System.Drawing.Common's COM interop usages.
    ///
    /// Supports IStream and IPicture COM interfaces.
    /// </summary>
    internal unsafe class DrawingComWrappers : ComWrappers
    {
        private const int OK = 0;
        private static readonly Guid IStreamIID = new Guid(0x0000000C, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
        private static readonly ComInterfaceEntry* s_wrapperEntry = InitializeComInterfaceEntry();
        internal static DrawingComWrappers Instance { get; } = new DrawingComWrappers();

        private DrawingComWrappers() { }

        internal static void CheckStatus(int result)
        {
            if (result != OK)
            {
                throw new ExternalException() { HResult = result };
            }
        }

        private static ComInterfaceEntry* InitializeComInterfaceEntry()
        {
            GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

            IntPtr iStreamVtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), sizeof(IStreamVtbl));
            IStreamVtbl.Fill((IStreamVtbl*)iStreamVtblRaw, fpQueryInteface, fpAddRef, fpRelease);

            ComInterfaceEntry* wrapperEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), sizeof(ComInterfaceEntry));
            wrapperEntry->IID = IStreamIID;
            wrapperEntry->Vtable = iStreamVtblRaw;
            return wrapperEntry;
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            Debug.Assert(obj is Interop.Ole32.IStream);
            Debug.Assert(s_wrapperEntry != null);

            // Always return the same table mappings.
            count = 1;
            return s_wrapperEntry;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            Debug.Assert(flags == CreateObjectFlags.None);

            Guid pictureIID = IPicture.IID;
            int hr = Marshal.QueryInterface(externalComObject, ref pictureIID, out IntPtr comObject);
            if (hr == 0)
            {
                return new PictureWrapper(comObject);
            }

            throw new NotImplementedException();
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }

        internal struct IUnknownVtbl
        {
            public IntPtr QueryInterface;
            public IntPtr AddRef;
            public IntPtr Release;
        }

        internal unsafe struct IStreamVtbl
        {
            public IUnknownVtbl IUnknownImpl;
            public delegate* unmanaged<IntPtr, byte*, uint, uint*, Interop.HRESULT> Read;
            public delegate* unmanaged<IntPtr, byte*, uint, uint*, Interop.HRESULT> Write;
            public delegate* unmanaged<IntPtr, long, SeekOrigin, ulong*, Interop.HRESULT> Seek;
            public delegate* unmanaged<IntPtr, ulong, Interop.HRESULT> SetSize;
            public delegate* unmanaged<IntPtr, IntPtr, ulong, ulong*, ulong*, Interop.HRESULT> CopyTo;
            public delegate* unmanaged<IntPtr, uint, Interop.HRESULT> Commit;
            public delegate* unmanaged<IntPtr, Interop.HRESULT> Revert;
            public delegate* unmanaged<IntPtr, ulong, ulong, uint, Interop.HRESULT> LockRegion;
            public delegate* unmanaged<IntPtr, ulong, ulong, uint, Interop.HRESULT> UnlockRegion;
            public delegate* unmanaged<IntPtr, out Interop.Ole32.STATSTG, Interop.Ole32.STATFLAG, Interop.HRESULT> Stat;
            public delegate* unmanaged<IntPtr, IntPtr*, Interop.HRESULT> Clone;

            public static void Fill(IStreamVtbl* vtable, IntPtr fpQueryInteface, IntPtr fpAddRef, IntPtr fpRelease)
            {
                vtable->IUnknownImpl = new IUnknownVtbl()
                {
                    QueryInterface = fpQueryInteface,
                    AddRef = fpAddRef,
                    Release = fpRelease
                };
                vtable->Read = &ReadImplementation;
                vtable->Write = &WriteImplementation;
                vtable->Seek = &SeekImplementation;
                vtable->SetSize = &SetSizeImplementation;
                vtable->CopyTo = &CopyToImplementation;
                vtable->Commit = &CommitImplementation;
                vtable->Revert = &RevertImplementation;
                vtable->LockRegion = &LockRegionImplementation;
                vtable->UnlockRegion = &UnlockRegionImplementation;
                vtable->Stat = &StatImplementation;
                vtable->Clone = &CloneImplementation;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT ReadImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbRead)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Read(pv, cb, pcbRead);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT WriteImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbWritten)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Write(pv, cb, pcbWritten);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT SeekImplementation(IntPtr thisPtr, long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Seek(dlibMove, dwOrigin, plibNewPosition);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT SetSizeImplementation(IntPtr thisPtr, ulong libNewSize)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.SetSize(libNewSize);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT CopyToImplementation(IntPtr thisPtr, IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                Interop.Ole32.IStream pstmStream = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)pstm);

                inst.CopyTo(pstmStream, cb, pcbRead, pcbWritten);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT CommitImplementation(IntPtr thisPtr, uint grfCommitFlags)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Commit(grfCommitFlags);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT RevertImplementation(IntPtr thisPtr)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Revert();
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT LockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                return inst.LockRegion(libOffset, cb, dwLockType);
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT UnlockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                return inst.UnlockRegion(libOffset, cb, dwLockType);
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT StatImplementation(IntPtr thisPtr, out Interop.Ole32.STATSTG pstatstg, Interop.Ole32.STATFLAG grfStatFlag)
            {
                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                inst.Stat(out pstatstg, grfStatFlag);
                return Interop.HRESULT.S_OK;
            }

            [UnmanagedCallersOnly]
            private static Interop.HRESULT CloneImplementation(IntPtr thisPtr, IntPtr* ppstm)
            {
                if (ppstm == null)
                {
                    return Interop.HRESULT.STG_E_INVALIDPOINTER;
                }

                Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);

                *ppstm = Instance.GetOrCreateComInterfaceForObject(inst.Clone(), CreateComInterfaceFlags.None);
                return Interop.HRESULT.S_OK;
            }
        }

        internal interface IPicture : IDisposable
        {
            static readonly Guid IID = new Guid(0x7BF80980, 0xBF32, 0x101A, 0x8B, 0xBB, 0, 0xAA, 0x00, 0x30, 0x0C, 0xAB);

            // NOTE: Only SaveAsFile is invoked. The other methods on IPicture are not necessary

            int SaveAsFile(IntPtr pstm, int fSaveMemCopy, int* pcbSize);
        }

        private class PictureWrapper : IPicture
        {
            private readonly IntPtr _wrappedInstance;

            public PictureWrapper(IntPtr wrappedInstance)
            {
                _wrappedInstance = wrappedInstance;
            }

            public void Dispose()
            {
                Marshal.Release(_wrappedInstance);
            }

            public unsafe int SaveAsFile(IntPtr pstm, int fSaveMemCopy, int* pcbSize)
            {
                // Get the IStream implementation, since the ComWrappers runtime returns a pointer to the IUnknown interface implementation
                Guid streamIID = IStreamIID;
                CheckStatus(Marshal.QueryInterface(pstm, ref streamIID, out IntPtr pstmImpl));

                try
                {
                    return ((delegate* unmanaged<IntPtr, IntPtr, int, int*, int>)(*(*(void***)_wrappedInstance + 15 /* IPicture.SaveAsFile slot */)))
                        (_wrappedInstance, pstmImpl, fSaveMemCopy, pcbSize);
                }
                finally
                {
                    Marshal.Release(pstmImpl);
                }
            }
        }
    }
}
