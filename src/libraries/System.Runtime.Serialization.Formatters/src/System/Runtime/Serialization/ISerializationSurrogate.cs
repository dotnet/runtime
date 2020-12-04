// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    public interface ISerializationSurrogate
    {
        void GetObjectData(object obj, SerializationInfo info, StreamingContext context);
        object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector);
    }
}
