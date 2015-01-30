// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;

namespace System.Runtime.InteropServices
{
    [GuidAttribute("03973551-57A1-3900-A2B5-9083E3FF2943")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Activator))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Activator
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("917B14D0-2D9E-38B8-92A9-381ACF52F7C0")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Attribute))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Attribute
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }

    [GuidAttribute("C281C7F1-4AA9-3517-961A-463CFED57E75")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
    [TypeLibImportClassAttribute(typeof(System.Threading.Thread))]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _Thread
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
#endif
    }
}
