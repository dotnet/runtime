// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml;


namespace System.Runtime.Serialization
{
    public abstract class DataContractResolver
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract bool TryResolveType(Type type, Type? declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString? typeName, out XmlDictionaryString? typeNamespace);
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract Type? ResolveName(string typeName, string? typeNamespace, Type? declaredType, DataContractResolver knownTypeResolver);
    }
}
