// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPath/PathHelper.
        /// </summary>
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial uint GetLongPathNameW(
#else
        [DllImport(Libraries.Kernel32, BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern uint GetLongPathNameW(
#endif
            ref char lpszShortPath,
            ref char lpszLongPath,
            uint cchBuffer);

    }
}
