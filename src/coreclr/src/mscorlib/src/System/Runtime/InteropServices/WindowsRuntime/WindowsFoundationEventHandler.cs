// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // WindowsFoundationEventHandler<T> a copy of the definition for the Windows.Foundation.EventHandler<T> delegate
    [Guid("9de1c535-6ae1-11e0-84e1-18a905bcc53f")]
    [WindowsRuntimeImport]
    internal delegate void WindowsFoundationEventHandler<T>(object sender, T args);
}
