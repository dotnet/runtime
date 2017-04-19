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

public class EmptyNamespaceResolver : DataContractResolver
{
    public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
    {
        XmlDictionary dic = new XmlDictionary();
        if (dataContractType == typeof(EmptyNsContainer))
        {
            typeName = dic.Add("EmptyNsContainer");
            typeNamespace = dic.Add("MyNamespace");
            return true;
        }
        else if (dataContractType == typeof(UknownEmptyNSAddress))
        {
            typeName = dic.Add("AddressFoo");
            typeNamespace = dic.Add("");
            return true;
        }
        else
        {
            return knownTypeResolver.TryResolveType(dataContractType, declaredType, null, out typeName, out typeNamespace);
        }
    }

    public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
    {
        if (typeNamespace == "MyNamespace")
        {
            switch (typeName)
            {
                case "EmptyNsContainer":
                    return typeof(EmptyNsContainer);
            }
        }
        else if (typeName.Equals("AddressFoo"))
        {
            return typeof(UknownEmptyNSAddress);
        }

        return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
    }
}

[Serializable]
public class ProxyDataContractResolver : DataContractResolver
{
    public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
    {
        return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
    }

    public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
    {
        Type actualDataContractType = dataContractType.Name.EndsWith("Proxy") ? dataContractType.BaseType : dataContractType;
        return knownTypeResolver.TryResolveType(actualDataContractType, declaredType, null, out typeName, out typeNamespace);
    }
}
