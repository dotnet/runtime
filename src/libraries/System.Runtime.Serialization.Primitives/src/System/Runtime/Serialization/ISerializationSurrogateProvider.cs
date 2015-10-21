// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.Serialization
{
    public interface ISerializationSurrogateProvider
    {
        Type GetSurrogateType(Type type);
        object GetObjectToSerialize(object obj, Type targetType);
        object GetDeserializedObject(object obj, Type targetType);
    }
}
