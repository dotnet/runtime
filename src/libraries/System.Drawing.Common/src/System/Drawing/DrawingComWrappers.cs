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
                Exception? ex = Marshal.GetExceptionForHR(result);
                if (ex != null)
                {
                    throw ex;
                }

                throw new ExternalException() { HResult = result };
            }
        }

        private static ComInterfaceEntry* InitializeComInterfaceEntry()
        {
            GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

            IntPtr iStreamVtbl = IStreamVtbl.Create(fpQueryInteface, fpAddRef, fpRelease);

            ComInterfaceEntry* wrapperEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DrawingComWrappers), sizeof(ComInterfaceEntry));
            wrapperEntry->IID = IStreamIID;
            wrapperEntry->Vtable = iStreamVtbl;
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

        internal static class IStreamVtbl
        {
            public static IntPtr Create(IntPtr fpQueryInteface, IntPtr fpAddRef, IntPtr fpRelease)
            {
                IntPtr* vtblRaw = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IStreamVtbl), IntPtr.Size * 14);
                vtblRaw[0] = fpQueryInteface;
                vtblRaw[1] = fpAddRef;
                vtblRaw[2] = fpRelease;
                vtblRaw[3] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)&ReadImplementation;
                vtblRaw[4] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, uint, uint*, int>)&WriteImplementation;
                vtblRaw[5] = (IntPtr)(delegate* unmanaged<IntPtr, long, SeekOrigin, ulong*, int>)&SeekImplementation;
                vtblRaw[6] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, int>)&SetSizeImplementation;
                vtblRaw[7] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, ulong, ulong*, ulong*, int>)&CopyToImplementation;
                vtblRaw[8] = (IntPtr)(delegate* unmanaged<IntPtr, uint, int>)&CommitImplementation;
                vtblRaw[9] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&RevertImplementation;
                vtblRaw[10] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, ulong, uint, int>)&LockRegionImplementation;
                vtblRaw[11] = (IntPtr)(delegate* unmanaged<IntPtr, ulong, ulong, uint, int>)&UnlockRegionImplementation;
                vtblRaw[12] = (IntPtr)(delegate* unmanaged<IntPtr, out Interop.Ole32.STATSTG, Interop.Ole32.STATFLAG, int>)&StatImplementation;
                vtblRaw[13] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr*, int>)&CloneImplementation;

                return (IntPtr)vtblRaw;
            }

            [UnmanagedCallersOnly]
            private static int ReadImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbRead)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Read(pv, cb, pcbRead);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int WriteImplementation(IntPtr thisPtr, byte* pv, uint cb, uint* pcbWritten)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Write(pv, cb, pcbWritten);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int SeekImplementation(IntPtr thisPtr, long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Seek(dlibMove, dwOrigin, plibNewPosition);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int SetSizeImplementation(IntPtr thisPtr, ulong libNewSize)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.SetSize(libNewSize);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int CopyToImplementation(IntPtr thisPtr, IntPtr pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    Interop.Ole32.IStream pstmStream = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)pstm);

                    inst.CopyTo(pstmStream, cb, pcbRead, pcbWritten);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int CommitImplementation(IntPtr thisPtr, uint grfCommitFlags)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Commit(grfCommitFlags);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }


                return OK;
            }

            [UnmanagedCallersOnly]
            private static int RevertImplementation(IntPtr thisPtr)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Revert();
                }
                catch (Exception e)
                {
                    return e.HResult;
                }


                return OK;
            }

            [UnmanagedCallersOnly]
            private static int LockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    return (int)inst.LockRegion(libOffset, cb, dwLockType);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            [UnmanagedCallersOnly]
            private static int UnlockRegionImplementation(IntPtr thisPtr, ulong libOffset, ulong cb, uint dwLockType)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    return (int)inst.UnlockRegion(libOffset, cb, dwLockType);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            [UnmanagedCallersOnly]
            private static int StatImplementation(IntPtr thisPtr, out Interop.Ole32.STATSTG pstatstg, Interop.Ole32.STATFLAG grfStatFlag)
            {
                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);
                    inst.Stat(out pstatstg, grfStatFlag);
                }
                catch (Exception e)
                {
                    pstatstg = default;
                    return e.HResult;
                }

                return OK;
            }

            [UnmanagedCallersOnly]
            private static int CloneImplementation(IntPtr thisPtr, IntPtr* ppstm)
            {
                if (ppstm == null)
                {
                    return (int)Interop.HRESULT.STG_E_INVALIDPOINTER;
                }

                try
                {
                    Interop.Ole32.IStream inst = ComInterfaceDispatch.GetInstance<Interop.Ole32.IStream>((ComInterfaceDispatch*)thisPtr);

                    *ppstm = Instance.GetOrCreateComInterfaceForObject(inst.Clone(), CreateComInterfaceFlags.None);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }

                return OK;
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
