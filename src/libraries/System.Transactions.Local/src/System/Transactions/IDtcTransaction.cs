// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions
{
    [ComImport]
    [Guid("0fb15084-af41-11ce-bd2b-204c4f4f5020")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDtcTransaction
    {
        void Commit(int retaining, [MarshalAs(UnmanagedType.I4)] int commitType, int reserved);

        void Abort(IntPtr reason, int retaining, int async);

        void GetTransactionInfo(IntPtr transactionInformation);
    }
}
