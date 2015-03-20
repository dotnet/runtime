// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

using System.Reflection;


namespace System.Runtime.Serialization
{
    internal class DataContractSet
    {
        private DataContractSet() { }


        private static bool IsTypeReferenceable(Type type)
        {
            Type itemType;
            return (type.GetTypeInfo().IsEnum ||
                    type.GetTypeInfo().IsDefined(Globals.TypeOfDataContractAttribute, false) ||
                    (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) && !type.GetTypeInfo().IsGenericTypeDefinition) ||
                    CollectionDataContract.IsCollection(type, out itemType) ||
                    ClassDataContract.IsNonAttributedTypeValidForSerialization(type));
        }
    }
}



