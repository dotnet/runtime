// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------

namespace System.Security
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    sealed internal class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        internal AllowPartiallyTrustedCallersAttribute() { throw null; }

        internal PartialTrustVisibilityLevel PartialTrustVisibilityLevel
        {
            get { throw null; }
            set { throw null; }
        }
    }

    internal enum PartialTrustVisibilityLevel
    {
        VisibleToAllHosts = 0,
        NotVisibleByDefault = 1
    }
}
