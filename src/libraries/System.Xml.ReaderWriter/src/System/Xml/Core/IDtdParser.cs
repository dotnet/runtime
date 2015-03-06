// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
