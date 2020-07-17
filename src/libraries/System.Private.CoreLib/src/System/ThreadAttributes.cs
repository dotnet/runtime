// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Purpose: For Threads-related custom attributes.
**
=============================================================================*/

namespace System
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class STAThreadAttribute : Attribute
    {
        public STAThreadAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MTAThreadAttribute : Attribute
    {
        public MTAThreadAttribute()
        {
        }
    }
}
