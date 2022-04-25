// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Data.Common;
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
    internal sealed unsafe class OleDbComWrappers : ComWrappers
    {
        private const int S_OK = (int)OleDbHResult.S_OK;
        private static readonly Guid IID_IErrorInfo = new Guid(0x1CF2B120, 0x547D, 0x101B, 0x8E, 0x65, 0x08, 0x00, 0x2B, 0x2B, 0xD1, 0x19);

        internal static OleDbComWrappers Instance { get; } = new OleDbComWrappers();

        private OleDbComWrappers() { }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            throw new NotImplementedException();
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            Debug.Assert(flags == CreateObjectFlags.UniqueInstance);

            Guid errorInfoIID = IID_IErrorInfo;
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

        // Doc and type layout: https://docs.microsoft.com/windows/win32/api/oaidl/nn-oaidl-ierrorinfo
        private sealed class ErrorInfoWrapper : UnsafeNativeMethods.IErrorInfo, IDisposable
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

            [Obsolete("not used", true)]
            void UnsafeNativeMethods.IErrorInfo.GetGUID(/*deleted parameter signature*/)
            {
                throw new NotImplementedException();
            }

            public unsafe System.Data.OleDb.OleDbHResult GetSource(out string? source)
            {
                IntPtr pSource = IntPtr.Zero;
                int errorCode = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 4 /* IErrorInfo.GetSource slot */)))
                    (_wrappedInstance, &pSource);
                if (pSource == IntPtr.Zero || errorCode < 0)
                {
                    source = null;
                }
                else
                {
                    source = Marshal.PtrToStringBSTR(pSource);
                }

                if (pSource != IntPtr.Zero)
                {
                    Marshal.FreeBSTR(pSource);
                }

                return (System.Data.OleDb.OleDbHResult)errorCode;
            }

            public unsafe System.Data.OleDb.OleDbHResult GetDescription(out string? description)
            {
                IntPtr pDescription = IntPtr.Zero;
                int errorCode = ((delegate* unmanaged<IntPtr, IntPtr*, int>)(*(*(void***)_wrappedInstance + 5 /* IErrorInfo.GetDescription slot */)))
                    (_wrappedInstance, &pDescription);
                if (pDescription == IntPtr.Zero || errorCode < 0)
                {
                    description = null;
                }
                else
                {
                    description = Marshal.PtrToStringBSTR(pDescription);
                }

                if (pDescription != IntPtr.Zero)
                {
                    Marshal.FreeBSTR(pDescription);
                }

                return (System.Data.OleDb.OleDbHResult)errorCode;
            }
        }

    }
}
