// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: For Threads-related custom attributes.
**
**
=============================================================================*/

namespace System {
    [AttributeUsage (AttributeTargets.Method)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class STAThreadAttribute : Attribute
    {
        public STAThreadAttribute()
        {
        }
    }

    [AttributeUsage (AttributeTargets.Method)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class MTAThreadAttribute : Attribute
    {
        public MTAThreadAttribute()
        {
        }
    }
}
