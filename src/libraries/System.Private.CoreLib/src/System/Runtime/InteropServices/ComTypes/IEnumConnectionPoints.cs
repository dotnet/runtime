// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes
{
    [Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IEnumConnectionPoints
    {
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] IConnectionPoint[] rgelt, IntPtr pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumConnectionPoints ppenum);
    }
}
