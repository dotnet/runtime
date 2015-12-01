// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.Serialization
{
    public static class DataContractSerializerExtensions
    {
        public static ISerializationSurrogateProvider GetSerializationSurrogateProvider(this DataContractSerializer serializer) 
        {
            return serializer.SerializationSurrogateProvider;
        }

        public static void SetSerializationSurrogateProvider(this DataContractSerializer serializer, ISerializationSurrogateProvider provider)
        {
            serializer.SerializationSurrogateProvider = provider;
        }
    }
}
