// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public interface IXmlDictionary
    {
        bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result);
        bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result);
        bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result);
    }
}
