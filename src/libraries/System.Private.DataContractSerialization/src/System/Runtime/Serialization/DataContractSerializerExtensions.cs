// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    public static class DataContractSerializerExtensions
    {
        // TODO: I propose to add an Obsolete attribute to the below two methods.

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
