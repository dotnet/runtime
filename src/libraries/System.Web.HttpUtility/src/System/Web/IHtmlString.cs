// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Web
{
    /// <summary>Represents an HTML-encoded string that should not be encoded again.</summary>
    public interface IHtmlString
    {
        /// <summary>Returns an HTML-encoded string.</summary>
        /// <returns>An HTML-encoded string.</returns>
        string? ToHtmlString();
    }
}
