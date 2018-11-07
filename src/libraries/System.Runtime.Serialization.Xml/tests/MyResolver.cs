// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Xml;

internal class MyResolver : DataContractResolver
{
    public bool ResolveNameInvoked = false;
    public bool TryResolveTypeInvoked = false;
    public bool DeclaredTypeIsNotNull = false;

    public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
    {
        ResolveNameInvoked = true;
        DeclaredTypeIsNotNull = declaredType != null;
        return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
    }

    public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver,
        out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
    {
        TryResolveTypeInvoked = true;
        return knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
    }
}
