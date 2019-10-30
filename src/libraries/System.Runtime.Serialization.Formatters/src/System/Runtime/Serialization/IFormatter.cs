// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Runtime.Serialization
{
    public interface IFormatter
    {
        object Deserialize(Stream serializationStream);
        void Serialize(Stream serializationStream, object graph);
        ISurrogateSelector SurrogateSelector { get; set; }
        SerializationBinder Binder { get; set; }
        StreamingContext Context { get; set; }
    }
}
