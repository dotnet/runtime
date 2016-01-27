// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    using System;
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate void EventHandler(Object sender, EventArgs e);

    [Serializable]
    public delegate void EventHandler<TEventArgs>(Object sender, TEventArgs e); // Removed TEventArgs constraint post-.NET 4
}
