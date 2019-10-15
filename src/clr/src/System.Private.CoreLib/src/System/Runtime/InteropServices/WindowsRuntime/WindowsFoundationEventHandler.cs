// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // WindowsFoundationEventHandler<T> a copy of the definition for the Windows.Foundation.EventHandler<T> delegate
    [Guid("9de1c535-6ae1-11e0-84e1-18a905bcc53f")]
    [WindowsRuntimeImport]
    internal delegate void WindowsFoundationEventHandler<T>(object sender, T args);
}
