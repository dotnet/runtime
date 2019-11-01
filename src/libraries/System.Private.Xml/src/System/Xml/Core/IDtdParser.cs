// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;

namespace System.Xml
{
    internal partial interface IDtdParser
    {
        IDtdInfo ParseInternalDtd(IDtdParserAdapter adapter, bool saveInternalSubset);
        IDtdInfo ParseFreeFloatingDtd(string baseUri, string docTypeName, string publicId, string systemId, string internalSubset, IDtdParserAdapter adapter);
    }
}
