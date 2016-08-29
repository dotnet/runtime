// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
   
    using System;
#if FEATURE_CORECLR
     [System.Security.SecurityCritical] // auto-generated
#endif
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate void UnhandledExceptionEventHandler(Object sender, UnhandledExceptionEventArgs e);
}
