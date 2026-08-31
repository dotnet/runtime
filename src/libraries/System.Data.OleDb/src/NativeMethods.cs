// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Data.Common
{
    internal static partial class NativeMethods
    {
        [Guid("0c733a1e-2a1c-11ce-ade5-00aa0044773d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
        internal interface ISourcesRowset
        {
            [PreserveSig]
            System.Data.OleDb.OleDbHResult GetSourcesRowset(
                [In] IntPtr pUnkOuter,
                [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [In] int cPropertySets,
                [In] IntPtr rgProperties,
                [Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
        }

        [Guid("0C733A5E-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
        internal interface ITransactionJoin
        {
            [Obsolete("not used", true)]
            [PreserveSig]
            int GetOptionsObject(IntPtr ppOptions);

            void JoinTransaction(
                [In, MarshalAs(UnmanagedType.Interface)] object? punkTransactionCoord,
                [In] int isoLevel,
                [In] int isoFlags,
                [In] IntPtr pOtherOptions);
        }
    }
}
