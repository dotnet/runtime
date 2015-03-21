// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

namespace System.Xml
{
    public interface IXmlDictionary
    {
        bool TryLookup(string value, out XmlDictionaryString result);
        bool TryLookup(int key, out XmlDictionaryString result);
        bool TryLookup(XmlDictionaryString value, out XmlDictionaryString result);
    }
}
