//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace System.Runtime.Serialization
{
    using System;
    using System.Reflection;
    using System.Collections.ObjectModel;

    public interface ISerializationSurrogateProvider
    {
        Type GetSurrogateType(Type type);
        object GetObjectToSerialize(object obj, Type targetType);
        object GetDeserializedObject(object obj, Type targetType);
    }
}
