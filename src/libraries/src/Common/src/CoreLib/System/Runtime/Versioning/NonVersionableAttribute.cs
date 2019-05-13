// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** The [NonVersionable] attribute is applied to indicate that the implementation 
** of a particular member or layout of a struct cannot be changed for given platform in incompatible way.
** This allows cross-module inlining of methods and data structures whose implementation 
** is never changed in ReadyToRun native images. Any changes to such members or types would be 
** breaking changes for ReadyToRun.
**
** Applying this type also has the side effect that the inlining tables in R2R images will not
** report that inlining of NonVersionable attributed methods occured. These inlining tables are used
** by profilers to figure out the set of methods that need to be rejited when one method is instrumented,
** so in effect NonVersionable methods are also non-instrumentable. Generally this is OK for
** extremely trivial low level methods where NonVersionable gets used, but if there is any plan to 
** significantly extend its usage or allow 3rd parties to use it please discuss with the diagnostics team.
===========================================================*/

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
                    AllowMultiple = false, Inherited = false)]
    internal sealed class NonVersionableAttribute : Attribute
    {
        public NonVersionableAttribute()
        {
        }
    }
}
