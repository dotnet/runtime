// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMIMoniker interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.FILETIME instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [StructLayout(LayoutKind.Sequential)]

    public struct FILETIME 
    {
        public int dwLowDateTime; 
        public int dwHighDateTime; 
    }

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IMoniker instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("0000000f-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIMoniker 
    {
        // IPersist portion
        void GetClassID(out Guid pClassID);

        // IPersistStream portion
        [PreserveSig]
        int IsDirty();
        void Load(UCOMIStream pStm);
        void Save(UCOMIStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
        void GetSizeMax(out Int64 pcbSize);

        // IMoniker portion
        void BindToObject(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [In()] ref Guid riidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult);
        void BindToStorage(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [In()] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out Object ppvObj);
        void Reduce(UCOMIBindCtx pbc, int dwReduceHowFar, ref UCOMIMoniker ppmkToLeft, out UCOMIMoniker ppmkReduced);
        void ComposeWith(UCOMIMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out UCOMIMoniker ppmkComposite);
        void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out UCOMIEnumMoniker ppenumMoniker);
        void IsEqual(UCOMIMoniker pmkOtherMoniker);
        void Hash(out int pdwHash);
        void IsRunning(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, UCOMIMoniker pmkNewlyRunning);
        void GetTimeOfLastChange(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, out FILETIME pFileTime);
        void Inverse(out UCOMIMoniker ppmk);
        void CommonPrefixWith(UCOMIMoniker pmkOther, out UCOMIMoniker ppmkPrefix);
        void RelativePathTo(UCOMIMoniker pmkOther, out UCOMIMoniker ppmkRelPath);
        void GetDisplayName(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out String ppszDisplayName);
        void ParseDisplayName(UCOMIBindCtx pbc, UCOMIMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] String pszDisplayName, out int pchEaten, out UCOMIMoniker ppmkOut);
        void IsSystemMoniker(out int pdwMksys);
    }
}
