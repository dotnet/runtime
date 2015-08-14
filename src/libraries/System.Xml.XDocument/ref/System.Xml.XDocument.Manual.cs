// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Xml.Linq
{
    public partial class XElement
    {
        // Needed for serialization to correctly generate code to deserialize XElement on .NET Native
        internal XElement() { }
    }
}
