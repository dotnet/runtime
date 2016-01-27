// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
//    This class defines entry point for multi-core JIT API
//
//
namespace System.Runtime {

    using System;
    
    using System.Reflection;

    using System.Security;
    using System.Security.Permissions;
    
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;

#if FEATURE_MULTICOREJIT

    public static class ProfileOptimization
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void InternalSetProfileRoot(string directoryPath);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);

        [SecurityCritical]
        public static void SetProfileRoot(string directoryPath)
        {
            InternalSetProfileRoot(directoryPath);
        }

        [SecurityCritical]
        public static void StartProfile(string profile)
        {
            InternalStartProfile(profile, IntPtr.Zero);
        }
    }

#endif
}

