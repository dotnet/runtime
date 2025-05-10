// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema.DateAndTime.Specifications
{
    /// <summary>
    /// Internal representation of <see cref="System.DateTimeKind"/>.
    /// </summary>
    internal enum XsdDateTimeKind
    {
        Unspecified,
        Zulu,
        LocalWestOfZulu,    // GMT-1..14, N..Y
        LocalEastOfZulu     // GMT+1..14, A..M
    }
}
