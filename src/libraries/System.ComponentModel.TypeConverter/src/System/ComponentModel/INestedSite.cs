// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    /// <summary>
    /// Nested containers site objects using INestedSite. A nested site is simply a site with
    /// an additional property that can retrieve the full nested name of a component.
    /// </summary>
    public interface INestedSite : ISite
    {
        /// <summary>
        /// Returns the full name of the component in this site in the format of &lt;owner&gt;.&lt;component&gt;.
        /// If this component's site has a null name, FullName also returns null.
        /// </summary>
        string? FullName { get; }
    }
}
