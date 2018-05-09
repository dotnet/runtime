// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    // AllowPartiallyTrustedCallersAttribute:
    //  Indicates that the Assembly is secure and can be used by untrusted
    //  and semitrusted clients
    //  For v.1, this is valid only on Assemblies, but could be expanded to 
    //  include Module, Method, class
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
    {
        public AllowPartiallyTrustedCallersAttribute() { }
        public PartialTrustVisibilityLevel PartialTrustVisibilityLevel { get; set; }
    }
}

