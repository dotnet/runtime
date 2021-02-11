// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    public static class DataContractSerializerExtensions
    {
        public static ISerializationSurrogateProvider? GetSerializationSurrogateProvider(this DataContractSerializer serializer)
        {
            return serializer.SerializationSurrogateProvider;
        }

        public static void SetSerializationSurrogateProvider(this DataContractSerializer serializer, ISerializationSurrogateProvider? provider)
        {
            serializer.SerializationSurrogateProvider = provider;
        }
    }
}
