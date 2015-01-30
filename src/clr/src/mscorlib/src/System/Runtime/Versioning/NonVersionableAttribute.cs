// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

namespace System.Runtime.Versioning {

    // This Conditional is here to strip the annotations for targets where ReadyToRun is not supported.
    // If this attribute is ever made public, this Conditional should be removed.
    [Conditional("FEATURE_READYTORUN")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, 
                    AllowMultiple = false, Inherited = false)]
    sealed class NonVersionableAttribute : Attribute {

        public NonVersionableAttribute()
        {
        }
    }
}
