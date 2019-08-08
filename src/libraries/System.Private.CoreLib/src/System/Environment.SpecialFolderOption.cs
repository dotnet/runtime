// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Environment
    {
        public enum SpecialFolderOption
        {
            None = 0,
            Create = SpecialFolderOptionValues.CSIDL_FLAG_CREATE,
            DoNotVerify = SpecialFolderOptionValues.CSIDL_FLAG_DONT_VERIFY,
        }

        // These values are specific to Windows and are known to SHGetFolderPath, however they are
        // also the values used in the SpecialFolderOption enum.  As such, we keep them as constants
        // with their Win32 names, but keep them here rather than in Interop.Kernel32 as they're
        // used on all platforms.
        private static class SpecialFolderOptionValues
        {
            /// <summary>
            /// Force folder creation in SHGetFolderPath. Equivalent of KF_FLAG_CREATE (0x00008000).
            /// </summary>
            internal const int CSIDL_FLAG_CREATE = 0x8000;

            /// <summary>
            /// Return an unverified folder path. Equivalent of KF_FLAG_DONT_VERIFY (0x00004000).
            /// </summary>
            internal const int CSIDL_FLAG_DONT_VERIFY = 0x4000;
        }
    }
}
