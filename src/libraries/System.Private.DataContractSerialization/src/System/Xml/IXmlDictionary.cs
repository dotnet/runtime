// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    public interface IXmlDictionary
    {
        bool TryLookup(string value, out XmlDictionaryString result);
        bool TryLookup(int key, out XmlDictionaryString result);
        bool TryLookup(XmlDictionaryString value, out XmlDictionaryString result);
    }
}
