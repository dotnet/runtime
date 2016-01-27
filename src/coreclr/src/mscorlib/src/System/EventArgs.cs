// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    using System;
    // The base class for all event classes.
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class EventArgs {
        public static readonly EventArgs Empty = new EventArgs();
    
        public EventArgs() 
        {
        }
    }
}
