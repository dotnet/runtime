// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl
{
    internal interface ISourceLineInfo
    {
        string Uri { get; }
        bool IsNoSource { get; }
        Location Start { get; }
        Location End { get; }
    }
}
