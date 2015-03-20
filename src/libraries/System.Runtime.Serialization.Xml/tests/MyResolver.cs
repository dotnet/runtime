// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Xml;

internal class MyResolver : DataContractResolver
{
    public bool ResolveNameInvoked = false;
    public bool TryResolveTypeInvoked = false;

    public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
    {
        ResolveNameInvoked = true;
        return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
    }

    public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver,
        out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
    {
        TryResolveTypeInvoked = true;
        return knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
    }
}
