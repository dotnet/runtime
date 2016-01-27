// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
**  
**
** Purpose:     Custom attribute to indicate that the field should be treated 
**              as a static relative to a context.
**          
**
**
===========================================================*/
namespace System {
    
    using System;
    using System.Runtime.Remoting;
[Serializable]
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_CORECLR
    [Obsolete("ContextStaticAttribute is not supported in this release. It has been left in so that legacy tools can be used with this release, but it cannot be used in your code.", true)] 
#endif // FEATURE_CORECLR
    public class  ContextStaticAttribute : Attribute
    {
        public ContextStaticAttribute()
        {
        }
    }
}
