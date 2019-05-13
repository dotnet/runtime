// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public int dwLowDateTime;
        public int dwHighDateTime;
    }

    [Guid("0000000f-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IMoniker
    {
        // IPersist portion
        void GetClassID(out Guid pClassID);

        // IPersistStream portion
        [PreserveSig]
        int IsDirty();
        void Load(IStream pStm);
        void Save(IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
        void GetSizeMax(out long pcbSize);

        // IMoniker portion
        void BindToObject(IBindCtx pbc, IMoniker? pmkToLeft, [In()] ref Guid riidResult, [MarshalAs(UnmanagedType.Interface)] out object ppvResult);
        void BindToStorage(IBindCtx pbc, IMoniker? pmkToLeft, [In()] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObj);
        void Reduce(IBindCtx pbc, int dwReduceHowFar, ref IMoniker? ppmkToLeft, out IMoniker? ppmkReduced);
        void ComposeWith(IMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out IMoniker? ppmkComposite);
        void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out IEnumMoniker? ppenumMoniker);
        [PreserveSig]
        int IsEqual(IMoniker pmkOtherMoniker);
        void Hash(out int pdwHash);
        [PreserveSig]
        int IsRunning(IBindCtx pbc, IMoniker? pmkToLeft, IMoniker? pmkNewlyRunning);
        void GetTimeOfLastChange(IBindCtx pbc, IMoniker? pmkToLeft, out FILETIME pFileTime);
        void Inverse(out IMoniker ppmk);
        void CommonPrefixWith(IMoniker pmkOther, out IMoniker? ppmkPrefix);
        void RelativePathTo(IMoniker pmkOther, out IMoniker? ppmkRelPath);
        void GetDisplayName(IBindCtx pbc, IMoniker? pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDisplayName);
        void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out int pchEaten, out IMoniker ppmkOut);
        [PreserveSig]
        int IsSystemMoniker(out int pdwMksys);
    }
}
