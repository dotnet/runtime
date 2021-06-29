// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    /// <summary>
    /// The ComWrappers implementation for System.Data.OleDb's COM interop usages.
    ///
    /// Supports IErrorInfo COM interface.
    /// </summary>
    internal unsafe class OleDbComWrappers : ComWrappers
    {
        private const int S_OK = (int)Interop.HRESULT.S_OK;

        internal static OleDbComWrappers Instance { get; } = new OleDbComWrappers();

        private OleDbComWrappers() { }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            throw new NotImplementedException();
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

            Guid errorInfoIID = IErrorInfo.IID;
            int hr = Marshal.QueryInterface(externalComObject, ref errorInfoIID, out IntPtr comObject);
            if (hr == S_OK)
            {
                return new ErrorInfoWrapper(comObject);
            }

            throw new NotImplementedException();
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }

        internal interface IErrorInfo
        {
            static readonly Guid IID = new Guid(0x1CF2B120, 0x547D, 0x101B, 0x8E, 0xBB, 0x65, 0x08, 0x00, 0x2B, 0x2B, 0xD1, 0x19);

            System.Data.OleDb.OleDbHResult GetSource(out string source);

            System.Data.OleDb.OleDbHResult GetDescription(out string description);
        }

        private class ErrorInfoWrapper : IErrorInfo
        {
            private readonly IntPtr _wrappedInstance;

            public ErrorInfoWrapper(IntPtr wrappedInstance)
            {
                _wrappedInstance = wrappedInstance;
            }

            public void Dispose()
            {
                Marshal.Release(_wrappedInstance);
            }

            public unsafe System.Data.OleDb.OleDbHResult GetSource(out string source)
            {
                IntPtr pSource = IntPtr.Zero;
                int errorCode = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 4 /* IErrorInfo.GetSource slot */)))
                    (_wrappedInstance, &pSource);
                if (pSource == IntPtr.Zero)
                {
                    source = null;
                }
                else
                {
                    source = Marshal.PtrToStringBSTR(pSource);
                }

                return (System.Data.OleDb.OleDbHResult)errorCode;
            }

            public unsafe System.Data.OleDb.OleDbHResult GetDescription(out string description)
            {
                IntPtr pDescription;
                int errorCode = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 5 /* IErrorInfo.GetDescription slot */)))
                    (_wrappedInstance, &pDescription);
                if (pDescription == IntPtr.Zero)
                {
                    description = null;
                }
                else
                {
                    description = Marshal.PtrToStringBSTR(pDescription);
                }

                return (System.Data.OleDb.OleDbHResult)errorCode;
            }
        }

    }
}
