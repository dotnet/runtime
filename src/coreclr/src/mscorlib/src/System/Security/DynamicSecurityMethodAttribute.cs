// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security
{
    // DynamicSecurityMethodAttribute:
    //  All methods that use StackCrawlMark should be marked with this attribute. This attribute
    //  disables inlining of the calling method to allow stackwalking to find the exact caller.
    //
    //  This attribute used to indicate that the target method requires space for a security object 
    //  to be allocated on the callers stack. It is not used for this purpose anymore because of security 
    //  stackwalks are not ever done in CoreCLR.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
    internal sealed class DynamicSecurityMethodAttribute : Attribute
    {
        public DynamicSecurityMethodAttribute() { }
    }
}
