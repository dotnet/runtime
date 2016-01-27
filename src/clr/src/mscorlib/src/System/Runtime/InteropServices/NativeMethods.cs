// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
**
** Purpose: part of ComEventHelpers APIs which allow binding 
** managed delegates to COM's connection point based events.
**
**/
#if FEATURE_COMINTEROP

namespace System.Runtime.InteropServices {
    
    internal static class NativeMethods {

        [
        System.Security.SuppressUnmanagedCodeSecurity,
        DllImport("oleaut32.dll", PreserveSig = false),
        System.Security.SecurityCritical
        ]
        internal static extern void VariantClear(IntPtr variant);

        [
        System.Security.SuppressUnmanagedCodeSecurity,
        ComImport,
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        Guid("00020400-0000-0000-C000-000000000046")
        ]
        internal interface IDispatch {

            [System.Security.SecurityCritical]
            void GetTypeInfoCount(out uint pctinfo);

            [System.Security.SecurityCritical]
            void GetTypeInfo(uint iTInfo, int lcid, out IntPtr info);

            [System.Security.SecurityCritical]
            void GetIDsOfNames(
                ref Guid iid,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]
                string[] names,
                uint cNames,
                int lcid,
                [Out]
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4, SizeParamIndex = 2)]
                int[] rgDispId);

            [System.Security.SecurityCritical]
            void Invoke(
                int dispIdMember,
                ref Guid riid,
                int lcid,
                ComTypes.INVOKEKIND wFlags,
                ref ComTypes.DISPPARAMS pDispParams,
                IntPtr pvarResult,
                IntPtr pExcepInfo,
                IntPtr puArgErr);
        }
    }
}

#endif
