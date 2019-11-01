// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AdsSearchPreferenceInfo
    {
        public int /*AdsSearchPreferences*/ dwSearchPref;
        internal int pad;
        public AdsValue vValue;
        public int /*AdsStatus*/ dwStatus;
        internal int pad2;
    }
}
