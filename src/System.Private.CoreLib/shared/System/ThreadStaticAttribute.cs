// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose:     Custom attribute to indicate that the field should be treated 
**              as a static relative to a thread.
**          
**
**
===========================================================*/

#nullable enable
using System;

namespace System
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class ThreadStaticAttribute : Attribute
    {
        public ThreadStaticAttribute()
        {
        }
    }
}
