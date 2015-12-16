// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;

unsafe partial class Interop
{
    public static partial class mincore_PInvokes
    {
#if FEATURE_APPX
        public const int LOCALE_NAME_MAX_LENGTH = 85;
        [DllImport("api-ms-win-core-localization-l1-2-0.dll")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static extern int ResolveLocaleName(string lpNameToResolve, StringBuilder lpLocaleName, int cchLocaleName);
#endif

#if COMINTEROP_IRESTRICTEDERRORINFO_IMPLEMENTED
        [DllImport("combase.dll", PreserveSig = false)]
        private static extern IRestrictedErrorInfo GetRestrictedErrorInfo();
#endif

        [DllImport("api-ms-win-core-winrt-robuffer-l1-1-0.dll", CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern Int32 RoGetBufferMarshaler(out IMarshal bufferMarshalerPtr);

    }
}

