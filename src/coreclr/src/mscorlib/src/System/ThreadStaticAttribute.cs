// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
namespace System {
    
    using System;

[Serializable]
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public class  ThreadStaticAttribute : Attribute
    {
        public ThreadStaticAttribute()
        {
        }
    }
}
