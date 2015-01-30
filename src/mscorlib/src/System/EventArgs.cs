// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
