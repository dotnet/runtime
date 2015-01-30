// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Security;

namespace System {

/// <summary>
/// For now, this class should be the central point to collect all managed declarations
/// of native functions designed to expose config switches.
/// In Dev11 M2.2 we will redesign this class to expose CLRConfig from within the CLR
/// and refactor managed Fx code to access all compat switches through here.
/// </summary>
[FriendAccessAllowed]
internal class CLRConfig {
    
    [FriendAccessAllowed]
    [System.Security.SecurityCritical]
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    [SuppressUnmanagedCodeSecurity]
    internal static extern bool CheckLegacyManagedDeflateStream();

    [System.Security.SecurityCritical]
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    [SuppressUnmanagedCodeSecurity]
    internal static extern bool CheckThrowUnobservedTaskExceptions();

}  // internal class CLRConfig

}  // namespace System

// file CLRConfig
