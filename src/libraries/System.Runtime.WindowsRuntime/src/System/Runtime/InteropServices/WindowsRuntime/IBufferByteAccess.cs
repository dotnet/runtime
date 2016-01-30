// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using GUID = System.Runtime.InteropServices.GuidAttribute;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [GUID("905a0fef-bc53-11df-8c49-001e4fc686da")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    /// <summary>
    /// WinRT's <code>IBufferByteAccess</code> interface definition.
    /// </summary>
    internal interface IBufferByteAccess
    {
        // This needs to be a function - MCG doesn't support properties/events for [ComImport] interface yet
        IntPtr GetBuffer();
    }  // interface IBufferByteAccess
}  // namespace

// IBufferByteAccess.cs
