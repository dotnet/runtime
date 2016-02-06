// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  NonVersionableAttribute
**
**
** The [NonVersionable] attribute is applied to indicate that the implementation 
** of a particular member or layout of a struct cannot be changed for given platform in incompatible way.
** This allows cross-module inlining of methods and data structures whose implementation 
** is never changed in ReadyToRun native images. Any changes to such members or types would be 
** breaking changes for ReadyToRun.
**
===========================================================*/
using System;
using System.Diagnostics;

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, 
                    AllowMultiple = false, Inherited = false)]
    sealed internal class NonVersionableAttribute : Attribute
    {
        public NonVersionableAttribute()
        {
        }
    }
}
