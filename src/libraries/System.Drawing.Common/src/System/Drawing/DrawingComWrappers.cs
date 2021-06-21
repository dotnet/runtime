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
        private static readonly ComInterfaceEntry* s_wrapperEntry = InitializeComInterfaceEntry();
        private static readonly Lazy<DrawingComWrappers> s_instance = new Lazy<DrawingComWrappers>(() => new DrawingComWrappers());

        private DrawingComWrappers() { }

        internal static DrawingComWrappers Instance => s_instance.Value;

        internal static void CheckStatus(int result)
        {
            if (result != 0)
            {
                throw new ExternalException() { HResult = result };
            }
        }

        private static ComInterfaceEntry* InitializeComInterfaceEntry()
        {
            GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

            IStreamVtbl iStreamVtbl = IStreamVtbl.Create(fpQueryInteface, fpAddRef, fpRelease);

            IntPtr iStreamVtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), sizeof(IStreamVtbl));
            Marshal.StructureToPtr(iStreamVtbl, iStreamVtblRaw, false);

            ComInterfaceEntry* wrapperEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), sizeof(ComInterfaceEntry));
            wrapperEntry->IID = Interop.Ole32.IStreamComWrapper.Guid;
            wrapperEntry->Vtable = iStreamVtblRaw;
            return wrapperEntry;
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            Debug.Assert(obj is Interop.Ole32.IStreamComWrapper);
            Debug.Assert(s_wrapperEntry != null);

            // Always return the same table mappings.
            count = 1;
            return s_wrapperEntry;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            Debug.Assert(flags == CreateObjectFlags.None);

            Guid pictureGuid = IPicture.Guid;
            int hr = Marshal.QueryInterface(externalComObject, ref pictureGuid, out IntPtr comObject);
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

        internal struct IStreamVtbl
        {
            public IUnknownVtbl IUnknownImpl;
            public IntPtr Read;
            public IntPtr Write;
            public IntPtr Seek;
            public IntPtr SetSize;
            public IntPtr CopyTo;
            public IntPtr Commit;
            public IntPtr Revert;
            public IntPtr LockRegion;
            public IntPtr UnlockRegion;
            public IntPtr Stat;
            public IntPtr Clone;

            private delegate void _Read(IntPtr thisPtr, byte* pv, uint cb, uint* pcbRead);
            private delegate void _Write(IntPtr thisPtr, byte* pv, uint cb, uint* pcbWritten);
            private delegate void _Seek(IntPtr thisPtr, long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition);
            private delegate void _SetSize(IntPtr thisPtr, ulong libNewSize);
            private delegate void _CopyTo(IntPtr thisPtr, IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten);
            private delegate void _Commit(IntPtr thisPtr, uint grfCommitFlags);
            private delegate void _Revert(IntPtr thisPtr);
            private delegate Interop.HRESULT _LockRegion(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType);
            private delegate Interop.HRESULT _UnlockRegion(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType);
            private delegate void _Stat(IntPtr thisPtr, out Interop.Ole32.STATSTG pstatstg, Interop.Ole32.STATFLAG grfStatFlag);
            private delegate IntPtr _Clone(IntPtr thisPtr);

            private static _Read s_Read = new _Read(ReadImplementation);
            private static _Write s_Write = new _Write(WriteImplementation);
            private static _Seek s_Seek = new _Seek(SeekImplementation);
            private static _SetSize s_SetSize = new _SetSize(SetSizeImplementation);
            private static _CopyTo s_CopyTo = new _CopyTo(CopyToImplementation);
            private static _Commit s_Commit = new _Commit(CommitImplementation);
            private static _Revert s_Revert = new _Revert(RevertImplementation);
            private static _LockRegion s_LockRegion = new _LockRegion(LockRegionImplementation);
            private static _UnlockRegion s_UnlockRegion = new _UnlockRegion(UnlockRegionImplementation);
            private static _Stat s_Stat = new _Stat(StatImplementation);
            private static _Clone s_Clone = new _Clone(CloneImplementation);

            public static IStreamVtbl Create(IntPtr fpQueryInteface, IntPtr fpAddRef, IntPtr fpRelease)
            {
                return new IStreamVtbl()
                {
                    IUnknownImpl = new IUnknownVtbl()
                    {
                        QueryInterface = fpQueryInteface,
                        AddRef = fpAddRef,
                        Release = fpRelease
                    },
                    Read = Marshal.GetFunctionPointerForDelegate(s_Read),
                    Write = Marshal.GetFunctionPointerForDelegate(s_Write),
                    Seek = Marshal.GetFunctionPointerForDelegate(s_Seek),
                    SetSize = Marshal.GetFunctionPointerForDelegate(s_SetSize),
                    CopyTo = Marshal.GetFunctionPointerForDelegate(s_CopyTo),
                    Commit = Marshal.GetFunctionPointerForDelegate(s_Commit),
                    Revert = Marshal.GetFunctionPointerForDelegate(s_Revert),
                    LockRegion = Marshal.GetFunctionPointerForDelegate(s_LockRegion),
                    UnlockRegion = Marshal.GetFunctionPointerForDelegate(s_UnlockRegion),
                    Stat = Marshal.GetFunctionPointerForDelegate(s_Stat),
                    Clone = Marshal.GetFunctionPointerForDelegate(s_Clone),
                };
            }

            private static void ReadImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbRead)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Read(pv, cb, pcbRead);
            }

            private static void WriteImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbWritten)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Write(pv, cb, pcbWritten);
            }

            private static void SeekImplementation(IntPtr thisPtr, long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Seek(dlibMove, dwOrigin, plibNewPosition);
            }

            private static void SetSizeImplementation(IntPtr thisPtr, ulong libNewSize)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.SetSize(libNewSize);
            }

            private static void CopyToImplementation(IntPtr thisPtr, IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                Interop.Ole32.IStreamComWrapper pstmStream = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)pstm);

                inst.CopyTo(pstmStream, cb, pcbRead, pcbWritten);
            }

            private static void CommitImplementation(IntPtr thisPtr, uint grfCommitFlags)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Commit(grfCommitFlags);
            }

            private static void RevertImplementation(IntPtr thisPtr)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Revert();
            }

            private static Interop.HRESULT LockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                return inst.LockRegion(libOffset, cb, dwLockType);
            }

            private static Interop.HRESULT UnlockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                return inst.UnlockRegion(libOffset, cb, dwLockType);
            }

            private static void StatImplementation(IntPtr thisPtr, out Interop.Ole32.STATSTG pstatstg, Interop.Ole32.STATFLAG grfStatFlag)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);
                inst.Stat(out pstatstg, grfStatFlag);
            }

            private static IntPtr CloneImplementation(IntPtr thisPtr)
            {
                Interop.Ole32.IStreamComWrapper inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStreamComWrapper>((ComInterfaceDispatch*)thisPtr);

                return Instance.GetOrCreateComInterfaceForObject(inst.Clone(), CreateComInterfaceFlags.None);
            }
        }

