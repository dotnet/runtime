// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace System.Runtime.InteropServices
{
    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDispatch
    {
        void GetIDsOfNames(
            ref Guid riid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2), In]
            string[] rgszNames,
            int cNames,
            int lcid,
            [Out] int[] rgDispId);

        ITypeInfo GetTypeInfo(
            int iTInfo,
            int lcid);

        int GetTypeInfoCount();

        void Invoke(
            int dispIdMember,
            ref Guid riid,
            int lcid,
            InvokeFlags wFlags,
            ref DISPPARAMS pDispParams,
            out object pVarResult,
            IntPtr pExcepInfo,
            IntPtr puArgErr);
    }

    [Flags]
    internal enum InvokeFlags : short
    {
        DISPATCH_METHOD = 1,
        DISPATCH_PROPERTYGET = 2,
        DISPATCH_PROPERTYPUT = 4,
        DISPATCH_PROPERTYPUTREF = 8
    }
}
