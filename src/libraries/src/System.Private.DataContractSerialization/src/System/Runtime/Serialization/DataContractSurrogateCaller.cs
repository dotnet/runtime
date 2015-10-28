//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace System.Runtime.Serialization
{
    using System;
    using System.CodeDom;
    using System.Reflection;
    using System.Collections.ObjectModel;

    static class DataContractSurrogateCaller
    {
        internal static Type GetDataContractType(ISerializationSurrogateProvider surrogateProvider, Type type)
        {
            if (DataContract.GetBuiltInDataContract(type) != null)
                return type;
            Type dcType = surrogateProvider.GetSurrogateType(type);
            if (dcType == null)
                return type;
            return dcType;
        }

        internal static object GetObjectToSerialize(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type membertype)
        {
            if (obj == null)
                return null;
            if (DataContract.GetBuiltInDataContract(objType) != null)
                return obj;
            return surrogateProvider.GetObjectToSerialize(obj, membertype);
        }

        internal static object GetDeserializedObject(ISerializationSurrogateProvider surrogateProvider, object obj, Type objType, Type memberType)
        {
            if (obj == null)
                return null;
            if (DataContract.GetBuiltInDataContract(objType) != null)
                return obj;
            return surrogateProvider.GetDeserializedObject(obj, memberType);
        }
    }
}
