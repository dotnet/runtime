// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Xml.Linq
{
    public partial class XElement
    {
        // Needed for serialization to correctly generate code to deserialize XElement on .NET Native
        internal XElement() { }
    }
}
