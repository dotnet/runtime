// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_COMINTEROP

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding managed delegates
    /// to COM's connection point based events.
    /// </summary>
    internal static class NativeMethods
    {
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("00020400-0000-0000-C000-000000000046")]
        internal interface IDispatch
        {
        }
    }
}

#endif
