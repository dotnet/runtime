// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;


namespace System.Runtime.Serialization
{
    public abstract class DataContractResolver
    {
        public abstract bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace);
        public abstract Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver);
    }
}