#pragma warning disable CS0649 // fields are never assigned to
        internal struct IPictureVtbl
        {
            public IUnknownVtbl IUnknownImpl;
            public IntPtr GetHandle;
            public IntPtr GetHPal;
            public IntPtr GetPictureType;
            public IntPtr GetWidth;
            public IntPtr GetHeight;
            public IntPtr Render;
            public IntPtr SetHPal;
            public IntPtr GetCurDC;
            public IntPtr SelectPicture;
            public IntPtr GetKeepOriginalFormat;
            public IntPtr SetKeepOriginalFormat;
            public IntPtr PictureChanged;
            public _SaveAsFile SaveAsFile;
            public IntPtr GetAttributes;
            public IntPtr SetHdc;

            public delegate int _SaveAsFile(IntPtr thisPtr, IntPtr pstm, int fSaveMemCopy, out int pcbSize);
        }

        internal struct VtblPtr
        {
            public IntPtr Vtbl;
        }
#pragma warning restore CS0649

        internal interface IPicture
        {
            static readonly Guid Guid = new Guid(0x7BF80980, 0xBF32, 0x101A, 0x8B, 0xBB, 0, 0xAA, 0x00, 0x30, 0x0C, 0xAB);

            // NOTE: Only SaveAsFile is invoked. The other methods on IPicture are not necessary

            int SaveAsFile(IntPtr pstm, int fSaveMemCopy, out int pcbSize);
        }

        private class PictureWrapper : IPicture
        {
            private readonly IntPtr _wrappedInstance;
            private readonly IPictureVtbl _vtable;

            public PictureWrapper(IntPtr wrappedInstance)
            {
                _wrappedInstance = wrappedInstance;

                VtblPtr inst = Marshal.PtrToStructure<VtblPtr>(_wrappedInstance);
                _vtable = Marshal.PtrToStructure<IPictureVtbl>(inst.Vtbl);
            }

            public int SaveAsFile(IntPtr pstm, int fSaveMemCopy, out int pcbSize)
            {
                // Get the IStream implementation, since the ComWrappers runtime returns a pointer to the IUnknown interface implementation
                Guid streamGuid = Interop.Ole32.IStreamComWrapper.Guid;
                CheckStatus(Marshal.QueryInterface(pstm, ref streamGuid, out IntPtr pstmImpl));

                return _vtable.SaveAsFile(_wrappedInstance, pstmImpl, fSaveMemCopy, out pcbSize);
            }
        }
    }
}
